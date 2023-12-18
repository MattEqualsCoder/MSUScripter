using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MSUScripter.Models;
using MSUScripter.Tools;

namespace MSUScripter.ViewModels;

public class MsuSongInfoViewModel : INotifyPropertyChanged
{
    private int _trackNumber;
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

    private string? _songName;
    public string? SongName
    {
        get => _songName;
        set => SetField(ref _songName, value);
    }

    private string? _artist;
    public string? Artist
    {
        get => _artist;
        set => SetField(ref _artist, value);
    }

    private string? _album;
    public string? Album
    {
        get => _album;
        set => SetField(ref _album, value);
    }

    private string? _url;
    public string? Url
    {
        get => _url;
        set => SetField(ref _url, value);
    }

    private string? _outputPath = "";
    public string? OutputPath
    {
        get => _outputPath;
        set => SetField(ref _outputPath, value);
    }

    private bool _isAlt;
    public bool IsAlt
    {
        get => _isAlt;
        set => SetField(ref _isAlt, value);
    }
    
    private bool _checkCopyright = true;
    public bool CheckCopyright
    {
        get => _checkCopyright;
        set => SetField(ref _checkCopyright, value);
    }
    
    private DateTime _lastModifiedDate;
    public DateTime LastModifiedDate
    {
        
        get => _lastModifiedDate;
        set => SetField(ref _lastModifiedDate, value);
    }
    
    private DateTime _lastGeneratedDate;
    public DateTime LastGeneratedDate
    {
        get => _lastGeneratedDate;
        set => SetField(ref _lastGeneratedDate, value);
    }

    [SkipConvert]
    public MsuProjectViewModel Project { get; set; } = null!;
    
    public MsuSongMsuPcmInfoViewModel MsuPcmInfo { get; set; } = new();
    
    public bool HasChangesSince(DateTime time)
    {
        return MsuPcmInfo.HasChangesSince(time) || LastModifiedDate > time;
    }

    public bool HasFiles()
    {
        return MsuPcmInfo.HasFiles();
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        if (propertyName != nameof(LastModifiedDate) && propertyName != nameof(LastGeneratedDate))
        {
            LastModifiedDate = DateTime.Now;
        }
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}