using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.Controls;
using MSUScripter.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Settings = MSUScripter.Configs.Settings;

namespace MSUScripter.Services;

public class SettingsService
{
    private ILogger<SettingsService> _logger;

    public static SettingsService Instance { get; private set; } = null!;

    public Settings Settings { get; set; } = null!;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        LoadSettings();
        Instance = this;
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

        ScalableWindow.GlobalScaleFactor = Settings.UiScaling;
    }

    public void SaveSettings()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        var yaml = serializer.Serialize(Settings);
        var path = GetSettingsPath();
        var directory = new FileInfo(path).DirectoryName;
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(GetSettingsPath(), yaml);

        ScalableWindow.GlobalScaleFactor = Settings.UiScaling;
    }

    public void AddRecentProject(MsuProject project)
    {
        var projectFile = new FileInfo(project.ProjectFilePath);
        var folder = projectFile.Directory?.Name ?? "";
        var baseName = projectFile.Name.Replace(projectFile.Extension, "");
        
        var projects = Settings.RecentProjects.Where(x => x.ProjectPath != project.ProjectFilePath).ToList();
        projects.Add(new RecentProject()
        {
            ProjectPath = project.ProjectFilePath,
            ProjectName = !string.IsNullOrEmpty(project.BasicInfo.PackName)
                ? project.BasicInfo.PackName
                : $"{folder}/{baseName}",
            Time = DateTime.Now
        });
        Settings.RecentProjects = projects.OrderByDescending(x => x.Time).Take(5).ToList();
        SaveSettings();
    }

    private string GetSettingsPath()
    {
#if DEBUG
        return Path.Combine(Directories.BaseFolder, "settings-debug.yml");
#else
        return Path.Combine(Directories.BaseFolder, "settings.yml");
#endif
    }
}