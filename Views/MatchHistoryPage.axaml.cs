using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using qiyana.Models;
using qiyana.ViewModels;

namespace qiyana.Views;

public partial class MatchHistoryPage : UserControl
{
    public MatchHistoryPage()
    {
        InitializeComponent();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MatchHistoryViewModel vm)
            vm.SearchCommand.Execute(null);
    }

    private void OnSearchBoxGotFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MatchHistoryViewModel vm)
            vm.ShowHistory = true;
    }

    private void OnSearchBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MatchHistoryViewModel vm)
            vm.ShowHistory = false;
    }

    private void OnSearchBoxPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MatchHistoryViewModel vm)
            vm.ShowHistory = true;
    }

    private void OnHistoryItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border { DataContext: MatchHistorySearch search }
            && DataContext is MatchHistoryViewModel vm)
        {
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
                vm.CloseHistoryItemCommand.Execute(search);
            else
            {
                vm.SelectHistoryCommand.Execute(search);
                this.Focus();
            }
        }
    }

    private void OnTabPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border { DataContext: MatchHistoryTab tab }
            && DataContext is MatchHistoryViewModel vm)
            vm.SelectTabCommand.Execute(tab);
    }
}
