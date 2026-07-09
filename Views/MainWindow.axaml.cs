using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using qiyana.ViewModels;

namespace qiyana.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnSplitterPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ToggleNavCollapseCommand.Execute(null);
        }
    }

    private void OnSplitterPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border border && border.Child is Rectangle rect)
        {
            rect.Fill = new SolidColorBrush(Color.Parse("#c084fc"));
            rect.Width = 2;
        }
    }

    private void OnSplitterPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Border border && border.Child is Rectangle rect)
        {
            rect.Fill = Brushes.Transparent;
            rect.Width = 1;
        }
    }

    private void OnWebsitePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://yhswa.icu/lol") { UseShellExecute = true });
        e.Handled = true;
    }

    private void OnGithubPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/cloud-wa/qiyana") { UseShellExecute = true });
        e.Handled = true;
    }
}
