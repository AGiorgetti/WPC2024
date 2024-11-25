using Akka.Persistence.Journal;
using AkkaShared;
using System.Collections.Immutable;

/// <summary>
/// Adapter used to add tags to events before they are written to the journal.
/// It makes easier to query events by tag and project them to a read model.
/// Adapters are configured in Hocon configuration.
/// </summary>
public class WeatherStationTagger : IWriteEventAdapter
{
    public const string Tag = "weather-station";
    private IImmutableSet<string> Tags = [Tag];

    public string Manifest(object evt)
    {
        return string.Empty;
    }

    public object ToJournal(object evt)
    {
        switch (evt)
        {
            case StationStarted stationStarted:
                return new Tagged(stationStarted, Tags);
            case WeatherReadingDetected weatherReadingDetected:
                return new Tagged(weatherReadingDetected, Tags);
            case StationMalfunctionDetected stationMalfunctionDetected:
                return new Tagged(stationMalfunctionDetected, Tags);
            case StationRestarted stationRestarted:
                return new Tagged(stationRestarted, Tags);
            default:
                return evt;
        }
    }
}
