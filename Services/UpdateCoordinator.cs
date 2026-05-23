using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NovaBrowser.Models;

namespace NovaBrowser.Services;

public sealed class UpdateCoordinator
{
    private readonly GitHubUpdateService _updateService = new();
    private bool _isBusy;

    public async Task CheckSilentlyOnStartupAsync(XamlRoot xamlRoot)
    {
        await Task.Delay(TimeSpan.FromSeconds(3));

        var result = await _updateService.CheckForUpdatesAsync();
        if (result.Status == UpdateCheckStatus.UpdateAvailable && result.Update is not null)
        {
            await ShowUpdateAvailableDialogAsync(xamlRoot, result, silent: true);
        }
    }

    public async Task CheckManuallyAsync(XamlRoot xamlRoot)
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;

        try
        {
            var result = await _updateService.CheckForUpdatesAsync();

            switch (result.Status)
            {
                case UpdateCheckStatus.UpToDate:
                    await ShowInfoDialogAsync(
                        xamlRoot,
                        "Обновления",
                        $"У вас установлена последняя версия NovaBrowser ({AppVersionService.CurrentVersionLabel}).");
                    break;

                case UpdateCheckStatus.UpdateAvailable when result.Update is not null:
                    await ShowUpdateAvailableDialogAsync(xamlRoot, result, silent: false);
                    break;

                default:
                    await ShowInfoDialogAsync(
                        xamlRoot,
                        "Ошибка обновления",
                        result.ErrorMessage ?? "Не удалось проверить обновления.");
                    break;
            }
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task ShowUpdateAvailableDialogAsync(
        XamlRoot xamlRoot,
        UpdateCheckResult result,
        bool silent)
    {
        var update = result.Update!;
        var title = silent ? "Доступно обновление NovaBrowser" : "Найдено обновление";
        var content = new ScrollViewer
        {
            MaxHeight = 240,
            Content = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Text = $"Текущая версия: {result.CurrentVersion}\n" +
                       $"Новая версия: {update.Version}\n\n" +
                       update.ReleaseNotes,
            },
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "Обновить",
            SecondaryButtonText = "Позже",
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
            Text = $"Скачивание {update.AssetName}...",
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
            Title = "Загрузка обновления",
            Content = progressPanel,
            XamlRoot = xamlRoot,
        };

        var progress = new Progress<double>(value => progressBar.Value = value);
        var dialogOperation = progressDialog.ShowAsync();

        var downloadResult = await _updateService.DownloadUpdateAsync(update, progress);

        progressDialog.Hide();
        await dialogOperation;

        if (!downloadResult.Succeeded || string.IsNullOrWhiteSpace(downloadResult.PackagePath))
        {
            await ShowInfoDialogAsync(
                xamlRoot,
                "Ошибка загрузки",
                downloadResult.ErrorMessage ?? "Не удалось скачать обновление.");
            return;
        }

        var confirmDialog = new ContentDialog
        {
            Title = "Установка обновления",
            Content = "NovaBrowser будет закрыт и перезапущен для установки обновления.",
            PrimaryButtonText = "Перезапустить",
            CloseButtonText = "Отмена",
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
            CloseButtonText = "OK",
            XamlRoot = xamlRoot,
        };

        await dialog.ShowAsync();
    }
}
