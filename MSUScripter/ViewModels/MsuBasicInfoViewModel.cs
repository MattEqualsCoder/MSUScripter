using System;
using AvaloniaControls.Models;
using Material.Icons;
using MSUScripter.Configs;
using MSUScripter.Models;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class MsuBasicInfoViewModel : ViewModelBase
{
    public MsuBasicInfoViewModel()
    {
        PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != nameof(LastModifiedDate) && args.PropertyName != nameof(HasBeenModified))
            {
                LastModifiedDate = DateTime.Now;
            }
        };
    }

    [SkipConvert] public MsuProjectViewModel Project { get; set; } = null!;
    [Reactive] public string MsuType { get; set; } = "";

    [Reactive] public string Game { get; set; } = "";
    [Reactive] public string PackName { get; set; } = "";
    [Reactive] public string PackCreator { get; set; } = "";
    [Reactive] public string PackVersion { get; set; } = "";
    [Reactive] public string Artist { get; set; } = "";
    [Reactive] public string Album { get; set; } = "";
    [Reactive] public string Url { get; set; } = ""; 
    [Reactive] public double? Normalization { get; set; } 
    [Reactive] public bool? Dither { get; set; } 
    [Reactive] public bool IsMsuPcmProject { get; set; }
    [Reactive] public bool CreateAltSwapper { get; set; }
    [Reactive] public bool CreateSplitSmz3Script { get; set; }
    [Reactive] public bool IsSmz3Project { get; set; }
    [Reactive] public string? ZeldaMsuPath { get; set; }
    [Reactive] public string? MetroidMsuPath { get; set; }

    [Reactive, ReactiveLinkedProperties(nameof(WriteTrackList))] public string TrackList { get; set; } = "";
    [Reactive] public bool WriteYamlFile { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public bool WriteTrackList => TrackList != TrackListType.Disabled;
    
    public bool HasChangesSince(DateTime time)
    {
        return LastModifiedDate > time;
    }

    public override ViewModelBase DesignerExample()
    {
        TrackList = TrackListType.List;
        IsMsuPcmProject = true;
        return this;
    }
}