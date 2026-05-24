namespace NovaBrowser.Services;

public sealed class TelemetryService
{
    private const int MaxEvents = 200;
    private readonly DataStoreService _store;
    private TelemetrySettings _settings = new();

    public TelemetryService(DataStoreService store)
    {
        _store = store;
        _settings = store.Load("telemetry.json", new TelemetrySettings());
    }

    public bool IsEnabled
    {
        get => _settings.IsEnabled;
        set
        {
            _settings.IsEnabled = value;
            Save();
        }
    }

    public IReadOnlyList<string> RecentEvents => _settings.RecentEvents;

    public void TrackEvent(string name, string? detail = null)
    {
        if (!_settings.IsEnabled)
        {
            return;
        }

        var line = $"{DateTime.UtcNow:O} | {name} | {detail ?? string.Empty}".Trim();
        _settings.RecentEvents.Insert(0, line);
        if (_settings.RecentEvents.Count > MaxEvents)
        {
            _settings.RecentEvents = _settings.RecentEvents.Take(MaxEvents).ToList();
        }

        Save();
    }

    public void TrackCrash(string message) => TrackEvent("crash", message);

    private void Save() => _store.Save("telemetry.json", _settings);
}

public sealed class TelemetrySettings
{
    public bool IsEnabled { get; set; }
    public List<string> RecentEvents { get; set; } = [];
}
