using DaprShared;

namespace DaprWeatherStation;

public class WeatherStationState : EventSourcingActorState
{
    public string? StationId { get; set; }
    public string? Location { get; set; }
    public bool IsMalfunctioning { get; set; }
    public List<WeatherReadingDetected> Readings { get; set; } = new List<WeatherReadingDetected>();

    public WeatherStationState Apply(StationStarted evt)
    {
        StationId = evt.StationId;
        Location = evt.Location;
        return this;
    }

    public override void CheckInvariants()
    {
        if (string.IsNullOrEmpty(StationId))
        {
            throw new InvalidOperationException("StationId must be set");
        }
        if (string.IsNullOrEmpty(Location))
        {
            throw new InvalidOperationException("Location must be set");
        }
    }

    public WeatherStationState Apply(StationRestarted evt)
    {
        IsMalfunctioning = false;
        return this;
    }

    public WeatherStationState Apply(WeatherReadingDetected evt)
    {
        Readings.Add(evt);
        return this;
    }

    public WeatherStationState Apply(StationMalfunctionDetected evt)
    {
        IsMalfunctioning = true;
        return this;
    }

    public override object Clone()
    {
        return new WeatherStationState
        {
            StationId = StationId,
            Location = Location,
            IsMalfunctioning = IsMalfunctioning,
            Readings = new List<WeatherReadingDetected>(Readings)
        };
    }
}