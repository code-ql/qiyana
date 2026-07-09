using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using qiyana.Models.MatchData;
using qiyana.ViewModels;

namespace qiyana.Views;

public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
    }

    private void MatchListBox_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is ListBox listBox)
        {
            var scrollViewer = listBox.FindDescendantOfType<ScrollViewer>();
            if (scrollViewer is not null)
                scrollViewer.ScrollChanged += OnScrollChanged;
        }
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer sv
            && sv.Extent.Height > sv.Viewport.Height
            && sv.Extent.Height - sv.Offset.Y - sv.Viewport.Height < 100
            && DataContext is DashboardViewModel vm)
            vm.LoadMoreCommand.Execute(null);
    }

    private void OnScoreboardPlayerNameClicked(object? sender, Participant p)
    {
        if (!string.IsNullOrEmpty(p.SummonerFullName)
            && TopLevel.GetTopLevel(this) is Window window
            && window.DataContext is MainWindowViewModel mainVM)
        {
            mainVM.NavigateToMatchHistory(p.SummonerFullName);
        }
    }
}
