using Dapr.Actors;
using Dapr.Actors.Client;
using DaprShared;
using DaprWeatherStation;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using NEventStore;
using NEventStore.Persistence.MongoDB;
using NEventStore.Serialization;
using Shared;

var builder = WebApplication.CreateBuilder(args);

// Configure MongoDb serialization
BsonSerializer.RegisterSerializer(new ObjectSerializer(ObjectSerializer.AllAllowedTypes));
// Customize NEventStore serialization
BsonClassMap.RegisterClassMap(delegate (BsonClassMap<MongoCommit> cm) {
    cm.AutoMap();
    cm.MapMember((MongoCommit c) => c.Headers).SetSerializer(new ImpliedImplementationInterfaceSerializer<IDictionary<string, object>, Dictionary<string, object>>().WithImplementationSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<string, object>>(DictionaryRepresentation.Document)));
    cm.MapMember((MongoCommit c) => c.CommitStamp).SetSerializer(DateTimeSerializer.UtcInstance);
    cm.SetIgnoreExtraElements(ignoreExtraElements: true);
});
MongoDbAutomapper.Automap(AppDomain.CurrentDomain.BaseDirectory, null);

builder.Services.AddActors(options =>
{
    // Register actor types and configure actor settings
    options.Actors.RegisterActor<WeatherStationActor>();
    options.ReentrancyConfig = new Dapr.Actors.ActorReentrancyConfig()
    {
        Enabled = true,
        MaxStackDepth = 32,
    };
});

builder.Services.AddSingleton<IStoreEvents>(
    (sp) => Wireup.Init()
        .UsingMongoPersistence("mongodb://localhost:27017/DaprEventStore", new DocumentObjectSerializer())
        .InitializeStorageEngine()
        .Build()
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // By default, ASP.Net Core uses port 5000 for HTTP. The HTTP
    // redirection will interfere with the Dapr runtime. You can
    // move this out of the else block if you use port 5001 in this
    // example, and developer tooling (such as the VSCode extension).
    app.UseHttpsRedirection();
}

app.MapActorsHandlers();

app.Lifetime.ApplicationStarted.Register(async () =>
{
   // read settings from appsettings.json
   var settings = app.Configuration.GetSection("WeatherStationSettings").Get<WeatherStationSettings>();
   if (settings == null)
   {
       throw new InvalidOperationException("WeatherStationSettings not found in configuration");
   }

   var proxyFactory = app.Services.GetRequiredService<IActorProxyFactory>();
   var actorProxy = proxyFactory.CreateActorProxy<IWeatherStationActor>(new ActorId(settings.StationId), "WeatherStationActor");
   await actorProxy.StartStation(settings.Location);
});

app.Run();
