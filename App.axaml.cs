using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using qiyana.Services;
using qiyana.ViewModels;
using qiyana.Views;

namespace qiyana;

public partial class App : Application
{
    public static ILcuDiscoveryService LcuDiscovery { get; } = new LcuDiscoveryService();
    public static LcuApiClient LcuApi { get; } = new(LcuDiscovery);
    public static GameDataService GameData { get; } = new(LcuApi);
    public static SgpApiClient SgpApi { get; } = new(LcuApi);
    public static LcuWebSocketClient LcuWebSocket { get; } = new(LcuDiscovery);
    public static LiveGameDataClient LiveGameData { get; } = new();
    public static SettingsService Settings { get; } = new();
    public static AgnesAiTeamReviewService AgnesAiTeamReview { get; } = new();
    public static AutoSelectService AutoSelect { get; } = new(LcuWebSocket, LcuApi, Settings);

    public static bool IsSgpSupported { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        _ = InitializeSgpAsync();
        _ = InitializeWebSocketAsync();
        _ = App.GameData.LoadAsync();
        App.AutoSelect.Start();

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task InitializeSgpAsync()
    {
        // Always subscribe regardless of current LCU state,
        // so SGP re-init works when LCU starts later or account switches.
        LcuWebSocket.EntitlementTokenUpdated += token =>
            SgpApi.UpdateEntitlementToken(token);

        LcuWebSocket.Reconnected += async () =>
            await TryInitializeSgpAsync();

        await TryInitializeSgpAsync();
    }

    private static async Task TryInitializeSgpAsync()
    {
        var lcuInfo = LcuDiscovery.GetLcuInfo();
        if (lcuInfo is null)
        {
            IsSgpSupported = false;
            return;
        }

        IsSgpSupported = await SgpApi.TryInitializeAsync(lcuInfo);
    }

    private static async Task InitializeWebSocketAsync()
    {
        try
        {
            await LcuWebSocket.StartAsync();
        }
        catch
        {
            // LCU not available
        }
    }
}
