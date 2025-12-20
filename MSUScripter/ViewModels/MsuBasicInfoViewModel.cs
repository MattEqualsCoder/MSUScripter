using System;
using MSUScripter.Configs;
using ReactiveUI.SourceGenerators;

namespace MSUScripter.ViewModels;

public partial class MsuBasicInfoViewModel : SavableViewModelBase
{
    public MsuProject? Project { get; set; }
    [Reactive] public partial string MsuType { get; set; }
    [Reactive] public partial string PackName { get; set; }
    [Reactive] public partial string PackCreator { get; set; }
    [Reactive] public partial string PackVersion { get; set; }
    [Reactive] public partial string Artist { get; set; }
    [Reactive] public partial string Album { get; set; }
    [Reactive] public partial string Url { get; set; }
    
    [Reactive] public partial bool IsMsuPcmProject { get; set; }
    [Reactive] public partial double? Normalization { get; set; }
    [Reactive] public partial DitherType DitherType { get; set; }
    public bool HasSeenDitherWarning { get; set; }
    [Reactive] public partial bool IncludeJson { get; set; }
    
    [Reactive] public partial TrackList TrackList { get; set; }
    [Reactive] public partial bool WriteYamlFile { get; set; }
    [Reactive] public partial bool CreateAltSwapperScript { get; set; }
    
    [Reactive] public partial bool IsSmz3Project { get; set; }
    [Reactive] public partial bool CreateSplitSmz3Script { get; set; }
    [Reactive] public partial string? ZeldaMsuPath { get; set; }
    [Reactive] public partial string? MetroidMsuPath { get; set; }
    [Reactive] public partial bool IsVisible { get; set; }

    public MsuBasicInfoViewModel()
    {
        MsuType = string.Empty;
        PackName = string.Empty;
        PackCreator = string.Empty;
        PackVersion = string.Empty;
        Artist = string.Empty;
        Album = string.Empty;
        Url = string.Empty;
    }
    
    public void UpdateModel(MsuProject project)
    {
        Project = project;
        MsuType = project.MsuTypeName;
        
        PackName = project.BasicInfo.PackName ?? "";
        PackCreator = project.BasicInfo.PackCreator ?? "";
        PackVersion = project.BasicInfo.PackVersion ?? "";
        Artist = project.BasicInfo.Artist ?? "";
        Album = project.BasicInfo.Album ?? "";
        Url = project.BasicInfo.Url ?? "";
        
        CreateAltSwapperScript = project.BasicInfo.CreateAltSwapperScript;
        CreateSplitSmz3Script = project.BasicInfo.CreateSplitSmz3Script;
        TrackList = project.BasicInfo.TrackListType;
        IsSmz3Project = project.BasicInfo.IsSmz3Project;
        ZeldaMsuPath = project.BasicInfo.ZeldaMsuPath;
        MetroidMsuPath = project.BasicInfo.MetroidMsuPath;
        WriteYamlFile = project.BasicInfo.WriteYamlFile;
        
        IsMsuPcmProject = project.BasicInfo.IsMsuPcmProject;
        Normalization = project.BasicInfo.Normalization;
        DitherType = project.BasicInfo.DitherType;
        HasSeenDitherWarning = project.BasicInfo.HasSeenDitherWarning;
        IncludeJson = project.BasicInfo.IncludeJson ?? false;

        LastModifiedDate = project.BasicInfo.LastModifiedDate;
    }
    
    public bool HasChangesSince(DateTime time)
    {
        return LastModifiedDate > time;
    }

    public override ViewModelBase DesignerExample()
    {
        TrackList = TrackList.ListAlbumFirst;
        IsMsuPcmProject = true;
        IsSmz3Project = true;
        return this;
    }

    public override void SaveChanges()
    {
        if (Project == null) return;
        
        Project.BasicInfo.PackName = PackName;
        Project.BasicInfo.PackCreator = PackCreator;
        Project.BasicInfo.PackVersion = PackVersion;
        
        Project.BasicInfo.Artist = Artist;
        Project.BasicInfo.Album = Album;
        Project.BasicInfo.Url = Url;
        
        Project.BasicInfo.CreateAltSwapperScript = CreateAltSwapperScript;
        Project.BasicInfo.CreateSplitSmz3Script = CreateSplitSmz3Script;
        Project.BasicInfo.TrackListType = TrackList;
        Project.BasicInfo.IsSmz3Project = IsSmz3Project;
        Project.BasicInfo.ZeldaMsuPath = ZeldaMsuPath;
        Project.BasicInfo.MetroidMsuPath = MetroidMsuPath;
        Project.BasicInfo.WriteYamlFile = WriteYamlFile;

        Project.BasicInfo.IsMsuPcmProject = IsMsuPcmProject;
        Project.BasicInfo.Normalization = Normalization;
        Project.BasicInfo.DitherType = DitherType;
        Project.BasicInfo.HasSeenDitherWarning = HasSeenDitherWarning;
        Project.BasicInfo.IncludeJson = IncludeJson;
    }
}