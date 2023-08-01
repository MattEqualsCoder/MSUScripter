using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using GitHubReleaseChecker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MSURandomizerLibrary;
using MSURandomizerLibrary.Models;
using MSURandomizerLibrary.Services;
using MSUScripter.Services;
using MSUScripter.UI;

namespace MSUScripter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        private ILogger<App>? _logger;
        
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(@"\UI\Themes\DarkTheme.xaml", UriKind.Relative)});
            
            _host = Host.CreateDefaultBuilder(e.Args)
                .ConfigureLogging(logging =>
                {
                    logging.AddFile($"%LocalAppData%\\MSUScripter\\msu-scripter-{DateTime.UtcNow:yyyyMMdd}.log", options =>
                    {
                        options.Append = true;
                        options.FileSizeLimitBytes = 52428800;
                        options.MaxRollingFiles = 5;
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddMsuRandomizerServices();
                    services.AddSingleton<SettingsService>();
                    services.AddSingleton<MsuPcmService>();
                    services.AddSingleton<AudioService>();
                    services.AddSingleton<AudioMetadataService>();
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<ProjectService>();
                    services.AddTransient<NewPanel>();
                    services.AddTransient<EditPanel>();
                    services.AddTransient<MsuTrackInfoPanel>();
                    services.AddTransient<AudioControl>();
                    services.AddGitHubReleaseCheckerServices();
                })
                .Start();

            _host.Services.GetRequiredService<SettingsService>();
            _logger = _host.Services.GetRequiredService<ILogger<App>>();
            var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            _logger.LogInformation("Starting MSU Scripter {Version}", version.ProductVersion ?? "");

            if (SettingsService.Settings.PromptOnUpdate)
            {
                var newerHubRelease = _host.Services.GetRequiredService<IGitHubReleaseCheckerService>()
                    .GetGitHubReleaseToUpdateTo("MattEqualsCoder", "MSUScripter", version.ProductVersion ?? "", SettingsService.Settings.PromptOnPreRelease);

                if (newerHubRelease != null)
                {
                    var response = MessageBox.Show("A new version of the MSU Scripter is now available!\n" +
                                                   "Do you want to open up the GitHub release page for the update?\n" +
                                                   "\n" +
                                                   "You can disable this check in the settings window.", "MSU Scripter Update",
                        MessageBoxButton.YesNo);

                    if (response == MessageBoxResult.Yes)
                    {
                        var url = newerHubRelease.Url.Replace("&", "^&");
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                }
            }
            
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
   
            var msuInitializationRequest = new MsuRandomizerInitializationRequest
            {
                MsuAppSettingsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MSUScripter.msu-randomizer-settings.yaml")
            };

#if DEBUG
            msuInitializationRequest.MsuTypeConfigPath = GetConfigDirectory();
            msuInitializationRequest.UserOptionsPath = "%LocalAppData%\\MSUScripter\\msu-user-settings-debug.yml";
#endif
            
            _host.Services.GetRequiredService<IMsuRandomizerInitializationService>().Initialize(msuInitializationRequest);
            _host.Services.GetRequiredService<MainWindow>().Show();
        }
        
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                _logger?.LogCritical(ex, "[CRASH] Uncaught {ExceptionType}: ", ex.GetType().Name);
            else
                _logger?.LogCritical("Unhandled exception in current domain but exception object is not an exception ({Obj})", e.ExceptionObject);
            
            var response = MessageBox.Show("A critical error has occurred. Please open an issue at\n" + 
                                           "https://github.com/MattEqualsCoder/MSUScripter/issues.\n" +
                                           "Press Yes to open the log directory.",
                "MSU Randomizer",
                MessageBoxButton.YesNo);
            
            if (response != MessageBoxResult.Yes) return;
            var logFileLocation = Environment.ExpandEnvironmentVariables("%LocalAppData%\\MSUScripter");
            var startInfo = new ProcessStartInfo
            {
                Arguments = logFileLocation, 
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
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
}