using System;
using System.Collections.Generic;
using System.ComponentModel;
using AvaloniaControls.Models;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.Text;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class MsuSongBasicPanelViewModel : SavableViewModelBase
{
    [Reactive, SkipLastModified] public bool IsScratchPad { get; set; }
    
    [Reactive] public string? SongName { get; set; }

    [Reactive] public string? ArtistName { get; set; }

    [Reactive] public string? Album { get; set; }

    [Reactive] public string? Url { get; set; }

    [Reactive] public string? OutputFilePath { get; set; }

    [Reactive] public bool IsAlt { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(HasSelectedInputFile))] public string? InputFilePath { get; set; }

    [Reactive, SkipLastModified] public bool PyMusicLooperRunning { get; set; }
    [Reactive, SkipLastModified] public bool CanUpdatePcmFile { get; set; }
    [Reactive] public int? TrimStart { get; set; }
    [Reactive] public int? TrimEnd { get; set; }
    [Reactive] public int? LoopPoint { get; set; }
    [Reactive] public double? Normalization { get; set; }
    
    [Reactive] public bool? CheckCopyright { get; set; }
    
    [Reactive] public bool? IsCopyrightSafe { get; set; }
    [Reactive, SkipLastModified] public bool DisplayOutputFile { get; set; } = true;
    [Reactive, SkipLastModified] public bool DisplayInputFile { get; set; } = true;
    [Reactive, SkipLastModified] public bool IsEnabled { get; set; } = true;
    [Reactive, SkipLastModified] public bool IsAdvancedMode { get; set; }
    [Reactive, SkipLastModified] public bool EnableMsuPcm { get; set; } = true;
    [Reactive, SkipLastModified] public int InputColumnSpan { get; set; } = 2;
    [Reactive, SkipLastModified] public int OutputColumn { get; set; } = 2;
    [Reactive, SkipLastModified] public int OutputColumnSpan { get; set; } = 2;
    [Reactive, SkipLastModified] public ApplicationText Text { get; set; } = ApplicationText.CurrentLanguageText;
    [Reactive, SkipLastModified] public bool DisplaySampleRateWarning { get; set; }
    public MsuProject? Project { get; set; }
    public bool HasSelectedInputFile => !string.IsNullOrEmpty(InputFilePath);
    
    private MsuTrackInfo? _currentTrackInfo;
    private MsuSongInfo? _currentSongInfo;
    private MsuProjectWindowViewModelTreeData? _treeData;
    private bool _updatingModel;

    public event EventHandler? ViewModelUpdated;

    public MsuSongBasicPanelViewModel() : base()
    {
        
        PropertyChanged += OnPropertyChanged;
        return;

        void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_updatingModel) return;
            if (e.PropertyName == nameof(SongName) && _treeData is { ParentTreeData: not null } && !string.IsNullOrEmpty(SongName))
            {
                _treeData.Name = SongName ?? "Test";
            }
            else if (e.PropertyName is nameof(CheckCopyright) or nameof(IsCopyrightSafe))
            {
                SaveChanges();
                _treeData?.UpdateCompletedFlag();
                _treeData?.ParentTreeData?.UpdateCompletedFlag();
            }
        }
    }
    
    public void UpdateViewModel(MsuProject project, MsuTrackInfo trackInfo, MsuSongInfo songInfo, MsuProjectWindowViewModelTreeData treeData)
    {
        Project = project;
        
        _updatingModel = true;
        _currentSongInfo = songInfo;
        _treeData = treeData;
        
        SongName = songInfo.SongName;
        ArtistName = songInfo.Artist;
        Album = songInfo.Album;
        Url = songInfo.Url;
        InputFilePath = songInfo.MsuPcmInfo.File;
        OutputFilePath = songInfo.OutputPath;
        IsScratchPad = trackInfo.IsScratchPad;
        TrimStart = songInfo.MsuPcmInfo.TrimStart;
        TrimEnd = songInfo.MsuPcmInfo.TrimEnd;
        LoopPoint = songInfo.MsuPcmInfo.Loop;
        Normalization = songInfo.MsuPcmInfo.Normalization;
        CheckCopyright = songInfo.CheckCopyright;
        IsCopyrightSafe = songInfo.IsCopyrightSafe;
        CanUpdatePcmFile = songInfo.IsAlt;
        DisplayOutputFile = !IsScratchPad;
        EnableMsuPcm = project.BasicInfo.IsMsuPcmProject;

        if (EnableMsuPcm && IsScratchPad)
        {
            DisplayInputFile = true;
            InputColumnSpan = 4;

            DisplayOutputFile = false;
            OutputColumn = 2;
            OutputColumnSpan = 2;
        }
        else if (EnableMsuPcm && !IsScratchPad)
        {
            DisplayInputFile = true;
            InputColumnSpan = 2;

            DisplayOutputFile = true;
            OutputColumn = 2;
            OutputColumnSpan = 2;
        }
        else if (!EnableMsuPcm && IsScratchPad)
        {
            DisplayInputFile = false;
            DisplayOutputFile = false;
        }
        else if (!EnableMsuPcm && !IsScratchPad)
        {
            DisplayInputFile = false;
            InputColumnSpan = 2;

            DisplayOutputFile = true;
            OutputColumn = 0;
            OutputColumnSpan = 4;
        }

        IsAdvancedMode = false;
        IsEnabled = true;
        HasBeenModified = false;
        _updatingModel = false;
        LastModifiedDate = songInfo.LastModifiedDate;
        ViewModelUpdated?.Invoke(this, EventArgs.Empty);
    }

    public override void SaveChanges()
    {
        if (_currentSongInfo == null || !HasBeenModified) return;
        _currentSongInfo.SongName = SongName;
        _currentSongInfo.Artist = ArtistName;
        _currentSongInfo.Album = Album;
        _currentSongInfo.Url = Url;
        _currentSongInfo.CheckCopyright = CheckCopyright;
        _currentSongInfo.IsCopyrightSafe = IsCopyrightSafe;
        _currentSongInfo.OutputPath = OutputFilePath;
        _currentSongInfo.MsuPcmInfo.File = InputFilePath;
        _currentSongInfo.MsuPcmInfo.TrimStart = TrimStart;
        _currentSongInfo.MsuPcmInfo.TrimEnd = TrimEnd;
        _currentSongInfo.MsuPcmInfo.Loop = LoopPoint;
        _currentSongInfo.MsuPcmInfo.Normalization = Normalization;
        HasBeenModified = false;
        Console.WriteLine("Saved Changes");
    }

    public override ViewModelBase DesignerExample()
    {
        return new MsuSongBasicPanelViewModel()
        {
            SongName = "Test Song",
            ArtistName = "Test Song Artist",
            Album = "Test Song Album",
            Url = "https://www.google.com",
            IsAlt = false,
        };
    }

    public void SetSampleRate(int sampleRate)
    {
        DisplaySampleRateWarning = sampleRate != 44100;
    }
}