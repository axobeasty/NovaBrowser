using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NovaBrowser.Helpers;
using NovaBrowser.Models;

namespace NovaBrowser.Services;

public sealed class UpdateCoordinator
{
    private readonly GitHubUpdateService _updateService = new();
    private DispatcherQueue? _dispatcherQueue;
    private DispatcherQueueTimer? _backgroundTimer;
    private bool _isBusy;
    private bool _isBackgroundChecking;
    private bool _startupPromptShown;

    public bool HasPendingUpdate { get; private set; }

    public UpdateCheckResult? PendingUpdate { get; private set; }

    public event EventHandler? UpdateAvailabilityChanged;

    public void StartBackgroundMonitoring(DispatcherQueue dispatcherQueue)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueue);

        _dispatcherQueue = dispatcherQueue;
        StopBackgroundMonitoring();

        _ = RunInitialBackgroundCheckAsync();

        _backgroundTimer = dispatcherQueue.CreateTimer();
        _backgroundTimer.Interval = UpdateSettings.BackgroundCheckInterval;
        _backgroundTimer.Tick += OnBackgroundTimerTick;
        _backgroundTimer.Start();
    }

    public void StopBackgroundMonitoring()
    {
        if (_backgroundTimer is null)
        {
            return;
        }

        _backgroundTimer.Tick -= OnBackgroundTimerTick;
        _backgroundTimer.Stop();
        _backgroundTimer = null;
    }

    public async Task CheckManuallyAsync(XamlRoot? xamlRoot = null)
    {
        if (_isBusy)
        {
            return;
        }

        var dialogRoot = DialogHost.ResolveXamlRoot(xamlRoot);
        if (dialogRoot is null)
        {
            return;
        }

        if (HasPendingUpdate && PendingUpdate?.Update is not null)
        {
            await ShowUpdateAvailableDialogAsync(dialogRoot, PendingUpdate, silent: false);
            return;
        }

        _isBusy = true;

        try
        {
            var result = await _updateService.CheckForUpdatesAsync().ConfigureAwait(true);
            ApplyBackgroundCheckResult(result);

            switch (result.Status)
            {
                case UpdateCheckStatus.UpToDate:
                    await ShowInfoDialogAsync(
                        dialogRoot,
                        L.Get("UpdatesTitle"),
                        L.Format("UpdatesUpToDate", AppVersionService.CurrentVersionLabel));
                    break;

                case UpdateCheckStatus.UpdateAvailable when result.Update is not null:
                    await ShowUpdateAvailableDialogAsync(dialogRoot, result, silent: false);
                    break;

                default:
                    await ShowInfoDialogAsync(
                        dialogRoot,
                        L.Get("UpdatesCheckFailed"),
                        result.ErrorMessage ?? L.Get("UpdatesCheckFailedMessage"));
                    break;
            }
        }
        catch (Exception ex)
        {
            await ShowInfoDialogAsync(
                dialogRoot,
                L.Get("UpdatesCheckFailed"),
                ex.Message);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task RunInitialBackgroundCheckAsync()
    {
        await Task.Delay(UpdateSettings.InitialCheckDelay);
        await CheckInBackgroundAsync(showPromptWhenAvailable: true);
    }

    private async void OnBackgroundTimerTick(DispatcherQueueTimer sender, object args) =>
        await CheckInBackgroundAsync(showPromptWhenAvailable: false);

    private async Task CheckInBackgroundAsync(bool showPromptWhenAvailable)
    {
        if (_isBackgroundChecking || _isBusy)
        {
            return;
        }

        _isBackgroundChecking = true;

        try
        {
            var result = await _updateService.CheckForUpdatesAsync().ConfigureAwait(false);
            ApplyBackgroundCheckResult(result);

            if (showPromptWhenAvailable &&
                !_startupPromptShown &&
                result.Status == UpdateCheckStatus.UpdateAvailable &&
                result.Update is not null)
            {
                _startupPromptShown = true;
                QueueStartupUpdatePrompt(result);
            }
        }
        catch
        {
            // Background checks fail silently; manual check still reports errors.
        }
        finally
        {
            _isBackgroundChecking = false;
        }
    }

    private void QueueStartupUpdatePrompt(UpdateCheckResult result)
    {
        _dispatcherQueue?.TryEnqueue(() => _ = ShowStartupUpdatePromptAsync(result));
    }

    private async Task ShowStartupUpdatePromptAsync(UpdateCheckResult result)
    {
        if (_isBusy)
        {
            return;
        }

        var dialogRoot = DialogHost.ResolveXamlRoot();
        if (dialogRoot is null)
        {
            return;
        }

        await ShowUpdateAvailableDialogAsync(dialogRoot, result, silent: true);
    }

    private void ApplyBackgroundCheckResult(UpdateCheckResult result)
    {
        var hadPending = HasPendingUpdate;
        var previousVersion = PendingUpdate?.Update?.Version;

        switch (result.Status)
        {
            case UpdateCheckStatus.UpdateAvailable when result.Update is not null:
                PendingUpdate = result;
                HasPendingUpdate = true;
                break;

            case UpdateCheckStatus.UpToDate:
                PendingUpdate = null;
                HasPendingUpdate = false;
                break;
        }

        if (hadPending != HasPendingUpdate || PendingUpdate?.Update?.Version != previousVersion)
        {
            UpdateAvailabilityChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task ShowUpdateAvailableDialogAsync(
        XamlRoot xamlRoot,
        UpdateCheckResult result,
        bool silent)
    {
        var update = result.Update!;
        var title = silent ? L.Get("UpdateAvailableSilent") : L.Get("UpdateAvailableManual");
        var content = new ScrollViewer
        {
            MaxHeight = 240,
            Content = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Text = L.Format("UpdateCurrentVersion", result.CurrentVersion) + "\n" +
                       L.Format("UpdateNewVersion", update.Version) + "\n\n" +
                       update.ReleaseNotes,
            },
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = L.Get("UpdateButton"),
            SecondaryButtonText = L.Get("LaterButton"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        await DownloadAndInstallAsync(xamlRoot, update);
    }

    private async Task DownloadAndInstallAsync(XamlRoot xamlRoot, UpdateInfo update)
    {
        var progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 1,
            Value = 0,
            IsIndeterminate = false,
        };

        var statusText = new TextBlock
        {
            Text = L.Format("DownloadProgress", update.AssetName),
            TextWrapping = TextWrapping.Wrap,
        };

        var progressPanel = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                statusText,
                progressBar,
            },
        };

        var progressDialog = new ContentDialog
        {
            Title = L.Get("DownloadTitle"),
            Content = progressPanel,
            CloseButtonText = L.Get("Cancel"),
            XamlRoot = xamlRoot,
        };

        var progress = new Progress<double>(value => progressBar.Value = value);
        var dialogOperation = progressDialog.ShowAsync();

        try
        {
            var downloadResult = await _updateService.DownloadUpdateAsync(update, progress).ConfigureAwait(true);

            progressDialog.Hide();
            await dialogOperation;

            if (!downloadResult.Succeeded || string.IsNullOrWhiteSpace(downloadResult.PackagePath))
            {
                await ShowInfoDialogAsync(
                    xamlRoot,
                    L.Get("DownloadFailed"),
                    downloadResult.ErrorMessage ?? L.Get("DownloadFailedMessage"));
                return;
            }

            var confirmDialog = new ContentDialog
            {
                Title = L.Get("InstallTitle"),
                Content = L.Get("InstallConfirm"),
                PrimaryButtonText = L.Get("RestartButton"),
                CloseButtonText = L.Get("Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot,
            };

            if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary)
            {
                File.Delete(downloadResult.PackagePath);
                return;
            }

            UpdateInstallerService.ScheduleInstall(downloadResult.PackagePath);
        }
        catch (Exception ex)
        {
            progressDialog.Hide();
            await ShowInfoDialogAsync(
                xamlRoot,
                L.Get("DownloadFailed"),
                ex.Message);
        }
    }

    private static async Task ShowInfoDialogAsync(XamlRoot xamlRoot, string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
            },
            CloseButtonText = L.Get("OkButton"),
            XamlRoot = xamlRoot,
        };

        await dialog.ShowAsync();
    }
}
