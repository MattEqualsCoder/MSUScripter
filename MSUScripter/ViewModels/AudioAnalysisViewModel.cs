﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSUScripter.ViewModels;

public class AudioAnalysisViewModel : INotifyPropertyChanged
{
    private List<AudioAnalysisSongViewModel> _rows { get; set; } = new List<AudioAnalysisSongViewModel>();
    public List<AudioAnalysisSongViewModel> Rows
    {
        get => _rows;
        set
        {
            _rows = value;
            OnPropertyChanged();
        }
    }
    
    private int _totalSongs;

    public int TotalSongs
    {
        get => _totalSongs;
        set => SetField(ref _totalSongs, value);
    }
    
    private int _songsCompleted;

    public int SongsCompleted
    {
        get => _songsCompleted;
        set => SetField(ref _songsCompleted, value);
    }
    
    private string _bottomBar = "";

    public string BottomBar
    {
        get => _bottomBar;
        set => SetField(ref _bottomBar, value);
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