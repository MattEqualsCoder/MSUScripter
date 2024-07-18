using System;
using AvaloniaControls.Models;
using MSUScripter.Configs;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    [Reactive] public string Title { get; set; } = "MSU Scripter";
    
    [Reactive, ReactiveLinkedProperties(nameof(DisplayNewPage), nameof(DisplayEditPage)), ReactiveLinkedEvent(nameof(CurrentMsuProjectChanged))]
    public MsuProject? CurrentMsuProject { get; set; }
    
    public MsuProject? InitProject { get; set; }
    public MsuProject? InitBackupProject { get; set; }
    public bool InitProjectError { get; set; }

    [Reactive, ReactiveLinkedProperties(nameof(DisplayNewVersionBanner))]
    public string? GitHubReleaseUrl { get; set; }
    
    public bool DisplayNewPage => CurrentMsuProject == null;
    public bool DisplayEditPage => CurrentMsuProject != null;
    public string AppVersion { get; set; } = "";
    public bool DisplayNewVersionBanner => !string.IsNullOrEmpty(GitHubReleaseUrl);
    public object? NullValue => null;
    public event EventHandler? CurrentMsuProjectChanged;
    public override ViewModelBase DesignerExample()
    {
        GitHubReleaseUrl = "a";
        return this;
    }
}