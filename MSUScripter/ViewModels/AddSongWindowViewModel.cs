using System;
using System.Collections.Generic;
using Avalonia.Media;
using AvaloniaControls.Models;
using Material.Icons;
using MSUScripter.Configs;
using ReactiveUI.Fody.Helpers;
#pragma warning disable CS0067 // Event is never used

namespace MSUScripter.ViewModels;

public class AddSongWindowViewModel : ViewModelBase
{
    [Reactive] public string? SongName { get; set; }
    [Reactive] public bool DisplayHertzWarning { get; set; }
    [Reactive] public string? ArtistName { get; set; }
    [Reactive] public string? AlbumName { get; set; }
    [Reactive, ReactiveLinkedEvent(nameof(TrimStartUpdated))] public int? TrimStart { get; set; }
    [Reactive] public int? TrimEnd { get; set; }
    [Reactive] public int? LoopPoint { get; set; }
    [Reactive] public double? Normalization { get; set; }
    [Reactive] public string? AverageAudio { get; set; }
    [Reactive] public string AddSongButtonText { get; set; } = "Add Song";
    [Reactive] public bool RunningPyMusicLooper { get; set; }
    [Reactive] public bool SingleMode { get; set; }
    public bool ShowPyMusicLooper => !string.IsNullOrEmpty(FilePath);
    
    [Reactive, ReactiveLinkedProperties(nameof(HasAudioAnalysis))]
    public string? PeakAudio { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(CanEditMainFields), nameof(CanAddSong), nameof(ShowPyMusicLooper))]
    public string FilePath { get; set; } = "";
    
    [Reactive, ReactiveLinkedProperties(nameof(CanAddSong))] 
    public MsuTrackInfoViewModel? SelectedTrack { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(CopyrightIconKind), nameof(CopyrightIconBrush), nameof(CopyrightSafeText))]
    public bool? IsCopyrightSafe { get; set; }

    [Reactive, ReactiveLinkedProperties(nameof(CheckCopyrightIconKind))]
    public bool CheckCopyright { get; set; } = true;
    
    public MaterialIconKind CheckCopyrightIconKind => CheckCopyright switch
    {
        true => MaterialIconKind.CheckboxOutline,
        false => MaterialIconKind.CheckboxBlankOutline,
    };
    
    public MaterialIconKind CopyrightIconKind => IsCopyrightSafe switch
    {
        true => MaterialIconKind.CheckboxOutline,
        false => MaterialIconKind.CancelBoxOutline,
        _ => MaterialIconKind.QuestionBoxOutline
    };
    
    public IBrush CopyrightIconBrush => IsCopyrightSafe switch
    {
        true => Brushes.LimeGreen,
        false => Brushes.IndianRed,
        _ => Brushes.Goldenrod
    };
    
    public string CopyrightSafeText => IsCopyrightSafe switch
    {
        true => "Safe",
        false => "Unsafe",
        _ => "Untested"
    };

    public List<ComboBoxAndSearchItem> TrackSearchItems { get; set; } = [];

    public bool HasAudioAnalysis => !string.IsNullOrEmpty(PeakAudio);

    public bool CanEditMainFields => !string.IsNullOrEmpty(FilePath);

    public bool CanAddSong => !string.IsNullOrEmpty(FilePath) && SelectedTrack != null && !RunningPyMusicLooper;

    public MsuProjectViewModel MsuProjectViewModel { get; set; } = new();

    public MsuProject MsuProject { get; set; } = new();

    public event EventHandler? TrimStartUpdated;

    public void Clear()
    {
        FilePath = "";
        SongName = "";
        ArtistName = "";
        AlbumName = "";
        TrimStart = null;
        TrimEnd = null;
        LoopPoint = null;
        Normalization = null;
        AverageAudio = null;
        PeakAudio = null;
        HasBeenModified = false;
    }

    public override ViewModelBase DesignerExample()
    {
        FilePath = "C:\\Test.mp3";
        SongName = "Test Song Name";
        ArtistName = "Test Song Artist";
        TrimStart = 10000;
        TrimEnd = 1000000;
        LoopPoint = 50000;
        TrackSearchItems =
        [
            new ComboBoxAndSearchItem(null, "Track", "Track Description")
        ];
        return this;
    }
}