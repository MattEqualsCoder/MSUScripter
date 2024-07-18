using System.Collections.Generic;
using AvaloniaControls.Models;
using MSURandomizerLibrary.Configs;
using MSUScripter.Configs;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class NewProjectPanelViewModel : ViewModelBase
{
    public List<MsuType> MsuTypes { get; set; } = [];
    
    [Reactive, ReactiveLinkedProperties(nameof(CanCreateNewProject))] public MsuType? SelectedMsuType { get; set; }  
    [Reactive, ReactiveLinkedProperties(nameof(CanCreateNewProject))] public string? MsuPath { get; set; }
    [Reactive] public string? MsuPcmTracksJsonPath { get; set; }
    [Reactive] public string? MsuPcmWorkingDirectoryPath { get; set; }
    [Reactive, ReactiveLinkedProperties(nameof(AnyRecentProjects))] public List<RecentProject> RecentProjects { get; set; } = [];
    public bool CanCreateNewProject => SelectedMsuType != null && !string.IsNullOrEmpty(MsuPath);
    public bool AnyRecentProjects => RecentProjects.Count > 0;
    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}