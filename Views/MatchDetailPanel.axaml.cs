using Avalonia.Controls;
using qiyana.Models.MatchData;
using qiyana.ViewModels;

namespace qiyana.Views;

public partial class MatchDetailPanel : UserControl
{
    public MatchDetailPanel()
    {
        InitializeComponent();
    }

    private void OnScoreboardPlayerNameClicked(object? sender, Participant p)
    {
        if (!string.IsNullOrEmpty(p.Puuid)
            && DataContext is MatchHistoryViewModel vm)
            vm.NavigateToPlayerCommand.Execute(p.Puuid);
    }
}
