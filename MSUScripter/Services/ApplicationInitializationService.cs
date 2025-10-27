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
    public void Initialize()
    {
        logger.LogInformation("Assembly Location: {Location}", Assembly.GetExecutingAssembly().Location);
        logger.LogInformation("Starting MSU Scripter {Version}", App.Version);
        
        var msuInitializationRequest = new MsuRandomizerInitializationRequest
        {
            MsuAppSettingsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MSUScripter.Assets.msu-randomizer-settings.yaml"),
            UserOptionsPath = Path.Combine(Directories.BaseFolder, "msu-user-settings.yml")
        };

#if DEBUG
        msuInitializationRequest.UserOptionsPath = Path.Combine(Directories.BaseFolder, "msu-user-settings-debug.yml");
#endif
        
        Program.MainHost.Services.GetRequiredService<IMsuRandomizerInitializationService>().Initialize(msuInitializationRequest);

    }
}