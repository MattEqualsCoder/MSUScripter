using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using AppImageManager;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using AvaloniaControls.Controls;
using Microsoft.Extensions.DependencyInjection;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.Views;

namespace MSUScripter;

public class App : Application
{
    public static MainWindow MainWindow = null!;
    public const string AppId = "org.mattequalscoder.msuscripter";
    public const string AppName = "MSU Scripter";

    private static readonly string? VersionOverride = null;
    
    public static string Version
    {
        get
        {
            var version = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location); 
            return VersionOverride ?? (version.ProductVersion ?? "").Split("+")[0];
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
            var settings = Program.MainHost.Services.GetRequiredService<Settings>();
            Current!.RequestedThemeVariant = settings.DarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;

            var mainWindow = new MainWindow();
            MessageWindow.GlobalParentWindow = mainWindow;
            desktop.MainWindow = MainWindow = mainWindow;
        }
        
        base.OnFrameworkInitializationCompleted();
    }
    
    [SupportedOSPlatform("linux")]
    internal static CreateDesktopFileResponse BuildLinuxDesktopFile()
    {
        return new DesktopFileBuilder(AppId, AppName)
            .AddUninstallAction(Directories.BaseFolder)
            .WithMimeType("application/x-msu-scripter-project", "MSU Scripter Project", "*.msup", true)
            .Build();
    }
}