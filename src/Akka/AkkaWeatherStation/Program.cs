// File: Program.cs (Stazione Meteo)

using Akka.Actor;
using Akka.Configuration;
using AkkaShared;
using System;

class Program
{
    static async Task Main(string[] args)
    {
        var stationId = args.Length > 0 ? args[0] : "station-1";
        var location = args.Length > 1 ? args[1] : "Unknown Location";

        var config = ConfigurationFactory.ParseString(@"
            akka {
                actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                remote.dot-netty.tcp {
                    hostname = ""localhost""
                    port = 0
                }
                persistence {
                    journal {
                        plugin = ""akka.persistence.journal.mongodb""
                        mongodb {
                            class = ""Akka.Persistence.MongoDb.Journal.MongoDbJournal, Akka.Persistence.MongoDb""
                            connection-string = ""mongodb://localhost:27017/AkkaWeatherStation""
                            collection = ""WeatherStationJournal""
                            metadata-collection = ""WeatherStationMetadata""
                            event-adapters = {
                                weather-station-tagger = ""WeatherStationTagger, AkkaWeatherStation""
                            }
                            event-adapter-bindings = {
                                ""AkkaShared.IWeatherStationEvent, AkkaShared"" = weather-station-tagger
                            }
                            use-write-transaction = off
                        }
                    }
                    snapshot-store {
                        plugin = ""akka.persistence.snapshot-store.mongodb""
                        mongodb {
                            class = ""Akka.Persistence.MongoDb.Snapshot.MongoDbSnapshotStore, Akka.Persistence.MongoDb""
                            connection-string = ""mongodb://localhost:27017/AkkaWeatherStation""
                            collection = ""WeatherStationSnapshot""
                            metadata-collection = ""WeatherStationSnapshotMetadata""
                            use-write-transaction = off
                        }
                    }
                    query {
                        mongodb {
                            class = ""Akka.Persistence.MongoDb.Query.MongoDbReadJournalProvider, Akka.Persistence.MongoDb""
                            connection-string = ""mongodb://localhost:27017/AkkaWeatherStation""
                            read-journal {
                                collection = ""WeatherStationJournal""
                            }
                        }
                    }
                }
            }
        ");

        using (var system = ActorSystem.Create("WeatherStationSystem", config))
        {
            // start the projection actor
            var projectionActor = system.ActorOf(Props.Create(() => new WeatherStationProjectionActor()), "weather-station-projectionActor");

            // Resolving and storing a remote actor reference is not a good practice in production code
            // But this is just a demo :D
            var collectorAddress = Address.Parse("akka.tcp://CollectorSystem@localhost:8081");
            var collectorSelection = system.ActorSelection(new RootActorPath(collectorAddress) / "user" / "collector");
            var collector = await collectorSelection.ResolveOne(TimeSpan.FromSeconds(3));

            var weatherStation = system.ActorOf(Props.Create(() => new WeatherStationActor(stationId, location, collector)), $"weather-station-{stationId}");

            weatherStation.Tell(new StartStation());

            Console.WriteLine("Premi ENTER per uscire...");
            Console.ReadLine();
        }
    }
}
