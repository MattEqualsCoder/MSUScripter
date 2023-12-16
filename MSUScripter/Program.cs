using Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using GitHubReleaseChecker;
using Microsoft.Extensions.DependencyInjection;
using MSURandomizerLibrary;
using MSUScripter.Controls;
using MSUScripter.Models;
using MSUScripter.Services;
using Serilog;
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
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.File(LogPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
            .WriteTo.Debug()
            .CreateLogger();
        
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"[CRASH] Uncaught {ex.GetType().Name}: ");
            using var source = new CancellationTokenSource();
            ShowExceptionPopup().ContinueWith(t => source.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.UIThread.MainLoop(source.Token);
        }
    }
    
    private static async Task ShowExceptionPopup()
    {
        var window = new MessageWindow("A critical error has occurred. Please open an issue at\n" +
                                               "https://github.com/MattEqualsCoder/MSUScripter/issues.\n" +
                                               "Press Yes to open the log directory.", MessageWindowType.YesNo, "Error");
        
        window.Closing += (sender, args) =>
        {
            if (window.Result != MessageWindowResult.Yes) return;
            var logFileLocation = Directories.LogFolder;
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
            .With(new X11PlatformOptions() { UseDBusFilePicker = false })
            .WithInterFont()
            .LogToTrace();
    }

    private static IServiceProvider? _serviceProvider;
    
    public static IServiceProvider GetServiceProvider()
    {
        if (_serviceProvider != null) return _serviceProvider;

        var serviceCollection = new ServiceCollection()
            .AddLogging(logging =>
            {
                logging.AddSerilog(dispose: true);
            })
            .AddMsuRandomizerServices()
            .AddGitHubReleaseCheckerServices()
            .AddSingleton<SettingsService>()
            .AddSingleton(serviceProvider => serviceProvider.GetRequiredService<SettingsService>().Settings)
            .AddSingleton<MsuPcmService>()
            .AddSingleton<AudioMetadataService>()
            .AddSingleton<ConverterService>()
            .AddSingleton<AudioAnalysisService>()
            .AddSingleton<MainWindow>()
            .AddSingleton<ProjectService>()
            .AddSingleton<PyMusicLooperService>()
            .AddSingleton<MainWindow>()
            .AddSingleton<TrackListService>()
            .AddTransient<NewProjectPanel>()
            .AddTransient<EditProjectPanel>()
            .AddTransient<PyMusicLooperPanel>()
            .AddTransient<MsuTrackInfoPanel>()
            .AddTransient<MsuPcmGenerationWindow>()
            .AddTransient<AudioControl>()
            .AddTransient<SettingsWindow>()
            .AddTransient<AudioAnalysisWindow>()
            .AddTransient<MusicLooperWindow>()
            .AddTransient<AddSongWindow>()
            .AddTransient<PythonCommandRunnerService>();

        if (OperatingSystem.IsWindows())
        {
            serviceCollection.AddSingleton<IAudioPlayerService, AudioPlayerServiceWindows>();
        }
        else
        {
            serviceCollection.AddSingleton<IAudioPlayerService, AudioPlayerServiceLinux>();    
        }
        
        _serviceProvider = serviceCollection.BuildServiceProvider();
        
        return _serviceProvider;
    }

#if DEBUG
    private static string LogPath => Path.Combine(Directories.LogFolder, "msu-scripter-debug_.log");
#else
    private static string LogPath => Path.Combine(Directories.LogFolder, "msu-scripter_.log");
#endif

}