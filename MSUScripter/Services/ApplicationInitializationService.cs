using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MSURandomizerLibrary.Models;
using MSURandomizerLibrary.Services;
using MSUScripter.Models;

namespace MSUScripter.Services;

public class ApplicationInitializationService(ILogger<ApplicationInitializationService> logger)
{
    public void Initialize(string[] args)
    {
        logger.LogInformation("Assembly Location: {Location}", Assembly.GetExecutingAssembly().Location);
        logger.LogInformation("Starting MSU Scripter {Version}", GetAppVersion());
        
        var msuInitializationRequest = new MsuRandomizerInitializationRequest
        {
            MsuAppSettingsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MSUScripter.msu-randomizer-settings.yaml"),
            UserOptionsPath = Path.Combine(Directories.BaseFolder, "msu-user-settings.yml")
        };

#if DEBUG
        msuInitializationRequest.UserOptionsPath = Path.Combine(Directories.BaseFolder, "msu-user-settings-debug.yml");
#endif
        
        Program.MainHost.Services.GetRequiredService<IMsuRandomizerInitializationService>().Initialize(msuInitializationRequest);

    }
    
    private static string GetAppVersion()
    {
        var version = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location); 
        return (version.ProductVersion ?? "").Split("+")[0];
    }
}