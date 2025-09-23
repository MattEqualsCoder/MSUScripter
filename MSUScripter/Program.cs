using Avalonia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
// ReSharper disable once RedundantUsingDirective
using System.Linq;
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

        if (OperatingSystem.IsLinux())
        {
            _ = SetupLinuxDesktopFile();
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

    private static async Task SetupLinuxDesktopFile()
    {
        await Task.Run(() =>
        {
            var desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "applications");
            if (!Directory.Exists(desktopPath))
            {
                return;
            }

            var iconFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "icons");
            if (!Directory.Exists(iconFolderPath))
            {
                Directory.CreateDirectory(iconFolderPath);
            }

            var iconPath = Path.Combine(Path.Combine(iconFolderPath, "MSUScripter.svg"));

            var appImagePath = Directory.EnumerateFiles(Environment.CurrentDirectory, "MSUScripter*.AppImage")
                .FirstOrDefault();
            Log.Logger.Information("appImagePath: {Path}", appImagePath);
            if (string.IsNullOrEmpty(appImagePath) || !File.Exists(appImagePath))
            {
                return;
            }

            var desktopFilePath = Path.Combine(desktopPath, "MSUScripter.desktop");
            var uninstallFilePath = Path.Combine(Directories.BaseFolder, "uninstall.sh");

            var assembly = Assembly.GetExecutingAssembly();
            CopyIconFile(assembly, iconPath);
            CopyUninstallFile(assembly, appImagePath, iconPath, desktopFilePath, uninstallFilePath);
            CopyDesktopFile(assembly, appImagePath, iconPath, uninstallFilePath, desktopFilePath);
        });
    }

    private static void CopyDesktopFile(Assembly assembly, string appImagePath, string iconPath, string uninstallPath, string targetPath)
    {
        const string resourceName = "MSUScripter.Assets.org.mattequalscoder.msuscripter.desktop";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return;
        }
        using var reader = new StreamReader(stream);
        var desktopText = reader.ReadToEnd();

        var workingDirectory = Path.GetDirectoryName(appImagePath);
        var fileName = Path.GetFileName(appImagePath);
        desktopText = desktopText.Replace("%FolderPath%", workingDirectory);
        desktopText = desktopText.Replace("%FileName%", fileName);
        desktopText = desktopText.Replace("%IconPath%", iconPath);
        desktopText = desktopText.Replace("%DesktopFilePath%", targetPath);
        desktopText = desktopText.Replace("%UninstallPath%", uninstallPath);
        File.WriteAllText(targetPath, desktopText);
    }
    
    private static void CopyUninstallFile(Assembly assembly, string appImagePath, string iconPath, string desktopFilePath, string targetPath)
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }
        
        const string resourceName = "MSUScripter.Assets.uninstall.sh";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return;
        }
        using var reader = new StreamReader(stream);
        var fileText = reader.ReadToEnd();

        var workingDirectory = Path.GetDirectoryName(appImagePath);
        var fileName = Path.GetFileName(appImagePath);
        fileText = fileText.Replace("%FolderPath%", workingDirectory);
        fileText = fileText.Replace("%FileName%", fileName);
        fileText = fileText.Replace("%IconPath%", iconPath);
        fileText = fileText.Replace("%DesktopFilePath%", desktopFilePath);
        fileText = fileText.Replace("%LocalDataPath%", Directories.BaseFolder);
        File.WriteAllText(targetPath, fileText);
        File.SetUnixFileMode(targetPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupExecute);
    }

    private static void CopyIconFile(Assembly assembly, string targetPath)
    {
        const string resourceName = "MSUScripter.Assets.icon.svg";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return;
        }

        using var fileStream = new FileStream(Path.Combine(targetPath), FileMode.Create);
        for (var i = 0; i < stream.Length; i++)
        {
            fileStream.WriteByte((byte)stream.ReadByte());
        }
        fileStream.Close();
    }

#if DEBUG
    private static string LogPath => Path.Combine(Directories.LogFolder, "msu-scripter-debug_.log");
#else
    private static string LogPath => Path.Combine(Directories.LogFolder, "msu-scripter_.log");
#endif

}