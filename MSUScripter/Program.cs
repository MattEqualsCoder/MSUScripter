using Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MSURandomizerLibrary;
using MSUScripter.Controls;
using MSUScripter.Services;
using Win32RenderingMode = Avalonia.Win32RenderingMode;

namespace MSUScripter;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            if (_serviceProvider != null)
            {
                var logger = _serviceProvider?.GetRequiredService<ILogger<Program>>();
                logger?.LogCritical(ex, "[CRASH] Uncaught {ExceptionType}: ", ex.GetType().Name);
            }

            using var source = new CancellationTokenSource();
            ShowExceptionPopup().ContinueWith(t => source.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.UIThread.MainLoop(source.Token);
        }
    }
    
    private static async Task ShowExceptionPopup()
    {
        /*var response = await MessageBoxManager.GetMessageBoxStandard("Error",
            "A critical error has occurred. Please open an issue at\n" +
            "https://github.com/MattEqualsCoder/MSUScripter/issues.\n" +
            "Press Yes to open the log directory.",
            ButtonEnum.YesNo, Icon.Error).ShowWindowDialogAsync(App._mainWindow);*/

        var window = new MessageWindow("A critical error has occurred. Please open an issue at\n" +
                                               "https://github.com/MattEqualsCoder/MSUScripter/issues.\n" +
                                               "Press Yes to open the log directory.", MessageWindowType.YesNo, "Error");
        
        window.Closing += (sender, args) =>
        {
            if (window.Result != MessageWindowResult.Yes) return;
            var logFileLocation = Environment.ExpandEnvironmentVariables(GetLogLocation());
            var startInfo = new ProcessStartInfo
            {
                Arguments = logFileLocation, 
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
        };
        
        window.Show();

        await Dispatcher.UIThread.Invoke(async () =>
        {
            while (window.IsVisible)
            {
                await Task.Delay(500);
            }
        });
        
        
    }
    
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions() { RenderingMode = new List<Win32RenderingMode>() { Win32RenderingMode.Software }  })
            .WithInterFont()
            .LogToTrace();
    }
        

    private static IServiceProvider? _serviceProvider;
    
    public static IServiceProvider GetServiceProvider()
    {
        if (_serviceProvider != null) return _serviceProvider;
        
        _serviceProvider =  new ServiceCollection()
#if DEBUG
            .AddLogging(x => x.AddDebug())
#else
            .AddLogging(logging =>
            {
                logging.AddFile($"{GetLogLocation()}{Path.DirectorySeparatorChar}msu-scripter-{DateTime.UtcNow:yyyyMMdd}.log", options =>
                {
                    options.Append = true;
                    options.FileSizeLimitBytes = 52428800;
                    options.MaxRollingFiles = 5;
                });
            })
#endif
            .AddMsuRandomizerServices()
            .AddSingleton<SettingsService>()
            .AddSingleton(serviceProvider => serviceProvider.GetRequiredService<SettingsService>().Settings)
            .AddSingleton<MsuPcmService>()
            .AddSingleton<AudioService>()
            .AddSingleton<AudioMetadataService>()
            .AddSingleton<ConverterService>()
            .AddSingleton<AudioAnalysisService>()
            .AddSingleton<MainWindow>()
            .AddSingleton<ProjectService>()
            .AddSingleton<PyMusicLooperService>()
            .AddSingleton<MainWindow>()
            .AddTransient<NewProjectPanel>()
            .AddTransient<EditProjectPanel>()
            .AddTransient<MsuTrackInfoPanel>()
            .AddTransient<MsuPcmGenerationWindow>()
            .AddTransient<AudioControl>()
            .AddTransient<SettingsWindow>()
            .AddTransient<AudioAnalysisWindow>()
            .BuildServiceProvider();
        
        return _serviceProvider;
    }

    public static string GetLogLocation()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"%LocalAppData%{Path.DirectorySeparatorChar}MSUScripter";
        }
        else
        {
            return "logs";
        }
    }

    public static string GetBaseFolder()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(Environment.ExpandEnvironmentVariables("%localappdata%"), "MSUScripter");
        }
        else
        {
            return AppContext.BaseDirectory;
        }
    }
    
}