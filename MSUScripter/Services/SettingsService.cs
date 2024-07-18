using System;
using System.IO;
using System.Linq;
using AvaloniaControls.Controls;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.Models;
using Settings = MSUScripter.Configs.Settings;

namespace MSUScripter.Services;

public class SettingsService
{
    private readonly YamlService _yamlService;

    public Settings Settings { get; set; } = null!;

    public SettingsService(YamlService yamlService)
    {
        _yamlService = yamlService;
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
        
        if (!_yamlService.FromYaml<Settings>(yaml, out var settingsObject, out _, false) ||
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
        var yaml = _yamlService.ToYaml(Settings, false, false);
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