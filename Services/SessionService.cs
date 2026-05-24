using NovaBrowser.Models;

namespace NovaBrowser.Services;

public sealed class SessionService
{
    private const string SessionFile = "session.json";
    private const string CrashFile = "crash-session.json";

    private readonly DataStoreService _store;

    public SessionService(DataStoreService store) => _store = store;

    public void SaveSession(IEnumerable<SessionTabEntry> tabs, int activeIndex)
    {
        _store.Save(SessionFile, new SessionSnapshot
        {
            Tabs = tabs.ToList(),
            ActiveIndex = activeIndex,
            SavedAt = DateTime.UtcNow,
        });
    }

    public SessionSnapshot? LoadSession() =>
        _store.Load<SessionSnapshot?>(SessionFile, null);

    public void SaveCrashRecovery(IEnumerable<SessionTabEntry> tabs, int activeIndex) =>
        _store.Save(CrashFile, new SessionSnapshot
        {
            Tabs = tabs.ToList(),
            ActiveIndex = activeIndex,
            SavedAt = DateTime.UtcNow,
        });

    public SessionSnapshot? LoadCrashRecovery() =>
        _store.Load<SessionSnapshot?>(CrashFile, null);

    public void ClearCrashRecovery() => _store.Delete(CrashFile);

    public void ClearSession() => _store.Delete(SessionFile);
}

public sealed class SessionSnapshot
{
    public List<SessionTabEntry> Tabs { get; set; } = [];
    public int ActiveIndex { get; set; }
    public DateTime SavedAt { get; set; }
}
