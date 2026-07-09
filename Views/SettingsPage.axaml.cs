using System;
using Avalonia.Controls;
using qiyana.Models.GameData;
using qiyana.ViewModels;

namespace qiyana.Views;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (PickChampionAutoComplete is not null)
        {
            PickChampionAutoComplete.FilterMode = AutoCompleteFilterMode.Custom;
            PickChampionAutoComplete.ItemFilter = FilterChampion;
            PickChampionAutoComplete.SelectionChanged += OnPickSelectionChanged;
            PickChampionAutoComplete.TextChanged += (_, _) => PickChampionAutoComplete.IsDropDownOpen = true;
        }
        if (BanChampionAutoComplete is not null)
        {
            BanChampionAutoComplete.FilterMode = AutoCompleteFilterMode.Custom;
            BanChampionAutoComplete.ItemFilter = FilterChampion;
            BanChampionAutoComplete.SelectionChanged += OnBanSelectionChanged;
            BanChampionAutoComplete.TextChanged += (_, _) => BanChampionAutoComplete.IsDropDownOpen = true;
        }
    }

    private static bool FilterChampion(string? search, object? item)
    {
        if (string.IsNullOrWhiteSpace(search) || item is not ChampionSummary c)
            return true;
        return c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               c.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               c.Alias.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private void OnPickSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is ChampionSummary c && DataContext is SettingsViewModel vm)
        {
            vm.AddPickChampion(c.Id);
            PickChampionAutoComplete.SelectedItem = null;
            PickChampionAutoComplete.Text = "";
        }
    }

    private void OnBanSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is ChampionSummary c && DataContext is SettingsViewModel vm)
        {
            vm.AddBanChampion(c.Id);
            BanChampionAutoComplete.SelectedItem = null;
            BanChampionAutoComplete.Text = "";
        }
    }

    private void OnPickRemoveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ChampionSelectItem item } && DataContext is SettingsViewModel vm)
            vm.RemovePickChampion(item.Id);
    }

    private void OnBanRemoveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ChampionSelectItem item } && DataContext is SettingsViewModel vm)
            vm.RemoveBanChampion(item.Id);
    }
}
