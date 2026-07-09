using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using qiyana.ViewModels;

namespace qiyana.Views;

public partial class MatchHistoryList : UserControl
{
    public MatchHistoryList()
    {
        InitializeComponent();
    }

    private ListBox? _matchListBox;
    private bool _scrollSubscribed;

    private void MatchListBox_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is ListBox listBox)
        {
            _matchListBox = listBox;
            TrySubscribeScrollViewer();
        }
    }

    private void TrySubscribeScrollViewer()
    {
        if (_scrollSubscribed) return;
        if (_matchListBox is null) return;

        var sv = _matchListBox.FindDescendantOfType<ScrollViewer>();
        if (sv is null)
        {
            _matchListBox.LayoutUpdated += RetrySubscribeOnLayoutUpdated;
            return;
        }

        sv.ScrollChanged += OnScrollChanged;
        _scrollSubscribed = true;
    }

    private void RetrySubscribeOnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_matchListBox is not null)
            _matchListBox.LayoutUpdated -= RetrySubscribeOnLayoutUpdated;
        TrySubscribeScrollViewer();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;
        if (e.ExtentDelta.Y == 0 && e.ViewportDelta.Y == 0 && e.OffsetDelta.Y == 0) return;
        if (sv.Extent.Height <= sv.Viewport.Height) return;
        if (sv.Extent.Height - sv.Offset.Y - sv.Viewport.Height >= 100) return;
        if (DataContext is not MatchHistoryViewModel vm) return;
        if (vm.CurrentTab?.LoadMoreAction is null || vm.CurrentTab.IsLoadingMore || vm.CurrentTab.IsLoading) return;

        _ = vm.CurrentTab.LoadMoreAction.Invoke();
    }
}
