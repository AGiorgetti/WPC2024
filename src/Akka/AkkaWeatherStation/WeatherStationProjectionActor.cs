using Akka.Actor;
using Akka.Persistence.Query;
using Akka.Persistence.MongoDb.Query;
using Akka.Streams;
using MongoDB.Driver;
using AkkaShared;

public class WeatherStationProjectionActor : ReceiveActor
{
    private readonly IMongoCollection<WeatherStationReadModel> _readModelCollection;

    public WeatherStationProjectionActor()
    {
        var system = Context.System;
        var _materializer = system.Materializer();

        // Configura il read journal
        var readJournal = PersistenceQuery.Get(system)
            .ReadJournalFor<MongoDbReadJournal>("akka.persistence.query.mongodb");

        // Sottoscrivi agli eventi per tag
        var eventsByTag = readJournal.EventsByTag(WeatherStationTagger.Tag, Offset.NoOffset());

        eventsByTag
            .RunForeach(envelope => ProcessEvent(envelope.PersistenceId, envelope.Event), _materializer)
            .PipeTo(Self);

        // Inizializza la collezione del read model
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("AkkaWeatherData");
        _readModelCollection = database.GetCollection<WeatherStationReadModel>("WeatherStation");
    }

    private void ProcessEvent(string persistenceId, object evt)
    {
        switch (evt)
        {
            case StationStarted stationStarted:
                _readModelCollection.ReplaceOne(
                    Builders<WeatherStationReadModel>.Filter.Eq(rm => rm.Id, persistenceId),
                    new WeatherStationReadModel
                    {
                        Id = persistenceId,
                        StationId = stationStarted.StationId,
                        Location = stationStarted.Location,
                        Readings = new List<WeatherReadingDetected>(),
                        IsMalfunctioning = false
                    },
                    new ReplaceOptions { IsUpsert = true }
                );
                break;
            case WeatherReadingDetected weatherReadingDetected:
                _readModelCollection.UpdateOne(
                    Builders<WeatherStationReadModel>.Filter.Eq(rm => rm.Id, persistenceId),
                    Builders<WeatherStationReadModel>.Update.Push(rm => rm.Readings, weatherReadingDetected)
                );
                break;
            case StationMalfunctionDetected stationMalfunctionDetected:
                _readModelCollection.UpdateOne(
                    Builders<WeatherStationReadModel>.Filter.Eq(rm => rm.Id, persistenceId),
                    Builders<WeatherStationReadModel>.Update.Set(rm => rm.IsMalfunctioning, true)
                );
                break;
            case StationRestarted stationRestarted:
                _readModelCollection.UpdateOne(
                    Builders<WeatherStationReadModel>.Filter.Eq(rm => rm.Id, persistenceId),
                    Builders<WeatherStationReadModel>.Update.Set(rm => rm.IsMalfunctioning, false)
                );
                break;
        }
    }
}
