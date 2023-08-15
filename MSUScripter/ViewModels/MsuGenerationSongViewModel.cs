using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSUScripter.ViewModels;

public class MsuGenerationSongViewModel : INotifyPropertyChanged
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

    public string _message = "";

    public string Message
    {
        get => _message;
        set => SetField(ref _message, value);
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