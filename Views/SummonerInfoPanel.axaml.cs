using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using qiyana.Models;

namespace qiyana.Views;

public partial class SummonerInfoPanel : UserControl
{
    public SummonerInfoPanel()
    {
        InitializeComponent();
    }

    private void OnCopyRiotIdPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is SummonerInfo summoner)
        {
            var text = $"{summoner.GameName}#{summoner.TagLine}";
            TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text);
        }
    }

    private void OnCopyPuuidPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is SummonerInfo summoner)
        {
            TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(summoner.Puuid);
        }
    }
}
