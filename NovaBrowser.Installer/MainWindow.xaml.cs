using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NovaBrowser.Installer.Models;
using NovaBrowser.Installer.Services;
using NovaBrowser.Setup.Common;
using NovaBrowser.Setup.Common.Models;
using System.Reflection;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace NovaBrowser.Installer;

public sealed partial class MainWindow : Window
{
    private enum SetupStep
    {
        Welcome,
        Options,
        Progress,
        Complete,
        UninstallConfirm,
        UninstallProgress,
        UninstallComplete,
    }

    private readonly InstallationService _installationService = new();
    private readonly InstallSettings _settings = new();
    private static Assembly HostAssembly => Assembly.GetExecutingAssembly();

    private SetupStep _step = SetupStep.Welcome;
    private bool _isBusy;
    private TextBlock? _statusText;
    private ProgressBar? _progressBar;

    public MainWindow()
    {
        InitializeComponent();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(760, 540));
        AppWindow.SetIcon("Assets/AppIcon.ico");

        ApplyUninstallPathOverride();
        ShowStep(App.IsUninstallMode ? SetupStep.UninstallConfirm : SetupStep.Welcome);
    }

    private void ApplyUninstallPathOverride()
    {
        if (!App.IsUninstallMode)
        {
            return;
        }

        var installPath = App.InstallPathOverride ?? InstallPaths.ReadInstalledPath();
        if (!string.IsNullOrWhiteSpace(installPath))
        {
            _settings.InstallPath = installPath;
        }
    }

    private void ShowStep(SetupStep step)
    {
        _step = step;
        StepHost.Children.Clear();
        BackButton.Visibility = step is SetupStep.Options ? Visibility.Visible : Visibility.Collapsed;

        switch (step)
        {
            case SetupStep.Welcome:
                HeaderTitle.Text = "NovaBrowser Setup";
                HeaderSubtitle.Text = $"Версия {EmbeddedSetupBundle.ReadBundleVersion(HostAssembly).ToString(3)}";
                FooterHint.Text = "Шаг 1 из 3";
                NextButton.Content = "Далее";
                NextButton.IsEnabled = EmbeddedSetupBundle.IsAvailable(HostAssembly);
                StepHost.Children.Add(CreateWelcomeView());
                break;

            case SetupStep.Options:
                HeaderTitle.Text = "Параметры установки";
                HeaderSubtitle.Text = "Выберите папку и ярлыки";
                FooterHint.Text = "Шаг 2 из 3";
                NextButton.Content = "Установить";
                NextButton.IsEnabled = true;
                StepHost.Children.Add(CreateOptionsView());
                break;

            case SetupStep.Progress:
                HeaderTitle.Text = "Установка";
                HeaderSubtitle.Text = "Копируем файлы...";
                FooterHint.Text = "Подождите";
                BackButton.Visibility = Visibility.Collapsed;
                NextButton.Visibility = Visibility.Collapsed;
                StepHost.Children.Add(CreateProgressView());
                _ = RunInstallAsync();
                break;

            case SetupStep.Complete:
                HeaderTitle.Text = "Готово";
                HeaderSubtitle.Text = "NovaBrowser установлен";
                FooterHint.Text = string.Empty;
                BackButton.Visibility = Visibility.Collapsed;
                NextButton.Content = "Закрыть";
                NextButton.Visibility = Visibility.Visible;
                NextButton.IsEnabled = true;
                StepHost.Children.Add(CreateCompleteView(success: true, message: $"NovaBrowser установлен в:\n{_settings.InstallPath}"));
                break;

            case SetupStep.UninstallConfirm:
                HeaderTitle.Text = "Удаление NovaBrowser";
                HeaderSubtitle.Text = "Подтвердите удаление";
                FooterHint.Text = string.Empty;
                BackButton.Visibility = Visibility.Collapsed;
                NextButton.Content = "Удалить";
                NextButton.IsEnabled = true;
                StepHost.Children.Add(CreateCompleteView(
                    success: false,
                    message: $"Будут удалены файлы из:\n{_settings.InstallPath}\n\nПродолжить?"));
                break;

            case SetupStep.UninstallProgress:
                HeaderTitle.Text = "Удаление";
                HeaderSubtitle.Text = "Очищаем систему...";
                FooterHint.Text = "Подождите";
                NextButton.Visibility = Visibility.Collapsed;
                StepHost.Children.Add(CreateProgressView());
                _ = RunUninstallAsync();
                break;

            case SetupStep.UninstallComplete:
                HeaderTitle.Text = "Удалено";
                HeaderSubtitle.Text = "NovaBrowser удалён";
                FooterHint.Text = string.Empty;
                NextButton.Content = "Закрыть";
                NextButton.Visibility = Visibility.Visible;
                StepHost.Children.Add(CreateCompleteView(success: true, message: "NovaBrowser успешно удалён с компьютера."));
                break;
        }
    }

    private UIElement CreateWelcomeView()
    {
        var version = EmbeddedSetupBundle.ReadBundleVersion(HostAssembly);
        var bundleOk = EmbeddedSetupBundle.IsAvailable(HostAssembly);
        var hasUninstaller = EmbeddedSetupBundle.BundleContainsUninstaller(HostAssembly);

        return new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "Добро пожаловать в мастер установки NovaBrowser",
                    FontSize = 20,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                },
                new TextBlock
                {
                    Text = "Современный браузер на WinUI 3 и WebView2 с вкладками, автообновлением и фирменным интерфейсом.",
                    Opacity = 0.82,
                    TextWrapping = TextWrapping.Wrap,
                },
                new TextBlock
                {
                    Text = bundleOk
                        ? $"Будет установлена версия {version.ToString(3)}."
                        : "Ошибка: в Setup.exe не найден встроенный архив. Соберите установщик через build-installer.ps1.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = bundleOk
                        ? (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
                        : new SolidColorBrush(Microsoft.UI.Colors.OrangeRed),
                },
                new TextBlock
                {
                    Text = hasUninstaller
                        ? "В комплект входит NovaBrowser.Uninstall.exe."
                        : "Предупреждение: в архиве нет деинсталлятора.",
                    Opacity = 0.75,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = hasUninstaller
                        ? (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
                        : new SolidColorBrush(Microsoft.UI.Colors.OrangeRed),
                },
            },
        };
    }

    private UIElement CreateOptionsView()
    {
        var pathBox = new TextBox
        {
            Text = _settings.InstallPath,
            PlaceholderText = "Папка установки",
            CornerRadius = new CornerRadius(10),
        };

        var browseButton = new Button
        {
            Content = "Обзор...",
            Style = (Style)Application.Current.Resources["NovaSecondaryButtonStyle"],
            Margin = new Thickness(8, 0, 0, 0),
        };

        browseButton.Click += async (_, _) =>
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
            };
            picker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder is not null)
            {
                pathBox.Text = folder.Path;
            }
        };

        var pathRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto },
            },
            Children =
            {
                pathBox,
                browseButton,
            },
        };

        Grid.SetColumn(pathBox, 0);
        Grid.SetColumn(browseButton, 1);

        var desktopCheck = new CheckBox
        {
            Content = "Создать ярлык на рабочем столе",
            IsChecked = _settings.CreateDesktopShortcut,
        };
        var startMenuCheck = new CheckBox
        {
            Content = "Добавить в меню «Пуск»",
            IsChecked = _settings.CreateStartMenuShortcut,
        };
        var launchCheck = new CheckBox
        {
            Content = "Запустить NovaBrowser после установки",
            IsChecked = _settings.LaunchAfterInstall,
        };

        pathBox.TextChanged += (_, _) => _settings.InstallPath = pathBox.Text.Trim();
        desktopCheck.Checked += (_, _) => _settings.CreateDesktopShortcut = true;
        desktopCheck.Unchecked += (_, _) => _settings.CreateDesktopShortcut = false;
        startMenuCheck.Checked += (_, _) => _settings.CreateStartMenuShortcut = true;
        startMenuCheck.Unchecked += (_, _) => _settings.CreateStartMenuShortcut = false;
        launchCheck.Checked += (_, _) => _settings.LaunchAfterInstall = true;
        launchCheck.Unchecked += (_, _) => _settings.LaunchAfterInstall = false;

        return new StackPanel
        {
            Spacing = 18,
            Children =
            {
                new TextBlock
                {
                    Text = "Папка установки",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                },
                pathRow,
                desktopCheck,
                startMenuCheck,
                launchCheck,
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

    private UIElement CreateCompleteView(bool success, string message)
    {
        return new StackPanel
        {
            Spacing = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new FontIcon
                {
                    Glyph = success ? "\uE73E" : "\uE783",
                    FontSize = 42,
                    Foreground = (Brush)Application.Current.Resources["NovaAccentBrush"],
                },
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 16,
                },
            },
        };
    }

    private async void OnNextClick(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        switch (_step)
        {
            case SetupStep.Welcome:
                ShowStep(SetupStep.Options);
                break;

            case SetupStep.Options:
                if (string.IsNullOrWhiteSpace(_settings.InstallPath))
                {
                    await ShowDialogAsync("Ошибка", "Укажите папку установки.");
                    return;
                }

                ShowStep(SetupStep.Progress);
                break;

            case SetupStep.Complete:
            case SetupStep.UninstallComplete:
                Close();
                break;

            case SetupStep.UninstallConfirm:
                ShowStep(SetupStep.UninstallProgress);
                break;
        }
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (_step == SetupStep.Options)
        {
            ShowStep(SetupStep.Welcome);
        }
    }

    private async Task RunInstallAsync()
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

        var result = await _installationService.InstallAsync(_settings, progress);
        _isBusy = false;

        if (!result.Succeeded)
        {
            await ShowDialogAsync("Ошибка установки", result.ErrorMessage ?? "Неизвестная ошибка.");
            ShowStep(SetupStep.Options);
            NextButton.Visibility = Visibility.Visible;
            return;
        }

        ShowStep(SetupStep.Complete);
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

        var result = await _installationService.UninstallAsync(_settings.InstallPath, progress);
        _isBusy = false;

        if (!result.Succeeded)
        {
            await ShowDialogAsync("Ошибка удаления", result.ErrorMessage ?? "Неизвестная ошибка.");
            Close();
            return;
        }

        ShowStep(SetupStep.UninstallComplete);
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
