using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using MSUScripter.Models;
using MSUScripter.Tools;

namespace MSUScripter.ViewModels;

public class MsuTrackInfoViewModel : INotifyPropertyChanged
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
    
    private DateTime _lastModifiedDate;
    public DateTime LastModifiedDate
    {
        
        get => _lastModifiedDate;
        set => SetField(ref _lastModifiedDate, value);
    }
    
    [SkipConvert]
    public string? Description { get; set; }

    [SkipConvert] 
    public bool HasDescription => !string.IsNullOrEmpty(Description); 

    [SkipConvert] 
    public MsuProjectViewModel Project { get; set; } = new();
    
    [SkipConvert]
    public ObservableCollection<MsuSongInfoViewModel> Songs { get; set; } = new ObservableCollection<MsuSongInfoViewModel>();
    
    public bool HasChangesSince(DateTime time)
    {
        return Songs.Any(x => x.HasChangesSince(time)) || LastModifiedDate > time;
    }

    public void AddSong(MsuSongInfoViewModel song)
    {
        Songs.Add(song);
         _lastModifiedDate = DateTime.Now;
    }
    
    public void RemoveSong(MsuSongInfoViewModel? song)
    {
        if (song != null && Songs.Contains(song))
        {
            Songs.Remove(song);
            OnPropertyChanged(nameof(Songs));
            _lastModifiedDate = DateTime.Now;
        }
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

    public override string ToString()
    {
        return TrackNumber.ToString();
    }
}