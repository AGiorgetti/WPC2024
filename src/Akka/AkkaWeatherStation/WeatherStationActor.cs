using Akka.Actor;
using Akka.Persistence;
using AkkaShared;

// todo: Akka.net built-in Persistent Actor is not really well suited to implement a domain object.
//       Checking the invariants of the state might be hard.
//       We should:
//       - execute the command (some validation might be done here)
//       - attempt to change the aggregate state.
//       - check the invariant
//       - if the invariant is not satisfied, revert the state and throw an exception.
//       - if the invariant is satisfied, persist the event and apply it to the state.

public class WeatherStationActor : ReceivePersistentActor, IWithTimers
{
    public override string PersistenceId => $"weather-station-{_state.StationId}";

    public ITimerScheduler? Timers { get; set; }

    private WeatherStationState _state = new WeatherStationState();
    private readonly IActorRef _collector;
    public WeatherStationActor(string stationId, string location, IActorRef collector)
    {
        _state.StationId = stationId;
        _state.Location = location;
        _collector = collector;

        Command<StartStation>(cmd => OnStartStation());
        Command<PerformReading>(cmd => OnPerformReadingReentrant());
        Command<WeatherReadingDetected>(OnWeatherReadingDetected);
        Command<StationMalfunctionDetected>(OnStationMalfunctionDetected);
        Command<RestartStation>(cmd => OnRestartStation());

        Recover<StationStarted>(Apply);
        Recover<WeatherReadingDetected>(Apply);
        Recover<StationMalfunctionDetected>(Apply);
        Recover<StationRestarted>(Apply);
    }

    protected override void PreStart()
    {
        Timers!.StartPeriodicTimer("PerformReading", new PerformReading(), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
        
        base.PreStart();
    }

    protected override void PostStop()
    {
        base.PostStop();
    }

    private void Apply(StationStarted @event)
    {
        _state.Apply(@event);
    }

    private void Apply(WeatherReadingDetected @event)
    {
        _state.Apply(@event);
    }

    private void Apply(StationMalfunctionDetected @event)
    {
        _state.Apply(@event);
    }

    private void Apply(StationRestarted @event)
    {
        _state.Apply(@event);
    }

    private void OnStartStation()
    {
        Persist(new StationStarted(_state.StationId!, _state.Location!), @event =>
        {
            Apply(@event);

            // Registra la stazione nel collector
            _collector.Tell(new RegisterStation(_state.StationId!, _state.Location!));
            Console.WriteLine($"[{_state.StationId}] Registrato nel collector.");
        });
    }

    /// <summary>
    /// Nell'ipotesi di operazioni "lunghe" o bloccanti o se dobbiamo gestire rientranza:
    /// una delle tecniche che si possono adottare è delegare le operazioni a un attore figlio.
    /// Per non bloccare l'attore principale, viene creato un attore figlio per gestire la lettura.
    /// </summary>
    private void OnPerformReadingReentrant()
    {
        // Crea un nome unico per l'attore figlio
        var readingHandlerName = $"reading-handler-{Guid.NewGuid()}";

        // Crea l'attore figlio
        var readingHandler = Context.ActorOf(
            Props.Create(() => new ReadingHandlerActor(_state.StationId!)),
            readingHandlerName);

        // Invia il messaggio per avviare la lettura
        readingHandler.Tell(new ProcessReading());
    }

    private void OnWeatherReadingDetected(WeatherReadingDetected msg)
    {
        Persist(msg, msg =>
        {
            Apply(msg);

            // Invia la lettura al collector
            _collector.Tell(new ReportWeatherReading(_state.StationId!, msg.Temperature, msg.Pressure));
            Console.WriteLine($"[{_state.StationId}] Lettura inviata: Temp={msg.Temperature}, Press={msg.Pressure}");
        });
    }

    private void OnStationMalfunctionDetected(StationMalfunctionDetected msg)
    {
        Persist(msg, @event =>
        {
            Apply(msg);

            // Invia il malfunzionamento al collector
            _collector.Tell(new ReportStationMalfunction(_state.StationId!));
            Console.WriteLine($"[{_state.StationId}] Malfunzionamento rilevato.");
        });
    }

    private void OnRestartStation()
    {
        Persist(new StationRestarted(), @event =>
        {
            Apply(@event);
        });
        Console.WriteLine($"[{_state.StationId}] Riavviato su richiesta del collector.");
    }

    protected override SupervisorStrategy SupervisorStrategy()
    {
        return new OneForOneStrategy(
            maxNrOfRetries: 10,
            withinTimeRange: TimeSpan.FromMinutes(1),
            decider: Decider.From(exception =>
            {
                if (exception is ApplicationException)
                {
                    return Directive.Restart;
                }
                else
                {
                    return Directive.Stop;
                }
            }));
    }
}
