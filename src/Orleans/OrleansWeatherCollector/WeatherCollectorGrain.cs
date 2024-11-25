namespace OrleansWeatherCollector;
using Orleans;
using OrleansShared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class WeatherCollectorGrain : Grain, IWeatherCollectorGrain
{
    private readonly List<string> _registeredStations = new List<string>();

    public Task RegisterStation(string stationId, string location)
    {
        _registeredStations.Add(stationId);
        Console.WriteLine($"Stazione registrata: ID={stationId}, Location={location}");
        return Task.CompletedTask;
    }

    public async Task ReportStationMalfunction(string stationId)
    {
        Console.WriteLine($"Stazione {stationId} in malfunzionamento, richiedo riavvio.");

        var station = GrainFactory.GetGrain<IWeatherStationGrain>(stationId);
        await station.RestartStation();
    }

    public async Task ReportWeatherReading(string stationId, double temperature, double pressure)
    {
        Console.WriteLine($"Lettura ricevuta da {stationId}: Temp={temperature}, Press={pressure}");
        
        if (temperature > 50 || temperature < -30)
        {
            Console.WriteLine($"Malfunzionamento rilevato nella stazione {stationId}, richiedo riavvio.");

            var station = GrainFactory.GetGrain<IWeatherStationGrain>(stationId);
            await station.RestartStation();
        }
    }
}
