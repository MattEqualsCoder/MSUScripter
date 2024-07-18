using System.Collections.Generic;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class VideoCreatorWindowViewModel : ViewModelBase
{
    [Reactive] public bool DisplayGitHubLink { get; set; }
    [Reactive] public string DisplayText { get; set; } = "Select video file to create";
    [Reactive] public bool CanRunVideoCreator { get; set; }
    [Reactive] public List<string?> PcmPaths { get; set; } = [];
    [Reactive] public string CloseButtonText { get; set; } = "Cancel";
    public string? PreviousPath { get; set; }
        
    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}