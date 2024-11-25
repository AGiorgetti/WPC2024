namespace OrleansWeatherStation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Serialization;
using OrleansShared;
using System;
using System.Net;
using System.Threading.Tasks;

public class WeatherStationHost
{
    public static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: WeatherStationHost <StationId> <Location>");
            return;
        }

        var stationId = args[0];
        var location = args[1];

        var host = new HostBuilder()
            .UseOrleans(clientBuilder =>
            {
                clientBuilder
                    .UseLocalhostClustering(siloPort: 11112, gatewayPort: 30001, primarySiloEndpoint: new IPEndPoint(IPAddress.Loopback, 11111))
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "WeatherStationService";
                    })
                    .ConfigureLogging(logging => logging.AddConsole())
                    //.AddMemoryGrainStorage("weatherStationStorage")
                    .UseMongoDBClient("mongodb://localhost")
                    .UseInMemoryReminderService() // could have been persisted in MongoDB
                    .Configure<ReminderOptions>(options =>
                    {
                        options.MinimumReminderPeriod = TimeSpan.FromSeconds(1);
                    })
                    .AddMongoDBGrainStorage("MongoDbStorageState", options =>
                    {
                        options.DatabaseName = "OrleansWeatherStation-Storage";
                        options.CollectionPrefix = "State";
                    })
                    .AddMongoDBGrainStorage("MongoDbStorageEvents", options =>
                    {
                        options.DatabaseName = "OrleansWeatherStation-Storage";
                        options.CollectionPrefix = "Events";
                    })
                    .AddLogStorageBasedLogConsistencyProvider("LogStorage")
                    .AddStateStorageBasedLogConsistencyProvider("StateStorage")
                    //.AddCustomStorageBasedLogConsistencyProviderAsDefault()
                    .Services
                        .Configure<JsonGrainStateSerializerOptions>(options =>
                        {
                            options.ConfigureJsonSerializerSettings = settings =>
                            {
                                settings.NullValueHandling = NullValueHandling.Include;
                                settings.DefaultValueHandling = DefaultValueHandling.Populate;
                                settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                            };
                        })
                        // configure custom serializer to avoid placing [GenerateSerializerAttribute] on all the messages
                        .AddSerializer(serializerBuilder =>
                        {
                            serializerBuilder.AddJsonSerializer(
                                isSupported: type => type.Namespace!.StartsWith("OrleansShared")
                                    || type.Namespace.StartsWith("OrleansWeatherStation"));
                        })
                    ;
            })
            .Build();

        await host.StartAsync();

        var client = host.Services.GetRequiredService<IClusterClient>();

        await Task.Delay(1000); // Wait 1 second to ensure the cluster client is fully initialized

        var weatherStation = client.GetGrain<IWeatherStationGrain>(stationId);
        await weatherStation.StartStation(location);

        Console.WriteLine("Premi ENTER per uscire...");
        Console.ReadLine();

        await host.StopAsync();
    }
}