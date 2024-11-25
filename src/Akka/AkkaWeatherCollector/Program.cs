using Akka.Actor;
using Akka.Configuration;

class Program
{
    static void Main(string[] args)
    {
        var config = ConfigurationFactory.ParseString(@"
            akka {
                actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                remote.dot-netty.tcp {
                    hostname = ""localhost""
                    port = 8081
                }
                persistence {
                    journal.plugin = ""akka.persistence.journal.mongodb""
                    journal.mongodb {
                        class = ""Akka.Persistence.MongoDb.Journal.MongoDbJournal, Akka.Persistence.MongoDb""
                        connection-string = ""mongodb://localhost:27017/AkkaCollector""
                        collection = ""CollectorJournal""
                        metadata-collection = ""CollectorMetadata""
                    }
                    snapshot-store.plugin = ""akka.persistence.snapshot-store.mongodb""
                    snapshot-store.mongodb {
                        class = ""Akka.Persistence.MongoDb.Snapshot.MongoDbSnapshotStore, Akka.Persistence.MongoDb""
                        connection-string = ""mongodb://localhost:27017/AkkaCollector""
                        collection = ""CollectorSnapshot""
                        metadata-collection = ""CollectorSnapshotMetadata""
                    }
                    query {
                        mongodb {
                            class = ""Akka.Persistence.MongoDb.Query.MongoDbReadJournalProvider, Akka.Persistence.MongoDb""
                            connection-string = ""mongodb://localhost:27017/AkkaCollector""
                            read-journal {
                                collection = ""CollectorJournal""
                            }
                        }
                    }
                }
            }
        ");

        using (var system = ActorSystem.Create("CollectorSystem", config))
        {
            var collector = system.ActorOf(Props.Create(() => new WeatherCollectorActor()), "collector");

            Console.WriteLine("Collector in esecuzione. Premi ENTER per uscire...");
            Console.ReadLine();
        }
    }
}
