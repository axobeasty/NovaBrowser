using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NovaBrowser.ViewModels;
using Windows.System;

namespace NovaBrowser.Controls;

public sealed partial class FindBar : UserControl
{
    public MainPageViewModel? ViewModel { get; set; }

    public event EventHandler? CloseRequested;

    public FindBar() => InitializeComponent();

    public void FocusQuery() => QueryBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);

    private void OnFindNextClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => ApplyFind(forward: true);

    private void OnFindPreviousClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => ApplyFind(forward: false);

    private void OnCloseClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void OnQueryKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            ApplyFind(forward: !e.KeyStatus.IsMenuKeyDown);
            e.Handled = true;
        }
    }

    private void ApplyFind(bool forward)
    {
        if (ViewModel is null)
        {
            return;
        }

        ViewModel.FindQuery = QueryBox.Text;
        ViewModel.ApplyFindQuery(forward);
    }
}
