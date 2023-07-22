using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSUScripter.ViewModels;

public class MsuInformationViewModel : INotifyPropertyChanged
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

    private string _outputPrefix = "";
    public string OutputPrefix
    {
        get => _outputPrefix;
        set => SetField(ref _outputPrefix, value);
    }

    private double _normalization;
    public double Normalization
    {
        get => _normalization;
        set => SetField(ref _normalization, value);
    }

    private bool _dither;
    public bool Dither
    {
        get => _dither;
        set => SetField(ref _dither, value);
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