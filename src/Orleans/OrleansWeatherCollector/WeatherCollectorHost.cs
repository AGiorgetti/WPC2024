namespace OrleansWeatherCollector;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Serialization;
using System.Threading.Tasks;

public class WeatherCollectorHost
{
    public static async Task Main(string[] args)
    {
        var host = new HostBuilder()
            .UseOrleans(builder =>
            {
                builder
                    .UseLocalhostClustering(siloPort: 11111, gatewayPort: 30000)
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
                    .AddMongoDBGrainStorageAsDefault(options =>
                    {
                        options.DatabaseName = "OrleansWeatherCollector-Storage";
                    })
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
                                isSupported: type => type.Namespace!.StartsWith("OrleansShared"));
                        })
                    ;
            })
            .Build();

        await host.RunAsync();
    }
}
