using System;
using System.IO;
using System.Linq;
using AvaloniaControls.Controls;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.Controls;
using MSUScripter.Models;
using Settings = MSUScripter.Configs.Settings;

namespace MSUScripter.Services;

public class SettingsService
{
    private ILogger<SettingsService> _logger;
    private YamlService _yamlService;

    public static SettingsService Instance { get; private set; } = null!;

    public Settings Settings { get; set; } = null!;

    public SettingsService(ILogger<SettingsService> logger, YamlService yamlService)
    {
        _logger = logger;
        _yamlService = yamlService;
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
        
        if (!YamlService.Instance.FromYaml<Settings>(yaml, out var settingsObject, out _, false) ||
            settingsObject == null)
        {
            Settings = new Settings();
        }
        else
        {
            Settings = settingsObject;
        }

        ScalableWindow.GlobalScaleFactor = decimal.ToDouble(Settings.UiScaling);
    }

    public void SaveSettings()
    {
        var yaml = YamlService.Instance.ToYaml(Settings, false);
        var path = GetSettingsPath();
        var directory = new FileInfo(path).DirectoryName;
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(GetSettingsPath(), yaml);

        ScalableWindow.GlobalScaleFactor = decimal.ToDouble(Settings.UiScaling);
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