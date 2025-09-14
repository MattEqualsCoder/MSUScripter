using System;
using System.Collections.Generic;
using Avalonia.Media;
using AvaloniaControls.Models;
using MSURandomizerLibrary.Configs;
using MSUScripter.Configs;
using ReactiveUI.Fody.Helpers;
#pragma warning disable CS0067 // Event is never used

namespace MSUScripter.ViewModels;

public class MsuTypeDropdownOption
{
    public required string DisplayName { get; init; }
    public required MsuType MsuType { get; init; }
}

public class MainWindowViewModel : TranslatedViewModelBase
{
    [Reactive] public string Title { get; set; } = "MSU Scripter";
    
    [Reactive, ReactiveLinkedEvent(nameof(CurrentMsuProjectChanged))]
    public MsuProject? CurrentMsuProject { get; set; }
    
    public string? InitProject { get; set; }
    public bool InitProjectError { get; set; }

    [Reactive, ReactiveLinkedProperties(nameof(DisplayNewVersionBanner))]
    public string? GitHubReleaseUrl { get; set; }
    public string AppVersion { get; set; } = "";
    public bool DisplayNewVersionBanner => !string.IsNullOrEmpty(GitHubReleaseUrl);
    public event EventHandler? CurrentMsuProjectChanged;

    [Reactive, ReactiveLinkedProperties(nameof(CanCreateProject))]
    public string MsuProjectName { get; set; } = string.Empty;
    [Reactive, ReactiveLinkedProperties(nameof(CanCreateProject))]
    public string MsuCreatorName { get; set; } = string.Empty;
    [Reactive, ReactiveLinkedProperties(nameof(CanCreateProject))]
    public string MsuPath { get; set; } = string.Empty;
    [Reactive, ReactiveLinkedProperties(nameof(CanCreateProject))]
    public string MsuProjectPath { get; set; } = string.Empty;
    [Reactive, ReactiveLinkedProperties(nameof(CanSetMsuPcmWorkingPath))]
    public string MsuPcmJsonPath { get; set; } = string.Empty;
    [Reactive] public string MsuPcmWorkingPath { get; set; } = string.Empty;
    public bool CanSetMsuPcmWorkingPath => !string.IsNullOrEmpty(MsuPcmJsonPath);
    public bool CanCreateProject => !string.IsNullOrEmpty(MsuProjectName) && !string.IsNullOrEmpty(MsuCreatorName) && !string.IsNullOrEmpty(MsuPath) && !string.IsNullOrEmpty(MsuProjectPath) && SelectedMsuType != null;
    [Reactive] public List<MsuType> MsuTypes { get; set; } = [];
    [Reactive, ReactiveLinkedProperties(nameof(CanCreateProject))]
    public MsuType? SelectedMsuType { get; set; } 
    [Reactive] public bool DisplayNewProjectPage { get; set; } = false;
    [Reactive] public bool DisplayOpenProjectPage { get; set; } = true;
    [Reactive] public bool DisplaySettingsPage { get; set; }
    [Reactive] public bool DisplayAboutPage { get; set; }
    [Reactive] public List<RecentProject> RecentProjects { get; set; } = [];
    [Reactive] public RecentProject? SelectedRecentProject { get; set; }
    [Reactive] public IBrush NewProjectBackground { get; set; } = Brushes.Transparent;
    [Reactive] public IBrush OpenProjectBackground { get; set; } = Brushes.Transparent;
    [Reactive] public IBrush SettingsBackground { get; set; } = Brushes.Transparent;
    [Reactive] public IBrush AboutBackground { get; set; } = Brushes.Transparent;
    public SettingsPanelViewModel Settings { get; set; } = new();

    public IBrush ActiveTabBackground = Brushes.Transparent;
    
    public override ViewModelBase DesignerExample()
    {
        GitHubReleaseUrl = "a";
        AppVersion = "v4.0.3";
        MsuTypes =
        [
            new MsuType()
            {
                Name = "SMZ3",
                DisplayName = "SMZ3",
                RequiredTrackNumbers = [],
                ValidTrackNumbers = [],
                Tracks = []
            }
        ];
        RecentProjects =
        [
            new RecentProject()
            {
                ProjectName = "Project 1",
                ProjectPath = "/home/matt/Documents/MSUProjects/When The Fates Cry/WhenTheFatesCry.msup"
            },
            new RecentProject()
            {
                ProjectName = "Project 2",
                ProjectPath = "C:\\User\\Test\\Documents\\test2.msup"
            },
        ];
        return this;
    }
}
