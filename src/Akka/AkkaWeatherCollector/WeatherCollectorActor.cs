using Akka.Actor;
using AkkaShared;

public class WeatherCollectorActor : ReceiveActor
{
    private Dictionary<string, IActorRef> _stations = new Dictionary<string, IActorRef>();

    public WeatherCollectorActor()
    {
        Receive<RegisterStation>(OnRegisterStation);
        Receive<ReportWeatherReading>(OnReportWeatherReading);
        Receive<ReportStationMalfunction>(OnStationMalfunction);
    }

    private void OnRegisterStation(RegisterStation msg)
    {
        _stations[msg.StationId] = Sender;
        Console.WriteLine($"Stazione registrata: ID={msg.StationId}, Location={msg.Location}");
    }

    private void OnReportWeatherReading(ReportWeatherReading msg)
    {
        Console.WriteLine($"Lettura ricevuta da {msg.StationId}: Temp={msg.Temperature}, Press={msg.Pressure}");

        if (msg.Temperature > 50 || msg.Temperature < -30)
        {
            Console.WriteLine($"Malfunzionamento rilevato nella stazione {msg.StationId}, richiedo riavvio.");
            Sender.Tell(new RestartStation());
        }
    }

    private void OnStationMalfunction(ReportStationMalfunction msg)
    {
        Console.WriteLine($"Stazione {msg.StationId} in malfunzionamento, richiedo riavvio.");
        if (_stations.TryGetValue(msg.StationId, out var station))
        {
            station.Tell(new RestartStation());
        }
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
