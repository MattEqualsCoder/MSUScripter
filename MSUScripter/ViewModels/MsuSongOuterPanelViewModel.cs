using System.ComponentModel;
using System.Linq;
using MSUScripter.Configs;
using MSUScripter.Models;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class MsuSongOuterPanelViewModel : SavableViewModelBase
{
    [Reactive, SkipLastModified] public string TrackTitleText { get; set; } = "";
    
    [Reactive, SkipLastModified] public string? TrackDescriptionText { get; set; }
    
    [Reactive, SkipLastModified] public bool IsScratchPad { get; set; }
    
    [Reactive, SkipLastModified] public bool DisplayAddSong { get; set; }
    
    public MsuProject? Project { get; set; }
    public MsuTrackInfo? TrackInfo { get; set; }
    public MsuSongInfo? SongInfo { get; set; }
    
    [Reactive, SkipLastModified] public bool HasTrackDescription { get; set; }
    
    [Reactive, SkipLastModified] public bool IsEnabled { get; set; }
    
    [Reactive] public bool IsComplete { get; set; }
    [Reactive, SkipLastModified] public bool DisplayCompleteCheckbox { get; set; }
    [Reactive, SkipLastModified] public string AverageAudioLevel { get; set; } = "";
    [Reactive, SkipLastModified] public string PeakAudioLevel { get; set; } = "";
    [Reactive, SkipLastModified] public bool DisplaySecondAudioLine { get; set; }
    [Reactive, SkipLastModified] public bool CanGeneratePcmFiles { get; set; } = true;

    public MsuSongBasicPanelViewModel BasicPanelViewModel { get; set; } = new();
    
    public MsuSongAdvancedPanelViewModel AdvancedPanelViewModel { get; set; } = new();

    public MsuProjectWindowViewModelTreeData? TreeData { get; set; }
    
    public MsuSongOuterPanelViewModel()
    {
        PropertyChanged += OnPropertyChanged;
        return;

        void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsComplete) && SongInfo != null)
            {
                SongInfo.IsComplete = IsComplete;
                TreeData?.UpdateCompletedFlag();
                TreeData?.ParentTreeData?.UpdateCompletedFlag();
            }
        }
    }
    
    public void UpdateViewModel(MsuProject project, MsuTrackInfo trackInfo, MsuSongInfo? songInfo, MsuProjectWindowViewModelTreeData treeData)
    {
        var altText = trackInfo.Songs.Count <= 1 || songInfo == null
            ? ""
            : songInfo.IsAlt
                ? " - Alt Song #" + trackInfo.Songs.IndexOf(songInfo)
                : " - Primary Song";
        var trackInfoNumber = trackInfo.TrackNumber != 9999 ? $"#{trackInfo.TrackNumber} " : string.Empty;
        Project = project;
        TrackInfo = trackInfo;
        SongInfo = songInfo;
        TrackTitleText = $"{trackInfoNumber}{trackInfo.TrackName}{altText}";
        TrackDescriptionText = trackInfo.TrackNumber != 9999
            ? project.MsuType.Tracks.FirstOrDefault(x => x.Number == trackInfo.TrackNumber)?.Description
            : "A temporary location for creating songs before moving them to a specific track.";
        IsScratchPad = trackInfo.IsScratchPad;
        HasTrackDescription = !string.IsNullOrEmpty(TrackDescriptionText);
        AverageAudioLevel = "";
        PeakAudioLevel = "";
        CanGeneratePcmFiles = project.BasicInfo.IsMsuPcmProject;

        if (songInfo != null)
        {
            IsComplete = songInfo.IsComplete;

            if (songInfo.DisplayAdvancedMode == true && project.BasicInfo.IsMsuPcmProject)
            {
                AdvancedPanelViewModel.UpdateViewModel(project, trackInfo, songInfo, treeData);
                BasicPanelViewModel.IsEnabled = false;
                AdvancedPanelViewModel.IsEnabled = true;
            }
            else
            {
                songInfo.DisplayAdvancedMode = false;
                BasicPanelViewModel.UpdateViewModel(project, trackInfo, songInfo, treeData);
                BasicPanelViewModel.IsEnabled = true;
                AdvancedPanelViewModel.IsEnabled = false;
            }
            
            DisplayCompleteCheckbox = true;
            DisplayAddSong = false;
        }
        else
        {
            DisplayAddSong = true;
            BasicPanelViewModel.IsEnabled = false;
            AdvancedPanelViewModel.IsEnabled = false;
            DisplayCompleteCheckbox = false;
        }
        
        IsEnabled = true;
        TreeData = treeData;
        HasBeenModified = false;

        if (songInfo != null)
        {
            LastModifiedDate = songInfo.LastModifiedDate;
        }
    }
    
    public override ViewModelBase DesignerExample()
    {
        return new MsuSongOuterPanelViewModel
        {
            TrackTitleText = "#1 Opening Theme - Primary Song",
            TrackDescriptionText = "Song played when first booting up the game after the three Triforce pieces start flying in and goes into the title screen. Lasts for about 16.5 seconds and does not loop. Not used in SMZ3.",
            HasTrackDescription = true,
            DisplayAddSong = false,
            DisplayCompleteCheckbox = true,
            BasicPanelViewModel = new MsuSongBasicPanelViewModel()
            {
                IsEnabled = false
            },
            AdvancedPanelViewModel = new MsuSongAdvancedPanelViewModel()
            {
                IsEnabled = false
            }
        };
    }

    public override void SaveChanges()
    {
        if (BasicPanelViewModel.IsEnabled)
        {
            BasicPanelViewModel.SaveChanges();
            if (BasicPanelViewModel.LastModifiedDate > LastModifiedDate)
            {
                LastModifiedDate = BasicPanelViewModel.LastModifiedDate;
            }
        }
        else if (AdvancedPanelViewModel.IsEnabled)
        {
            AdvancedPanelViewModel.SaveChanges();
            if (AdvancedPanelViewModel.LastModifiedDate > LastModifiedDate)
            {
                LastModifiedDate = AdvancedPanelViewModel.LastModifiedDate;
            }
        }
        
        HasBeenModified = false;
    }
}