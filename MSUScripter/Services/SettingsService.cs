using System;
using System.IO;
using System.Linq;
using AvaloniaControls.Controls;
using AvaloniaControls.Services;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.Models;
using Settings = MSUScripter.Configs.Settings;

namespace MSUScripter.Services;

public class SettingsService
{
    private readonly YamlService _yamlService;
    private readonly ILogger<SettingsService> _logger;

    public Settings Settings { get; set; } = null!;

    public SettingsService(YamlService yamlService, ILogger<SettingsService> logger)
    {
        _yamlService = yamlService;
        _logger = logger;
        LoadSettings();
    }

    public void LoadSettings()
    {
        var settingsPath = GetSettingsPath();
        if (!File.Exists(settingsPath))
        {
            Settings = new Settings();
            SaveSettings();
            return;
        }

        var yaml = File.ReadAllText(settingsPath);
        
        if (!_yamlService.FromYaml<Settings>(yaml, YamlType.Pascal, out var settingsObject, out _) ||
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
        var yaml = _yamlService.ToYaml(Settings, YamlType.Pascal);
        var path = GetSettingsPath();
        var directory = new FileInfo(path).DirectoryName;
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(GetSettingsPath(), yaml);

        ScalableWindow.GlobalScaleFactor = decimal.ToDouble(Settings.UiScaling);
    }

    public void TrySaveSettings()
    {
        _ = ITaskService.Run(() =>
        {
            try
            {
                SaveSettings();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error saving settings");
            }
        });
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