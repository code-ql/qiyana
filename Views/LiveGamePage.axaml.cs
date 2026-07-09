using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using qiyana.Models;
using qiyana.ViewModels;

namespace qiyana.Views;

public partial class LiveGamePage : UserControl
{
    public LiveGamePage()
    {
        InitializeComponent();
    }

    private void OnPlayerNamePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;

        if (sender is Border { Tag: TeammateDisplayInfo p }
            && TopLevel.GetTopLevel(this) is Window window
            && window.DataContext is MainWindowViewModel mainVM)
        {
            mainVM.NavigateToMatchHistory($"{p.SummonerName}#{p.TagLine}");
        }
    }

    private void OnAccordionHeaderPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is TeammateDisplayInfo clicked
            && DataContext is LiveGameViewModel vm)
        {
            foreach (var other in vm.OtherPlayers)
            {
                if (other != clicked)
                    other.IsExpanded = false;
            }
            clicked.IsExpanded = !clicked.IsExpanded;
        }
    }
}
