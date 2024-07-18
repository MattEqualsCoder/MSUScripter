using Avalonia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using AvaloniaControls.Services;
using GitHubReleaseChecker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MSURandomizerLibrary;
using MSUScripter.Models;
using MSUScripter.Services;
using MSUScripter.Services.ControlServices;
using MSUScripter.Views;
using Serilog;
using Win32RenderingMode = Avalonia.Win32RenderingMode;

namespace MSUScripter;

class Program
{
    internal static IHost MainHost { get; private set; } = null!;
    internal static string? StartingProject { get; private set; }
        
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
            
        var loggerConfiguration = new LoggerConfiguration();
        
#if DEBUG
        loggerConfiguration = loggerConfiguration.MinimumLevel.Debug();
#else
        if (args.Contains("-d"))
        {
            loggerConfiguration = loggerConfiguration.MinimumLevel.Debug();
        }
        else
        {
            loggerConfiguration = loggerConfiguration.MinimumLevel.Information();
        }
#endif
        
        Log.Logger = loggerConfiguration
            .Enrich.FromLogContext()
            .WriteTo.File(LogPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
#if DEBUG
            .WriteTo.Debug()
            .WriteTo.Console()
#endif
            .CreateLogger();

        if (args.Length == 1 && args[0].EndsWith(".msup", StringComparison.OrdinalIgnoreCase) && File.Exists(args[0]))
        {
            StartingProject = args[0];
        }
        
        MainHost = Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureLogging(logging =>
            {
                logging.AddSerilog(dispose: true);
            })
            .ConfigureServices(services =>
            {
                ConfigureServices(services);
            })
            .Build();

        InitializeServices(args);

        ExceptionWindow.GitHubUrl = "https://github.com/MattEqualsCoder/MSUScripter/issues";
        ExceptionWindow.LogPath = Directories.LogFolder;
        
        using var source = new CancellationTokenSource();
        
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            ShowExceptionPopup(e).ContinueWith(t => source.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.UIThread.MainLoop(source.Token);
        }
        
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

    private static IServiceCollection ConfigureServices(IServiceCollection collection)
    {
        collection.AddMsuRandomizerServices()
            .AddGitHubReleaseCheckerServices()
            .AddSingleton<SettingsService>()
            .AddSingleton<YamlService>()
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
            .AddSingleton<StatusBarService>()
            .AddTransient<NewProjectPanel>()
            .AddTransient<PyMusicLooperPanel>()
            .AddTransient<MsuTrackInfoPanel>()
            .AddTransient<MsuPcmGenerationWindow>()
            .AddTransient<AudioControl>()
            .AddTransient<SettingsWindow>()
            .AddTransient<AudioAnalysisWindow>()
            .AddTransient<PyMusicLooperWindow>()
            .AddTransient<AddSongWindow>()
            .AddTransient<PythonCommandRunnerService>()
            .AddTransient<VideoCreatorWindowService>()
            .AddTransient<VideoCreatorWindow>()
            .AddTransient<SharedPcmService>()
            .AddAvaloniaControlServices<Program>()
            .AddTransient<ApplicationInitializationService>();

        if (OperatingSystem.IsWindows())
        {
            collection.AddSingleton<IAudioPlayerService, AudioPlayerServiceWindows>();
        }
        else
        {
            collection.AddSingleton<IAudioPlayerService, AudioPlayerServiceLinux>();    
        }

        return collection;
    }

    private static void InitializeServices(string[] args)
    {
        var services = MainHost.Services;
        services.GetRequiredService<SettingsService>();
        services.GetRequiredService<ITaskService>();
        services.GetRequiredService<IControlServiceFactory>();
        services.GetRequiredService<ConverterService>();
        services.GetRequiredService<YamlService>();
        services.GetRequiredService<ApplicationInitializationService>().Initialize(args);
    }
    
    private static async Task ShowExceptionPopup(Exception e)
    {
        Log.Error(e, "[CRASH] Uncaught {Name}: ", e.GetType().Name);
        var window = new ExceptionWindow();
        window.Show();
        await Dispatcher.UIThread.Invoke(async () =>
        {
            while (window.IsVisible)
            {
                await Task.Delay(500);
            }
        });
    }

#if DEBUG
    private static string LogPath => Path.Combine(Directories.LogFolder, "msu-scripter-debug_.log");
#else
    private static string LogPath => Path.Combine(Directories.LogFolder, "msu-scripter_.log");
#endif

}