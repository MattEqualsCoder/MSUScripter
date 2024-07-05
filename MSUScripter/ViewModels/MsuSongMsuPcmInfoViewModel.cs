using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using MSUScripter.Models;

namespace MSUScripter.ViewModels;

public class MsuSongMsuPcmInfoViewModel : INotifyPropertyChanged
{
    
    private int? _loop;
    public int? Loop
    {
        get => _loop;
        set => SetField(ref _loop, value);
    }

    private int? _trimStart;
    public int? TrimStart
    {
        get => _trimStart;
        set => SetField(ref _trimStart, value);
    }

    private int? _trimEnd;
    public int? TrimEnd
    {
        get => _trimEnd;
        set => SetField(ref _trimEnd, value);
    }

    private int? _fadeIn;
    public int? FadeIn
    {
        get => _fadeIn;
        set => SetField(ref _fadeIn, value);
    }

    private int? _fadeOut;
    public int? FadeOut
    {
        get => _fadeOut;
        set => SetField(ref _fadeOut, value);
    }

    private int? _crossFade;
    public int? CrossFade
    {
        get => _crossFade;
        set => SetField(ref _crossFade, value);
    }

    private int? _padStart;
    public int? PadStart
    {
        get => _padStart;
        set => SetField(ref _padStart, value);
    }

    private int? _padEnd;
    public int? PadEnd
    {
        get => _padEnd;
        set => SetField(ref _padEnd, value);
    }

    private double? _tempo;
    public double? Tempo
    {
        get => _tempo;
        set => SetField(ref _tempo, value);
    }

    private double? _normalization;
    public double? Normalization
    {
        get => _normalization;
        set => SetField(ref _normalization, value);
    }

    private bool? _compression;
    public bool? Compression
    {
        get => _compression;
        set => SetField(ref _compression, value);
    }

    private string? _file;
    public string? File
    {
        get => _file;
        set
        {
            SetField(ref _file, value);
            HasFile = !string.IsNullOrEmpty(_file) && System.IO.File.Exists(_file);
        }
    }
    
    private string? _output;
    public string? Output
    {
        get => _output;
        set => SetField(ref _output, value);
    }

    private DateTime _lastModifiedDate;
    public DateTime LastModifiedDate
    {
        get => _lastModifiedDate;
        set => SetField(ref _lastModifiedDate, value);
    }

    public bool CanDisplayTrimStartButton => OperatingSystem.IsWindows();
    
    private ObservableCollection<MsuSongMsuPcmInfoViewModel> _subTracks = [];
    public ObservableCollection<MsuSongMsuPcmInfoViewModel> SubTracks
    {
        get => _subTracks;
        set
        {
            SetField(ref _subTracks, value);
            OnPropertyChanged(nameof(CanEditFile));
        }
    }

    private ObservableCollection<MsuSongMsuPcmInfoViewModel> _subChannels = [];
    public ObservableCollection<MsuSongMsuPcmInfoViewModel> SubChannels
    {
        get => _subChannels;
        set
        {
            SetField(ref _subChannels, value);
            OnPropertyChanged(nameof(CanEditFile));
        }
    }
    
    private bool _hasFile;

    [SkipConvert]
    public bool HasFile
    {
        get => _hasFile;
        set => SetField(ref _hasFile, value);
    }
    
    private bool _displayHertzWarning;

    [SkipConvert]
    public bool DisplayHertzWarning
    {
        get => _displayHertzWarning;
        set => SetField(ref _displayHertzWarning, value);
    }
    
    private bool _displayMultiWarning;

    [SkipConvert]
    public bool DisplayMultiWarning
    {
        get => _displayMultiWarning;
        set => SetField(ref _displayMultiWarning, value);
    }

    public void AddSubChannel(int? index = null)
    {
        if (index is null or -1)
        {
            SubChannels.Add(new MsuSongMsuPcmInfoViewModel
                { Project = Project, Song = Song, IsAlt = IsAlt, ParentMsuPcmInfo = this });
        }
        else
        {
            SubChannels.Insert(index.Value, new MsuSongMsuPcmInfoViewModel
                { Project = Project, Song = Song, IsAlt = IsAlt, ParentMsuPcmInfo = this });
        }
        
        LastModifiedDate = DateTime.Now;
    }

    public void RemoveSubChannel(MsuSongMsuPcmInfoViewModel model)
    {
        SubChannels.Remove(model);
        LastModifiedDate = DateTime.Now;
    }
    
    public void AddSubTrack(int? index = null)
    {
        if (index is null or -1)
        {
            SubTracks.Add(new MsuSongMsuPcmInfoViewModel
                { Project = Project, Song = Song, IsAlt = IsAlt, ParentMsuPcmInfo = this });
        }
        else
        {
            SubTracks.Insert(index.Value, new MsuSongMsuPcmInfoViewModel
                { Project = Project, Song = Song, IsAlt = IsAlt, ParentMsuPcmInfo = this });
        }
        LastModifiedDate = DateTime.Now;
    }

    public void RemoveSubTrack(MsuSongMsuPcmInfoViewModel model)
    {
        SubTracks.Remove(model);
        LastModifiedDate = DateTime.Now;
    }

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

    public bool CanDelete => !IsTopLevel;

    public bool CanEditFile => !_subTracks.Any() && !_subChannels.Any();

    public bool HasSubChannels => _subChannels.Any();

    public bool HasSubTracks => _subTracks.Any();

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

    public void ApplyCascadingSettings(MsuProjectViewModel projectModel, MsuSongInfoViewModel songModel, bool isAlt, MsuSongMsuPcmInfoViewModel? parent, bool updateLastModified)
    {
        Project = projectModel;
        Song = songModel;
        IsAlt = isAlt;
        ParentMsuPcmInfo = parent;

        if (updateLastModified)
        {
            LastModifiedDate = DateTime.Now;
        }

        foreach (var subItem in _subChannels.Concat(_subTracks))
        {
            subItem.ApplyCascadingSettings(projectModel, songModel, isAlt, this, updateLastModified);
        }
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
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        if (propertyName != nameof(LastModifiedDate))
        {
            LastModifiedDate = DateTime.Now;
        }
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}