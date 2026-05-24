using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NovaBrowser.Setup.Common;
using NovaBrowser.Setup.Common.Models;

namespace NovaBrowser.Uninstaller;

public sealed partial class MainWindow : Window
{
    private enum UninstallStep
    {
        Confirm,
        Progress,
        Complete,
    }

    private readonly string _installPath;
    private UninstallStep _step = UninstallStep.Confirm;
    private bool _isBusy;
    private TextBlock? _statusText;
    private ProgressBar? _progressBar;

    public MainWindow(string installPath)
    {
        _installPath = string.IsNullOrWhiteSpace(installPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NovaBrowser")
            : installPath;

        InitializeComponent();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(760, 540));
        AppWindow.SetIcon("Assets/AppIcon.ico");

        var version = Directory.Exists(_installPath)
            ? InstallPaths.ReadInstalledVersion(_installPath)
            : new Version(0, 4, 0);

        HeaderSubtitle.Text = $"Версия {version.ToString(3)}";
        ShowStep(UninstallStep.Confirm);
    }

    private void ShowStep(UninstallStep step)
    {
        _step = step;
        StepHost.Children.Clear();

        switch (step)
        {
            case UninstallStep.Confirm:
                HeaderTitle.Text = "Удаление NovaBrowser";
                HeaderSubtitle.Text = $"Папка: {_installPath}";
                FooterHint.Text = string.Empty;
                NextButton.Content = "Удалить";
                NextButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Visible;
                NextButton.IsEnabled = true;
                StepHost.Children.Add(CreateConfirmView());
                break;

            case UninstallStep.Progress:
                HeaderTitle.Text = "Удаление";
                HeaderSubtitle.Text = "Очищаем систему...";
                FooterHint.Text = "Подождите";
                NextButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Collapsed;
                StepHost.Children.Add(CreateProgressView());
                _ = RunUninstallAsync();
                break;

            case UninstallStep.Complete:
                HeaderTitle.Text = "Удалено";
                HeaderSubtitle.Text = "NovaBrowser удалён";
                FooterHint.Text = string.Empty;
                NextButton.Content = "Закрыть";
                NextButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Collapsed;
                NextButton.IsEnabled = true;
                StepHost.Children.Add(CreateCompleteView());
                break;
        }
    }

    private UIElement CreateConfirmView()
    {
        return new StackPanel
        {
            Spacing = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new FontIcon
                {
                    Glyph = "\uE783",
                    FontSize = 42,
                    Foreground = (Brush)Application.Current.Resources["NovaAccentBrush"],
                },
                new TextBlock
                {
                    Text = "Будут удалены все файлы NovaBrowser (включая данные WebView2 и настройки), ярлыки и запись в «Приложениях».\nПосле закрытия окна оставшиеся файлы будут удалены автоматически.",
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 16,
                },
                new TextBlock
                {
                    Text = _installPath,
                    Opacity = 0.75,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                },
            },
        };
    }

    private UIElement CreateProgressView()
    {
        _progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 1,
            Value = 0,
            Height = 8,
        };

        _statusText = new TextBlock
        {
            Text = "Подготовка...",
            Opacity = 0.8,
            TextWrapping = TextWrapping.Wrap,
        };

        return new StackPanel
        {
            Spacing = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new ProgressRing { Width = 48, Height = 48, IsActive = true },
                _progressBar,
                _statusText,
            },
        };
    }

    private UIElement CreateCompleteView()
    {
        return new StackPanel
        {
            Spacing = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new FontIcon
                {
                    Glyph = "\uE73E",
                    FontSize = 42,
                    Foreground = (Brush)Application.Current.Resources["NovaAccentBrush"],
                },
                new TextBlock
                {
                    Text = "NovaBrowser удалён. Если окно только что закрылось, подождите несколько секунд — папка установки исчезнет автоматически.",
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 16,
                },
            },
        };
    }

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        switch (_step)
        {
            case UninstallStep.Confirm:
                ShowStep(UninstallStep.Progress);
                break;

            case UninstallStep.Complete:
                Close();
                Environment.Exit(0);
                break;
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        if (!_isBusy)
        {
            Close();
        }
    }

    private async Task RunUninstallAsync()
    {
        _isBusy = true;
        var progress = new Progress<InstallProgressReport>(report =>
        {
            if (_progressBar is not null)
            {
                _progressBar.Value = report.Progress;
            }

            if (_statusText is not null)
            {
                _statusText.Text = report.Status;
            }
        });

        var result = await UninstallService.UninstallAsync(_installPath, progress);
        _isBusy = false;

        if (!result.Succeeded)
        {
            await ShowDialogAsync("Ошибка удаления", result.ErrorMessage ?? "Неизвестная ошибка.");
            Close();
            return;
        }

        ShowStep(UninstallStep.Complete);
    }

    private async Task ShowDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };

        await dialog.ShowAsync();
    }
}
