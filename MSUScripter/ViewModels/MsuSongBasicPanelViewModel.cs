using System;
using System.ComponentModel;
using AvaloniaControls.Models;
using MSUScripter.Configs;
using MSUScripter.Models;
using ReactiveUI.SourceGenerators;

namespace MSUScripter.ViewModels;

public partial class MsuSongBasicPanelViewModel : SavableViewModelBase
{
    [Reactive, SkipLastModified] public partial bool IsScratchPad { get; set; }
    
    [Reactive] public partial string? SongName { get; set; }

    [Reactive] public partial string? ArtistName { get; set; }

    [Reactive] public partial string? Album { get; set; }

    [Reactive] public partial string? Url { get; set; }

    [Reactive] public partial string? OutputFilePath { get; set; }

    [Reactive] public partial bool IsAlt { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(HasSelectedInputFile))] public partial string? InputFilePath { get; set; }

    [Reactive, SkipLastModified] public partial bool PyMusicLooperRunning { get; set; }
    [Reactive, SkipLastModified] public partial bool CanUpdatePcmFile { get; set; }
    [Reactive] public partial int? TrimStart { get; set; }
    [Reactive] public partial int? TrimEnd { get; set; }
    [Reactive] public partial int? LoopPoint { get; set; }
    [Reactive] public partial double? Normalization { get; set; }
    
    [Reactive] public partial bool? CheckCopyright { get; set; }
    
    [Reactive] public partial bool? IsCopyrightSafe { get; set; }
    [Reactive, SkipLastModified] public partial bool DisplayOutputFile { get; set; }
    [Reactive, SkipLastModified] public partial bool DisplayInputFile { get; set; }
    [Reactive, SkipLastModified] public partial bool IsEnabled { get; set; }
    [Reactive, SkipLastModified] public partial bool IsAdvancedMode { get; set; }
    [Reactive, SkipLastModified, ReactiveLinkedProperties(nameof(DisplayPyMusicLooperPanel))] public partial bool EnableMsuPcm { get; set; }
    [Reactive, SkipLastModified, ReactiveLinkedProperties(nameof(DisplayPyMusicLooperPanel))] public partial bool PyMusicLooperEnabled { get; set; }
    [Reactive, SkipLastModified] public partial int InputColumnSpan { get; set; }
    [Reactive, SkipLastModified] public partial int OutputColumn { get; set; }
    [Reactive, SkipLastModified] public partial int OutputColumnSpan { get; set; }
    [Reactive, SkipLastModified] public partial bool DisplaySampleRateWarning { get; set; }
    public bool DisplayPyMusicLooperPanel => EnableMsuPcm && PyMusicLooperEnabled;
    public MsuProject? Project { get; private set; }
    public bool HasSelectedInputFile => !string.IsNullOrEmpty(InputFilePath);

    private MsuSongInfo? _currentSongInfo;
    private MsuProjectWindowViewModelTreeData? _treeData;
    private bool _updatingModel;

    public event EventHandler? ViewModelUpdated;
    public event EventHandler? FileDragDropped;

    public MsuSongBasicPanelViewModel()
    {
        DisplayOutputFile = true;
        DisplayInputFile = true;
        IsEnabled = true;
        EnableMsuPcm = true;
        PyMusicLooperEnabled = true;
        InputColumnSpan = 2;
        OutputColumn = 2;
        OutputColumnSpan = 2;
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
        _currentSongInfo.MsuPcmInfo.Output = OutputFilePath;
        HasBeenModified = false;
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
    
    public void DragDropFile(string fileName)
    {
        if (!DisplayInputFile) return;
        InputFilePath = fileName;
        FileDragDropped?.Invoke(this, EventArgs.Empty);
    }
}