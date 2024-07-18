using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using AvaloniaControls.Controls;
using MSUScripter.Services;
using MSUScripter.Views;

namespace MSUScripter;

public partial class App : Application
{
    public static MainWindow MainWindow = null!;
    
    public static string Version
    {
        get
        {
            var version = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location); 
            return (version.ProductVersion ?? "").Split("+")[0];
        }
    }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Current!.RequestedThemeVariant = SettingsService.Instance.Settings.DarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;

            var mainWindow = new MainWindow();
            MessageWindow.GlobalParentWindow = mainWindow;
            desktop.MainWindow = MainWindow = mainWindow;
        }
        
        base.OnFrameworkInitializationCompleted();
    }
}