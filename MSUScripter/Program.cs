using Avalonia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
// ReSharper disable once RedundantUsingDirective
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppImageDesktopFileCreator;
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

        InitializeServices();

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
            ShowExceptionPopup(e).ContinueWith(_ => source.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.UIThread.MainLoop(source.Token);
        }
        
    }
    
    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
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
            .AddSingleton<ProjectService>()
            .AddSingleton<TrackListService>()
            .AddSingleton<StatusBarService>()
            .AddSingleton<PythonCompanionService>()
            .AddSingleton<DependencyInstallerService>()
            .AddAvaloniaControlServices<Program>()
            .AddTransient<ApplicationInitializationService>();

        if (OperatingSystem.IsWindows())
        {
            collection.AddSingleton<IAudioPlayerService, AudioPlayerServiceNAudio>();
        }
        else
        {
            collection.AddSingleton<IAudioPlayerService, AudioPlayerServiceSoundFlow>();
        }

        return collection;
    }

    private static void InitializeServices()
    {
        var services = MainHost.Services;
        services.GetRequiredService<SettingsService>();
        services.GetRequiredService<ITaskService>();
        services.GetRequiredService<IControlServiceFactory>();
        services.GetRequiredService<ConverterService>();
        services.GetRequiredService<YamlService>();
        services.GetRequiredService<ApplicationInitializationService>().Initialize();
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

    [SupportedOSPlatform("linux")]
    internal static CreateDesktopFileResponse BuildLinuxDesktopFile()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return new DesktopFileBuilder("org.mattequalscoder.msuscripter", "MSU Scripter")
            .AddDescription("UI application for creating MSUs and PCM files")
            .AddCategory(DesktopFileCategories.Development)
            .AddWindowClass("MSUScripter")
            .AddIcon(assembly, "MSUScripter.Assets.icon.16.png", 16)
            .AddIcon(assembly, "MSUScripter.Assets.icon.32.png", 32)
            .AddIcon(assembly, "MSUScripter.Assets.icon.48.png", 48)
            .AddIcon(assembly, "MSUScripter.Assets.icon.256.png", 256)
            .AddIcon(assembly, "MSUScripter.Assets.icon.svg")
            .AddUninstallAction(Directories.BaseFolder)
            .WithMimeType("application/x-msu-scripter-project", "MSU Scripter Project", "*.msup", true)
            .Build();
    }

#if DEBUG
    private static string LogPath => Path.Combine(Directories.LogFolder, "msu-scripter-debug_.log");
#else
    private static string LogPath => Path.Combine(Directories.LogFolder, "msu-scripter_.log");
#endif

}