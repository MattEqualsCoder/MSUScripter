using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MSUScripter.Configs;

namespace MSUScripter.ViewModels;

public class MsuBasicInfoViewModel : INotifyPropertyChanged
{
    private string _msuType = "";
    public string MsuType
    {
        get => _msuType;
        set => SetField(ref _msuType, value);
    }

    private string _game = "";
    public string Game
    {
        get => _game;
        set => SetField(ref _game, value);
    }

    private string _packName = "";
    public string PackName
    {
        get => _packName;
        set => SetField(ref _packName, value);
    }

    private string _packCreator = "";
    public string PackCreator
    {
        get => _packCreator;
        set => SetField(ref _packCreator, value);
    }

    private string _packVersion = "";
    public string PackVersion
    {
        get => _packVersion;
        set => SetField(ref _packVersion, value);
    }

    private string _artist = "";
    public string Artist
    {
        get => _artist;
        set => SetField(ref _artist, value);
    }

    private string _album = "";
    public string Album
    {
        get => _album;
        set => SetField(ref _album, value);
    }

    private string _url = "";
    public string Url
    {
        get => _url;
        set => SetField(ref _url, value);
    }

    private double? _normalization;
    public double? Normalization
    {
        get => _normalization;
        set => SetField(ref _normalization, value);
    }

    private bool? _dither;
    public bool? Dither
    {
        get => _dither;
        set => SetField(ref _dither, value);
    }

    public bool _isMsuPcmProject = true;
    public bool IsMsuPcmProject
    {
        get => _isMsuPcmProject;
        set => SetField(ref _isMsuPcmProject, value);
    }
    
    public DateTime _lastModifiedDate;
    public DateTime LastModifiedDate
    {
        
        get => _lastModifiedDate;
        set => SetField(ref _lastModifiedDate, value);
    }
    
    public bool _createAltSwapper = true;
    public bool CreateAltSwapper
    {
        
        get => _createAltSwapper;
        set => SetField(ref _createAltSwapper, value);
    }
    
    public bool _createSplitSmz3Script = true;
    public bool CreateSplitSmz3Script
    {
        
        get => _createSplitSmz3Script;
        set => SetField(ref _createSplitSmz3Script, value);
    }
    
    public bool _isSmz3Project = true;
    public bool IsSmz3Project
    {
        
        get => _isSmz3Project;
        set => SetField(ref _isSmz3Project, value);
    }
    
    public string? _zeldaMsuPath;
    public string? ZeldaMsuPath
    {
        
        get => _zeldaMsuPath;
        set => SetField(ref _zeldaMsuPath, value);
    }
    
    public string? _metroidMsuPath;
    public string? MetroidMsuPath
    {
        
        get => _metroidMsuPath;
        set => SetField(ref _metroidMsuPath, value);
    }
    
    public string _trackList = TrackListType.List;
    public string TrackList
    {
        
        get => _trackList;
        set
        {
            SetField(ref _trackList, value);
            OnPropertyChanged(nameof(WriteTrackList));
        }
    }

    public bool WriteTrackList => _trackList != TrackListType.Disabled;
    
    public bool _writeYamlFile;
    public bool WriteYamlFile
    {
        
        get => _writeYamlFile;
        set => SetField(ref _writeYamlFile, value);
    }
    
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public bool HasChangesSince(DateTime time)
    {
        return LastModifiedDate > time;
    }

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