using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaControls.Models;
using MSUScripter.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class MsuSongMsuPcmInfoViewModel : ViewModelBase
{

    public MsuSongMsuPcmInfoViewModel()
    {
        PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != nameof(LastModifiedDate) && args.PropertyName != nameof(HasBeenModified))
            {
                LastModifiedDate = DateTime.Now;
            }
        };

        SubTracks.CollectionChanged += (sender, args) =>
        {
            this.RaisePropertyChanged(nameof(SubTracks));
        };
        
        SubChannels.CollectionChanged += (sender, args) =>
        {
            this.RaisePropertyChanged(nameof(SubChannels));
        };
    }
    
    [Reactive] public int? Loop { get; set; }

    [Reactive] public int? TrimStart { get; set; }

    [Reactive] public int? TrimEnd { get; set; }

    [Reactive] public int? FadeIn { get; set; }

    [Reactive] public int? FadeOut { get; set; }

    [Reactive] public int? CrossFade { get; set; }

    [Reactive] public int? PadStart { get; set; }

    [Reactive] public int? PadEnd { get; set; }

    [Reactive] public double? Tempo { get; set; }

    [Reactive] public double? Normalization { get; set; }

    [Reactive] public bool? Compression { get; set; }

    [Reactive] public string? Output { get; set; }

    [Reactive] public DateTime LastModifiedDate { get; set; }

    [Reactive] public ObservableCollection<MsuSongMsuPcmInfoViewModel> SubTracks { get; init; } = [];

    [Reactive] public ObservableCollection<MsuSongMsuPcmInfoViewModel> SubChannels { get; init; } = [];
    
    [SkipConvert] public bool HasFile => !string.IsNullOrEmpty(File) && System.IO.File.Exists(File);
    
    [SkipConvert, Reactive]
    public bool DisplayHertzWarning { get; set; }

    [SkipConvert, Reactive]
    public bool DisplayMultiWarning { get; set; }

    [SkipConvert, Reactive]
    public bool DisplaySubTrackSubChannelWarning { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(HasFile))] 
    public string? File { get; set; }

    [Reactive] public bool ShowPanel { get; set; } = true;
    
    public bool CanDisplayTrimStartButton => OperatingSystem.IsWindows();

    [SkipConvert]
    public MsuProjectViewModel Project { get; set; } = null!;
    
    [SkipConvert]
    public MsuSongInfoViewModel Song { get; set; } = null!;
    
    [SkipConvert]
    public bool IsAlt { get; set; }

    [SkipConvert] 
    public bool IsTopLevel => ParentMsuPcmInfo == null;
    
    [SkipConvert]
    public MsuSongMsuPcmInfoViewModel? ParentMsuPcmInfo { get; set; }

    [SkipConvert]
    public MsuSongMsuPcmInfoViewModel TopLevel
    {
        get
        {
            if (IsTopLevel) return this;
            var topModel = ParentMsuPcmInfo;
            while (topModel?.ParentMsuPcmInfo != null)
                topModel = topModel.ParentMsuPcmInfo;
            return topModel ?? this;
        }
    }

    public bool CanDelete => !IsTopLevel;

    public bool CanEditFile => !SubTracks.Any() && !SubChannels.Any();

    public bool HasSubChannels => SubChannels.Any();

    public bool HasSubTracks => SubTracks.Any();

    public bool HasChangesSince(DateTime time)
    {
        if (SubTracks.Any(x => x.HasChangesSince(time)))
            return true;
        if (SubChannels.Any(x => x.HasChangesSince(time)))
            return true;
        return LastModifiedDate > time;
    }

    public bool HasFiles()
    {
        return !string.IsNullOrEmpty(File) || SubTracks.Any(x => x.HasFiles()) || SubChannels.Any(x => x.HasFiles());
    }
    
    public int GetFileCount()
    {
        var fileCount = !string.IsNullOrEmpty(File) ? 1 : 0;
        return fileCount + SubTracks.Sum(x => x.GetFileCount()) + SubChannels.Sum(x => x.GetFileCount());
    }

    public void ApplyCascadingSettings(MsuProjectViewModel projectModel, MsuSongInfoViewModel songModel, bool isAlt, MsuSongMsuPcmInfoViewModel? parent, bool canPlaySongs, bool updateLastModified, bool forceOpen)
    {
        var lastModified = updateLastModified ? DateTime.Now : LastModifiedDate;
        Project = projectModel;
        Song = songModel;
        IsAlt = isAlt;
        ParentMsuPcmInfo = parent;
        CanPlaySongs = canPlaySongs;

        if (forceOpen)
        {
            ShowPanel = forceOpen;
        }

        foreach (var subItem in SubChannels.Concat(SubTracks))
        {
            subItem.ApplyCascadingSettings(projectModel, songModel, isAlt, this, canPlaySongs, updateLastModified, forceOpen);
        }

        LastModifiedDate = lastModified;
    }

    [SkipConvert]
    public bool IsSubChannel => !IsTopLevel && ParentMsuPcmInfo?.SubChannels.Contains(this) == true;
    
    [SkipConvert]
    public bool IsSubTrack => !IsTopLevel && ParentMsuPcmInfo?.SubTracks.Contains(this) == true;

    [SkipConvert]
    public string InsertText => IsSubChannel ? "Insert New Sub Channel Before This" : "Insert New Sub Track Before This";

    [SkipConvert]
    public string HeaderText =>
        IsTopLevel ? "MsuPcm++ Details" : IsSubChannel ? "Sub Channel Details" : "Sub Track Details";
    
    [SkipConvert]
    public string RemoveText =>
        IsSubChannel ? "Remove Sub Channel" : "Remove Sub Track";

    [SkipConvert] public bool CanPlaySongs { get; set; }
    
    public void UpdateHertzWarning(int? sampleRate)
    {
        DisplayHertzWarning = sampleRate != 44100;
    }
    
    public void UpdateMultiWarning()
    {
        DisplayMultiWarning = GetFiles().Distinct().Count() > 1;
    }

    public void UpdateSubTrackSubChannelWarning()
    {
        DisplaySubTrackSubChannelWarning = HasBothSubTracksAndChannels;
    }

    private List<string> GetFiles()
    {
        List<string> toReturn = [];

        if (!string.IsNullOrEmpty(File))
        {
            toReturn.Add(File);
        }

        toReturn.AddRange(SubChannels.Concat(SubTracks).SelectMany(x => x.GetFiles()));

        return toReturn;
    }

    private bool HasBothSubTracksAndChannels
    {
        get
        {
            if (HasSubChannels && HasSubTracks)
            {
                return true;
            }

            return SubChannels.Concat(SubTracks).Any(x => x.HasBothSubTracksAndChannels);
        }
            
    }

    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}