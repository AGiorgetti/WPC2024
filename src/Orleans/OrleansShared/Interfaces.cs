namespace OrleansShared;

using Orleans;
using System.Threading.Tasks;

public interface IWeatherStationGrain : IGrainWithStringKey
{
    Task StartStation(string location);
    Task RestartStation();
    Task PerformReading();
}

public interface IWeatherCollectorGrain : IGrainWithStringKey
{
    Task RegisterStation(string stationId, string location);
    Task ReportStationMalfunction(string stationId);
    Task ReportWeatherReading(string stationId, double temperature, double pressure);
}