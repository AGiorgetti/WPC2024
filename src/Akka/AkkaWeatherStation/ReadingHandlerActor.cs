using Akka.Actor;
using AkkaShared;

/// <summary>
/// Gestisce richieste or operazioni potenzialmente lunghe o problematiche.
/// Deleghiamo lavoro a questo attore per evitare di bloccare l'attore principale.
/// </summary>
public class ReadingHandlerActor : ReceiveActor
{
    private readonly string _stationId;
    public ReadingHandlerActor(string stationId)
    {
        _stationId = stationId;

        // Configura il ricevitore per gestire il messaggio di avvio
        Receive<ProcessReading>(msg => OnProcessReading());
    }

    protected override void PreRestart(Exception reason, object message)
    {
        Console.WriteLine($"[{_stationId}] ReadingHandlerActor in riavvio. {reason.Message}");
        if (message != null)
        {
            // By design the message that caused the issue is considered "processed",
            // there are configurations and techniques to handle this differently.
            // Here we just process the same message again after the actor is restarted.
            Self.Tell(message, Sender);
        }
        base.PreRestart(reason, message);
    }

    protected override void PostStop()
    {
        base.PostStop();
        // Logica di pulizia se necessaria
        Console.WriteLine($"[{_stationId}] ReadingHandlerActor terminato.");
    }

    private void OnProcessReading()
    {
        // Simula un ritardo o una operazione lunga
        // Thread.Sleep(500);

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
            Sender.Tell(new StationMalfunctionDetected());
            return;
        }
        // Genera una lettura casuale
        else
        {
            var reading = GenerateRandomReading();
            Sender.Tell(reading);
        }
        // Termina l'attore dopo aver completato il lavoro
        Context.Stop(Self);
    }

    private WeatherReadingDetected GenerateRandomReading()
    {
        var random = new Random();
        return new WeatherReadingDetected(
            (float)(random.NextDouble() * 40 - 10),
            (float)(random.NextDouble() * 20 + 980)
        );
    }
}

public class ProcessReading { }
