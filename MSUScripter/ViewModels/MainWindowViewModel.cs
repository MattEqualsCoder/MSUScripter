using System;
using System.Collections.Generic;
using Avalonia.Media;
using AvaloniaControls.Models;
using MSURandomizerLibrary.Configs;
using MSUScripter.Configs;
using ReactiveUI.SourceGenerators;

#pragma warning disable CS0067 // Event is never used

namespace MSUScripter.ViewModels;

public class MsuTypeDropdownOption
{
    public required string DisplayName { get; init; }
    public required MsuType MsuType { get; init; }
}

public partial class MainWindowViewModel : TranslatedViewModelBase
{
    [Reactive] public partial string Title { get; set; }
    
    [Reactive, ReactiveLinkedEvent(nameof(CurrentMsuProjectChanged))]
    public partial MsuProject? CurrentMsuProject { get; set; }
    
    public string? InitProject { get; set; }
    public bool InitProjectError { get; set; }
    public string AppVersion { get; set; } = "";
    public event EventHandler? CurrentMsuProjectChanged;

    [Reactive, ReactiveLinkedProperties(nameof(CanCreateProject))]
    public partial string MsuProjectName { get; set; }
    [Reactive, ReactiveLinkedProperties(nameof(CanCreateProject))]
    public partial string MsuCreatorName { get; set; }
    [Reactive, ReactiveLinkedProperties(nameof(CanCreateProject))]
    public partial string MsuPath { get; set; }
    [Reactive, ReactiveLinkedProperties(nameof(CanCreateProject))]
    public partial string MsuProjectPath { get; set; }
    [Reactive, ReactiveLinkedProperties(nameof(CanSetMsuPcmWorkingPath))]
    public partial string MsuPcmJsonPath { get; set; }
    [Reactive] public partial string MsuPcmWorkingPath { get; set; }
    public bool CanSetMsuPcmWorkingPath => !string.IsNullOrEmpty(MsuPcmJsonPath);
    public bool CanCreateProject => !string.IsNullOrEmpty(MsuProjectName) && !string.IsNullOrEmpty(MsuCreatorName) && !string.IsNullOrEmpty(MsuPath) && !string.IsNullOrEmpty(MsuProjectPath) && SelectedMsuType != null;
    [Reactive] public partial List<MsuType> MsuTypes { get; set; }
    [Reactive, ReactiveLinkedProperties(nameof(CanCreateProject))]
    public partial MsuType? SelectedMsuType { get; set; } 
    [Reactive] public partial bool DisplayNewProjectPage { get; set; }
    [Reactive] public partial bool DisplayOpenProjectPage { get; set; }
    [Reactive] public partial bool DisplaySettingsPage { get; set; }
    [Reactive] public partial bool DisplayAboutPage { get; set; }
    [Reactive] public partial bool ValidatedDependencies { get; set; }
    [Reactive] public partial List<RecentProject> RecentProjects { get; set; }
    [Reactive] public partial RecentProject? SelectedRecentProject { get; set; }
    [Reactive] public partial IBrush NewProjectBackground { get; set; }
    [Reactive] public partial IBrush OpenProjectBackground { get; set; }
    [Reactive] public partial IBrush SettingsBackground { get; set; }
    [Reactive] public partial IBrush AboutBackground { get; set; }
    public SettingsPanelViewModel Settings { get; set; } = new();
    public IBrush ActiveTabBackground = Brushes.Transparent;

    public MainWindowViewModel()
    {
        Title = "MSU Scripter";
        MsuProjectName = string.Empty;
        MsuCreatorName = string.Empty;
        MsuPath = string.Empty;
        MsuProjectPath = string.Empty;
        MsuPcmJsonPath = string.Empty;
        MsuPcmWorkingPath = string.Empty;
        MsuTypes = [];
        RecentProjects = [];
        DisplayOpenProjectPage = true;
        NewProjectBackground = Brushes.Transparent;
        OpenProjectBackground = Brushes.Transparent;
        SettingsBackground = Brushes.Transparent;
        AboutBackground = Brushes.Transparent;
    }
    
    public override ViewModelBase DesignerExample()
    {
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
