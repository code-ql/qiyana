using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using qiyana.Models;
using qiyana.ViewModels;

namespace qiyana.Views;

public partial class InGamePage : UserControl
{
    public InGamePage()
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


}
