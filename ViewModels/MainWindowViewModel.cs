using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using qiyana.Models;
using qiyana.Services;

namespace qiyana.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private string? _lastSummonerPuuid;

    public ObservableCollection<NavItem> NavItems { get; } =
    [
        new("点击重连", "\U0001f534", typeof(DashboardViewModel)),
        new("战绩查询", "\U0001f4ca", typeof(MatchHistoryViewModel)),
        new("对局开始", "\u2694\ufe0f", typeof(LiveGameViewModel)),
        new("游戏中", "\U0001f3ae", typeof(InGameViewModel)),
        new("工具", "\U0001f527", typeof(ToolsViewModel)),
        new("设置", "\u2699\ufe0f", typeof(SettingsViewModel))
    ];

    [ObservableProperty]
    private NavItem? _selectedNavItem;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SidebarWidth))]
    private bool _isNavCollapsed;

    public GridLength SidebarWidth => IsNavCollapsed ? new GridLength(0) : new GridLength(200);

    private NavItem? _previousNavItem;

    [RelayCommand]
    private void ToggleNavCollapse()
    {
        IsNavCollapsed = !IsNavCollapsed;
    }

    private void NavigateToPage(Type viewModelType)
    {
        if (!_pageCache.TryGetValue(viewModelType, out var vm))
        {
            vm = (ViewModelBase)Activator.CreateInstance(viewModelType)!;
            _pageCache[viewModelType] = vm;
        }
        CurrentPage = vm;
    }

    private readonly Dictionary<Type, ViewModelBase> _pageCache = new();

    partial void OnSelectedNavItemChanged(NavItem? value)
    {
        if (value is null) return;

        if (value.ViewModelType == typeof(DashboardViewModel))
        {
            if (NavItems[0].Label != "点击重连")
            {
                _previousNavItem = value;
                NavigateToPage(typeof(DashboardViewModel));
            }
            else
            {
                SelectedNavItem = _previousNavItem;
                var connected = App.LcuDiscovery.GetLcuInfo() is not null;
                if (connected)
                {
                    NavItems[0].Label = "已连接";
                    NavItems[0].IconText = "";
                    _wasConnected = true;
                    _ = TryLoadCurrentSummonerAsync();
                    NavigateToPage(typeof(DashboardViewModel));
                }
            }
            return;
        }

        _previousNavItem = value;
        NavigateToPage(value.ViewModelType);
    }

    public void NavigateToMatchHistory(string summonerName)
    {
        if (!_pageCache.TryGetValue(typeof(MatchHistoryViewModel), out var cached))
        {
            cached = new MatchHistoryViewModel();
            _pageCache[typeof(MatchHistoryViewModel)] = cached;
        }

        var vm = (MatchHistoryViewModel)cached;
        vm.SearchText = summonerName;
        vm.SearchCommand.Execute(null);
        SelectedNavItem = NavItems[1];
    }

    private bool _wasConnected;

    public MainWindowViewModel()
    {
        SelectedNavItem = NavItems[0];
        var connected = App.LcuDiscovery.GetLcuInfo() is not null;
        _wasConnected = connected;
        if (connected)
        {
            NavItems[0].Label = "已连接";
            NavItems[0].IconText = "";
            _ = TryLoadCurrentSummonerAsync();
        }
        _ = PollLcuStatusAsync();

        App.LcuWebSocket.SummonerChanged += OnSummonerChanged;
    }

    private async void OnSummonerChanged(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object) return;

        var puuid = data.TryGetProperty("puuid", out var p) ? p.GetString() : null;
        if (string.IsNullOrEmpty(puuid)) return;

        if (puuid == _lastSummonerPuuid) return;

        _lastSummonerPuuid = puuid;

        var summonerName = data.TryGetProperty("gameName", out var gn) ? gn.GetString() : null;
        if (!string.IsNullOrEmpty(summonerName))
            NavItems[0].Label = summonerName;

        var profileIconId = data.TryGetProperty("profileIconId", out var pi) ? pi.GetInt32() : 0;
        var iconPath = profileIconId > 0
            ? $"/lol-game-data/assets/v1/profile-icons/{profileIconId}.jpg" : null;
        if (iconPath is not null)
            await App.GameData.LoadIconBytesAsync(iconPath);
        NavItems[0].ProfileIconPath = iconPath;
        NavItems[0].IconText = "";

        _pageCache.Clear();
        App.LcuApi.ClearGameDetailCache();
        LcuImageCache.Clear();

        if (App.SgpApi.IsSupported())
            _ = App.SgpApi.RefreshEntitlementTokenAsync();

        if (CurrentPage is not null)
        {
            var type = CurrentPage.GetType();
            NavigateToPage(type);
        }
    }

    private async Task PollLcuStatusAsync()
    {
        while (true)
        {
            await Task.Delay(5000);
            var connected = App.LcuDiscovery.GetLcuInfo() is not null;
            if (connected != _wasConnected)
            {
                _wasConnected = connected;
                if (connected)
                {
                    NavItems[0].Label = "已连接";
                    NavItems[0].IconText = "";
                    _ = TryLoadCurrentSummonerAsync();
                }
                else
                {
                    NavItems[0].Label = "点击重连";
                    NavItems[0].IconText = "\U0001f534";
                }
            }
            else if (connected && NavItems[0].Label == "已连接")
            {
                _ = TryLoadCurrentSummonerAsync();
            }
        }
    }

    private async Task TryLoadCurrentSummonerAsync()
    {
        try
        {
            var summoner = await App.LcuApi.GetCurrentSummonerAsync();
            if (summoner is not null)
            {
                var name = !string.IsNullOrEmpty(summoner.GameName)
                    ? summoner.GameName
                    : summoner.Name;
                NavItems[0].Label = name;
                var iconPath = summoner.ProfileIconPath;
                if (iconPath is not null)
                    await App.GameData.LoadIconBytesAsync(iconPath);
                NavItems[0].ProfileIconPath = iconPath;
                NavItems[0].IconText = "";
            }
        }
        catch
        {
            // Keep "已连接" as fallback
        }
    }
}
