using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

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
        set => SetField(ref _file, value);
    }
    
    private string? _output;
    public string? Output
    {
        get => _output;
        set => SetField(ref _output, value);
    }

    public DateTime _lastModifiedDate;
    public DateTime LastModifiedDate
    {
        
        get => _lastModifiedDate;
        set => SetField(ref _lastModifiedDate, value);
    }
    
    private ObservableCollection<MsuSongMsuPcmInfoViewModel> _subTracks = new ObservableCollection<MsuSongMsuPcmInfoViewModel>();
    public ObservableCollection<MsuSongMsuPcmInfoViewModel> SubTracks
    {
        get => _subTracks;
        set
        {
            SetField(ref _subTracks, value);
            OnPropertyChanged(nameof(CanEditFile));
            OnPropertyChanged(nameof(CanEditSubChannels));
        }
    }

    private ObservableCollection<MsuSongMsuPcmInfoViewModel> _subChannels = new ObservableCollection<MsuSongMsuPcmInfoViewModel>();
    public ObservableCollection<MsuSongMsuPcmInfoViewModel> SubChannels
    {
        get => _subChannels;
        set
        {
            SetField(ref _subChannels, value);
            OnPropertyChanged(nameof(CanEditFile));
            OnPropertyChanged(nameof(CanEditSubTracks));
        }
    }

    public void AddSubChannel()
    {
        SubChannels.Add(new MsuSongMsuPcmInfoViewModel() { Project = Project });
        OnPropertyChanged(nameof(CanEditSubTracks));
        LastModifiedDate = DateTime.Now;
    }

    public void RemoveSubChannel(MsuSongMsuPcmInfoViewModel model)
    {
        SubChannels.Remove(model);
        OnPropertyChanged(nameof(CanEditSubTracks));
        LastModifiedDate = DateTime.Now;
    }
    
    public void AddSubTrack()
    {
        SubTracks.Add(new MsuSongMsuPcmInfoViewModel() { Project = Project });
        OnPropertyChanged(nameof(CanEditSubChannels));
        LastModifiedDate = DateTime.Now;
    }

    public void RemoveSubTrack(MsuSongMsuPcmInfoViewModel model)
    {
        SubTracks.Remove(model);
        OnPropertyChanged(nameof(CanEditSubChannels));
        LastModifiedDate = DateTime.Now;
    }

    [SkipConvert]
    public MsuProjectViewModel Project { get; set; } = null!;
    
    [SkipConvert]
    public MsuSongInfoViewModel Song { get; set; } = null!;
    
    [SkipConvert]
    public bool IsTopLevel { get; set; }

    public bool CanDelete => !IsTopLevel;

    public bool CanEditFile => !_subTracks.Any() && !_subChannels.Any();

    public bool CanEditSubTracks => !_subChannels.Any();

    public bool CanEditSubChannels => !_subTracks.Any();

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