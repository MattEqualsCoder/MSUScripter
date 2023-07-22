using System;
using System.IO;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Settings = MSUScripter.Configs.Settings;

namespace MSUScripter.Services;

public class SettingsService
{
    private ILogger<SettingsService> _logger;

    public static Settings Settings { get; private set; } = new();
    public static bool SettingsLoaded { get; private set; }

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        LoadSettings();
        SettingsLoaded = true;
    }

    public void LoadSettings()
    {
        var settingsPath = GetSettingsPath();
        if (!File.Exists(settingsPath))
        {
            Settings = new();
            SaveSettings();
            return;
        }

        var yaml = File.ReadAllText(settingsPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        Settings = deserializer.Deserialize<Settings>(yaml);
    }

    public void SaveSettings()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        var yaml = serializer.Serialize(Settings);
        File.WriteAllText(GetSettingsPath(), yaml);
    }

    private string GetSettingsPath()
    {
        var settingsDirectory = Environment.ExpandEnvironmentVariables("%LocalAppData%\\MSUScripter");
#if DEBUG
        return Path.Combine(settingsDirectory, "settings-debug.yml");
#else
        return Path.Combine(settingsDirectory, "settings.yml");
#endif
    }
}