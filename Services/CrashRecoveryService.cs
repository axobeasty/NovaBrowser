using NovaBrowser.Models;

namespace NovaBrowser.Services;

public sealed class CrashRecoveryService
{
    private readonly SessionService _sessionService;

    public CrashRecoveryService(SessionService sessionService) => _sessionService = sessionService;

    public bool HasPendingRecovery => _sessionService.LoadCrashRecovery()?.Tabs.Count > 0;

    public SessionSnapshot? GetPendingRecovery() => _sessionService.LoadCrashRecovery();

    public void MarkHealthyShutdown() => _sessionService.ClearCrashRecovery();

    public void MarkUncleanShutdown(IEnumerable<SessionTabEntry> tabs, int activeIndex) =>
        _sessionService.SaveCrashRecovery(tabs, activeIndex);
}
