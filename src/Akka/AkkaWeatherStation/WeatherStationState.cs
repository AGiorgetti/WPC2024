using AkkaShared;

public class WeatherStationState
{
    public string? StationId { get; set; }
    public string? Location { get; set; }
    public List<WeatherReadingDetected> Readings { get; set; } = new List<WeatherReadingDetected>();
    public bool IsMalfunctioning { get; set; } = false;

    public void CheckInvariants() {
        if (string.IsNullOrEmpty(StationId)) {
            throw new InvalidOperationException("StationId must be set");
        }
        if (string.IsNullOrEmpty(Location)) {
            throw new InvalidOperationException("Location must be set");
        }
    }

    public WeatherStationState Apply(StationStarted evt)
    {
        StationId = evt.StationId;
        Location = evt.Location;
        return this;
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
}
