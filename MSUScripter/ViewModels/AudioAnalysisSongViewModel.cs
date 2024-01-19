using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MSUScripter.Models;
using MSUScripter.Services;

namespace MSUScripter.ViewModels;

public class AudioAnalysisSongViewModel : INotifyPropertyChanged
{
    private string _songName = "";
    public string SongName
    {
        get => _songName;
        set => SetField(ref _songName, value);
    }
    
    private MsuSongInfoViewModel? _originalViewModel;
    public MsuSongInfoViewModel? OriginalViewModel
    {
        get => _originalViewModel;
        set => SetField(ref _originalViewModel, value);
    }
    
    private int _trackNumber = 0;
    public int TrackNumber
    {
        get => _trackNumber;
        set => SetField(ref _trackNumber, value);
    }
    
    private string _trackName = "";
    public string TrackName
    {
        get => _trackName;
        set => SetField(ref _trackName, value);
    }
    
    private string _path = "";
    public string Path
    {
        get => _path;
        set => SetField(ref _path, value);
    }
    
    private double? _avgDecibals;
    public double? AvgDecibals
    {
        get => _avgDecibals;
        set => SetField(ref _avgDecibals, value);
    }
    
    private double? _maxDecibals;
    public double? MaxDecibals
    {
        get => _maxDecibals;
        set => SetField(ref _maxDecibals, value);
    }

    private bool _hasLoaded;

    public bool HasLoaded
    {
        get => _hasLoaded;
        set => SetField(ref _hasLoaded, value);
    }
    
    private bool _hasWarning;

    public bool HasWarning
    {
        get => _hasWarning;
        set => SetField(ref _hasWarning, value);
    }
    
    public string _warningMessage = "";

    public string WarningMessage
    {
        get => _warningMessage;
        set => SetField(ref _warningMessage, value);
    }
    

    public void ApplyAudioAnalysis(AnalysisDataOutput data)
    {
        AvgDecibals = data.AvgDecibals;
        MaxDecibals = data.MaxDecibals;
        HasLoaded = true;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}