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
using MSUScripter.Services;

namespace MSUScripter;

public partial class App : Application
{
    private ILogger<App>? _logger;
    public static MainWindow? _mainWindow;
    private IServiceProvider? _services;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
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
            _logger.LogInformation("Starting MSU Scripter {Version}", version.ProductVersion ?? "");
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        
            var settings = _services.GetRequiredService<SettingsService>().Settings;
            Current!.RequestedThemeVariant = settings.DarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
            
            var msuInitializationRequest = new MsuRandomizerInitializationRequest
            {
                MsuAppSettingsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MSUScripter.msu-randomizer-settings.yaml")
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                msuInitializationRequest.MsuTypeConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "Configs");
                msuInitializationRequest.UserOptionsPath = Path.Combine(Directory.GetCurrentDirectory(), "Settings", "msu-user-settings.yml");
            }

#if DEBUG
            msuInitializationRequest.MsuTypeConfigPath = GetConfigDirectory();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                msuInitializationRequest.UserOptionsPath = "%LocalAppData%\\MSUScripter\\msu-user-settings-debug.yml";
            }
            else
            {
                msuInitializationRequest.UserOptionsPath = Path.Combine(Directory.GetCurrentDirectory(), "Settings", "msu-user-settings-debug.yml");
            }
#endif
        
            _services.GetRequiredService<IMsuRandomizerInitializationService>().Initialize(msuInitializationRequest);
            _services.GetRequiredService<ConverterService>();
            Resources[typeof(IServiceProvider)] = _services;
            desktop.MainWindow = _mainWindow = _services?.GetRequiredService<MainWindow>();
        }
        
        base.OnFrameworkInitializationCompleted();
    }

    private async void DesktopOnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (_mainWindow == null) return;
        
        var hasPendingChanges = _mainWindow.CheckPendingChanges();
        if (!hasPendingChanges) return;
        
        e.Cancel = true;
            
        var window = new MessageWindow("You currently have unsaved changes. Do you want to save your changes?", MessageWindowType.YesNo, "MSU Scripter", _mainWindow);
        window.Show();

        window.OnButtonClick += (o, args) =>
        {
            var result = window.Result;
            if (result == MessageWindowResult.Yes)
            {
                _mainWindow.SaveChanges();
            }
        };
    }

#if DEBUG
    public string GetConfigDirectory()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null && !directory.GetFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }

        return directory != null ? Path.Combine(directory.FullName, "ConfigRepo", "resources") : "";
    }
#endif
    
    
}