using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MSURandomizerLibrary.Models;
using MSURandomizerLibrary.Services;
using MSUScripter.Controls;
using MSUScripter.Models;
using MSUScripter.Services;

namespace MSUScripter;

public partial class App : Application
{
    private ILogger<App>? _logger;
    public static MainWindow? MainWindow { get; private set; }
    private IServiceProvider? _services;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static string GetAppVersion()
    {
        var version = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location); 
        return (version.ProductVersion ?? "").Split("+")[0];
    }
    
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            _logger?.LogCritical(ex, "[CRASH] Uncaught {ExceptionType}: ", ex.GetType().Name);
        else
            _logger?.LogCritical("Unhandled exception in current domain but exception object is not an exception ({Obj})", e.ExceptionObject);
    }
    

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _services = Program.GetServiceProvider();
            _logger = _services.GetRequiredService<ILogger<App>>();
            
            desktop.ShutdownRequested += DesktopOnShutdownRequested;
        
            _logger.LogInformation("Assembly Location: {Location}", Assembly.GetExecutingAssembly().Location);
            var version = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location);
            _logger.LogInformation("Starting MSU Scripter {Version}", GetAppVersion());
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        
            var settings = _services.GetRequiredService<SettingsService>().Settings;
            Current!.RequestedThemeVariant = settings.DarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
            
            var msuInitializationRequest = new MsuRandomizerInitializationRequest
            {
                MsuAppSettingsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MSUScripter.msu-randomizer-settings.yaml"),
                UserOptionsPath = Path.Combine(Directories.BaseFolder, "msu-user-settings.yml")
            };

#if DEBUG
            msuInitializationRequest.UserOptionsPath = Path.Combine(Directories.BaseFolder, "msu-user-settings-debug.yml");
#endif
        
            _services.GetRequiredService<IMsuRandomizerInitializationService>().Initialize(msuInitializationRequest);
            _services.GetRequiredService<ConverterService>();
            Resources[typeof(IServiceProvider)] = _services;
            desktop.MainWindow = MainWindow = _services?.GetRequiredService<MainWindow>();
        }
        
        base.OnFrameworkInitializationCompleted();
    }

    private void DesktopOnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (MainWindow == null) return;
        
        var hasPendingChanges = MainWindow.CheckPendingChanges();
        if (!hasPendingChanges) return;
        
        e.Cancel = true;
            
        var window = new MessageWindow("You currently have unsaved changes. Do you want to save your changes?", MessageWindowType.YesNo, "MSU Scripter", MainWindow);
        window.Show();

        window.OnButtonClick += (o, args) =>
        {
            var result = window.Result;
            if (result == MessageWindowResult.Yes)
            {
                MainWindow.SaveChanges();
            }
        };
    }

}