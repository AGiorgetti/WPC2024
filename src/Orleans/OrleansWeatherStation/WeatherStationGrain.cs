namespace OrleansWeatherStation;
using Orleans;
using Orleans.EventSourcing;
using Orleans.Providers;
using Orleans.Runtime;
using OrleansShared;
using System;
using System.Threading;
using System.Threading.Tasks;

// todo: Orleans built-in JournaledGrain is not really well suited to implement a domain object.
//       Checking the invariants of the state might be hard.
//       We should:
//       - execute the command (some validation might be done here)
//       - attempt to change the aggregate state.
//       - check the invariant
//       - if the invariant is not satisfied, revert the state and throw an exception.
//       - if the invariant is satisfied, persist the event and apply it to the state.

// EventSourcing: https://learn.microsoft.com/en-us/dotnet/orleans/grains/event-sourcing/event-sourcing-configuration
// Built-in log consistency providers: https://learn.microsoft.com/en-us/dotnet/orleans/grains/event-sourcing/log-consistency-providers

//[StorageProvider(ProviderName = "MongoDbStorageState")] // db stati
//[LogConsistencyProvider(ProviderName = "StateStorage")] // periste lo stato
[StorageProvider(ProviderName = "MongoDbStorageEvents")] // db eventi
[LogConsistencyProvider(ProviderName = "LogStorage")] // persiste eventi
public class WeatherStationGrain : JournaledGrain<WeatherStationState, IWeatherStationEvent>, IWeatherStationGrain, IRemindable
{
    private IWeatherCollectorGrain? _collector;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var collectorGrainId = "collector"; // Assuming a single collector grain for simplicity
        _collector = GrainFactory.GetGrain<IWeatherCollectorGrain>(collectorGrainId);

        // using a Reminder instead of a Timer, so if the actor crashes for whatever reason
        // it will be activated again when the request to perform the new reading arrives
        await this.RegisterOrUpdateReminder(
                    "PerformReading",                       // Reminder name
                    TimeSpan.FromSeconds(5),            // Initial delay
                    TimeSpan.FromSeconds(10));           // Periodic interval

        await base.OnActivateAsync(cancellationToken);
    }

    public async Task StartStation(string location)
    {
        var stationId = this.GetPrimaryKeyString();
        RaiseEvent(new StationStarted(stationId, location));
        await ConfirmEvents(); // wait for events to be written and state updated

        await _collector!.RegisterStation(State.StationId!, State.Location!);
        Console.WriteLine($"[{State.StationId}] Registrato nel collector.");
    }

    public async Task RestartStation()
    {
        RaiseEvent(new StationRestarted());
        await ConfirmEvents();

        Console.WriteLine($"[{State.StationId}] Riavviato su richiesta del collector.");
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
            await ConfirmEvents();

            Console.WriteLine($"[{State.StationId}] Malfunzionamento rilevato.");
            // awaiting this call might lead to deadlock, the Collector grain will attempt to "restart" the station
            // we need to manage reentrancy
            // (see: https://learn.microsoft.com/en-us/dotnet/orleans/grains/request-scheduling)
            using var scope = RequestContext.AllowCallChainReentrancy();
            await _collector!.ReportStationMalfunction(State.StationId!);
            return;
        }
        // Genera una lettura casuale
        else
        {
            var reading = GenerateRandomReading();
            RaiseEvent(reading);
            await ConfirmEvents();

            Console.WriteLine($"[{State.StationId}] Invio lettura: Temp={reading.Temperature}, Press={reading.Pressure}");
            // awaiting this call might lead to deadlock, the Collector grain will attempt to "restart" the station
            // we need to manage reentrancy
            // (see: https://learn.microsoft.com/en-us/dotnet/orleans/grains/request-scheduling)
            using var scope = RequestContext.AllowCallChainReentrancy();
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

    public Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (reminderName == "PerformReading")
        {
            return PerformReading();
        }
        return Task.CompletedTask;
    }
}
