using System;
using AvaloniaControls.Models;
using Material.Icons;
using MSUScripter.Configs;
using MSUScripter.Models;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class MsuSongInfoViewModel : ViewModelBase
{
    public MsuSongInfoViewModel()
    {
        PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != nameof(LastModifiedDate) && args.PropertyName != nameof(HasBeenModified))
            {
                LastModifiedDate = DateTime.Now;
            }
        };
    }
    
    public int TrackNumber { get; set; }

    public string TrackName { get; set; } = "";

    [Reactive] public string? SongName { get; set; }

    [Reactive] public string? Artist { get; set; }

    [Reactive] public string? Album { get; set; }

    [Reactive] public string? Url { get; set; }

    [Reactive] public string? OutputPath { get; set; }

    [Reactive] public bool IsAlt { get; set; }

    public bool DisplayPcmFile => !Track.IsScratchPad;
    
    [Reactive] 
    public bool IsComplete { get; set; }
    
    [Reactive] public bool CheckCopyright { get; set; }
    
    [Reactive] public DateTime LastModifiedDate { get; set; }
    
    [Reactive] public DateTime LastGeneratedDate { get; set; }

    [SkipConvert] public MsuProjectViewModel Project { get; set; } = null!;
    
    [SkipConvert] public MsuTrackInfoViewModel Track { get; set; } = null!;
    
    [SkipConvert] public bool CanPlaySongs { get; set; }
    [Reactive, SkipConvert] public MaterialIconKind PauseStopIcon { get; set; }
    [Reactive, SkipConvert] public string PauseStopText { get; set; } = "Pause Song";

    [Reactive, SkipConvert] public string? AverageAudio { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(HasAudioAnalysis)), SkipConvert]
    public string? PeakAudio { get; set; }
    
    [SkipConvert] public bool HasAudioAnalysis => !string.IsNullOrEmpty(PeakAudio);

    [Reactive] public bool ShowPanel { get; set; } = true;
    public bool ShowCreatePcmSection => Project.BasicInfo.IsMsuPcmProject && !Track.IsScratchPad;
    public MsuSongMsuPcmInfoViewModel MsuPcmInfo { get; set; } = new();
    
    public bool HasChangesSince(DateTime time)
    {
        return MsuPcmInfo.HasChangesSince(time) || LastModifiedDate > time;
    }

    public bool HasFiles()
    {
        return MsuPcmInfo.HasFiles();
    }
    
    public void ApplyAudioMetadata(AudioMetadata metadata, bool force)
    {
        if (metadata.HasData != true) return;
        if (force || string.IsNullOrEmpty(SongName) || SongName.StartsWith("Track #"))
            SongName = metadata.SongName;
        if (force || (string.IsNullOrEmpty(Artist) && !string.IsNullOrEmpty(metadata.Artist)))
            Artist = metadata.Artist;
        if (force || (string.IsNullOrEmpty(Album) && !string.IsNullOrEmpty(metadata.Album)))
            Album = metadata.Album;
        if (force || (string.IsNullOrEmpty(Url) && !string.IsNullOrEmpty(metadata.Url)))
            Url = metadata.Url;
    }

    public void ApplyCascadingSettings(MsuProjectViewModel projectModel, MsuTrackInfoViewModel track, bool isAlt, bool canPlaySongs, bool updateLastModified, bool forceOpen)
    {
        var lastModified = updateLastModified ? DateTime.Now : LastModifiedDate;
        Project = projectModel;
        Track = track;
        TrackName = track.TrackName;
        TrackNumber = track.TrackNumber;
        IsAlt = isAlt;
        CanPlaySongs = canPlaySongs;

        if (forceOpen)
        {
            ShowPanel = true;
        }

        MsuPcmInfo.ApplyCascadingSettings(Project, this, isAlt, null, canPlaySongs, updateLastModified, forceOpen);

        LastModifiedDate = lastModified;
    }


    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}