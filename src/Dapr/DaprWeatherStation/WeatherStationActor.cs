namespace DaprWeatherStation;

using System;
using System.Threading.Tasks;
using Dapr.Actors;
using Dapr.Actors.Runtime;
using DaprShared;
using NEventStore;

public class WeatherStationActor : EventSourcingActor<WeatherStationState>, IWeatherStationActor, IRemindable
{
    private IWeatherCollectorActor? _collector;
    
    public WeatherStationActor(ActorHost host, IStoreEvents store) : base(host, store)
    { }

    protected override async Task OnActivateAsync()
    {
        ReHydrate();

        var collectorId = new ActorId("collector"); // Assuming a single collector grain for simplicity
        _collector = ProxyFactory.CreateActorProxy<IWeatherCollectorActor>(collectorId, "WeatherCollectorActor");

        await RegisterReminderAsync(
            "PerformReading",                       // Reminder name
            null,
            TimeSpan.FromSeconds(5),            // Initial delay
            TimeSpan.FromSeconds(10));           // Periodic interval
    }

    public async Task StartStation(string location)
    {
        var stationId = Id.GetId();
        RaiseEvent(new StationStarted(stationId, location));

        await _collector!.RegisterStation(State.StationId!, State.Location!);
        Console.WriteLine($"[{State.StationId}] Registrato nel collector.");
    }

    public Task RestartStation()
    {
        RaiseEvent(new StationRestarted());

        Console.WriteLine($"[{State.StationId}] Riavviato su richiesta del collector.");
        return Task.CompletedTask;
    }

    public async Task PerformReading()
    {
        // generate a random reading or a malfunction event
        // events will be persisted, then sent to the collector

        // Simula un malfunzionamento casuale
        // L'hardware può fallire casualmente
        // - in modo critico (10% delle volte)
        var random = new Random();
        var value = random.Next(100);
        if (value <= 10)
        {
            throw new ApplicationException("Errore hardware");
        }
        // - in modo non critico (20% delle volte)
        else if (value <= 30)
        {
            RaiseEvent(new StationMalfunctionDetected());

            Console.WriteLine($"[{State.StationId}] Malfunzionamento rilevato.");
            // awaiting this call might lead to deadlock, the Collector grain will attempt to "restart" the station
            // we need to manage reentrancy
            // (see: https://docs.dapr.io/developing-applications/building-blocks/actors/actor-reentrancy/)
            // REENTRANCY DOES NOT WORK BETWEEN DIFFERENT APPLICATIONS
            _collector!.ReportStationMalfunction(State.StationId!);
            return;
        }
        // Genera una lettura casuale
        else
        {
            var reading = GenerateRandomReading();
            RaiseEvent(reading);

            Console.WriteLine($"[{State.StationId}] Invio lettura: Temp={reading.Temperature}, Press={reading.Pressure}");
            await _collector!.ReportWeatherReading(State.StationId!, reading.Temperature, reading.Pressure);
            Console.WriteLine($"[{State.StationId}] Lettura inviata: Temp={reading.Temperature}, Press={reading.Pressure}");
            return;
        }
    }

    private WeatherReadingDetected GenerateRandomReading()
    {
        var random = new Random();
        return new WeatherReadingDetected(
            (float)(random.NextDouble() * 40 - 10),
            (float)(random.NextDouble() * 20 + 980)
        );
    }

    public Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
    {
        if (reminderName == "PerformReading")
        {
            return PerformReading();
        }
        return Task.CompletedTask;
    }

    protected override void ApplyEvent(WeatherStationState state, dynamic @event)
    {
        state.Apply(@event);
    }
}
