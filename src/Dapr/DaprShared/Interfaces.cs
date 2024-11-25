namespace DaprShared;

using System.Threading.Tasks;
using Dapr.Actors;

public interface IWeatherStationActor : IActor
{
    Task StartStation(string location);
    Task RestartStation();
    Task PerformReading();
}

public interface IWeatherCollectorActor : IActor
{
    Task RegisterStation(string stationId, string location);
    Task ReportStationMalfunction(string stationId);
    Task ReportWeatherReading(string stationId, double temperature, double pressure);
}