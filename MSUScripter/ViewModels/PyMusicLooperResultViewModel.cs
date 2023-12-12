using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSUScripter.ViewModels;

public class PyMusicLooperResultViewModel : INotifyPropertyChanged
{
    public PyMusicLooperResultViewModel(int loopStart, int loopEnd, decimal score)
    {
        _loopStart = loopStart;
        _loopEnd = loopEnd;
        _score = Math.Round(score * 100, 2);
    }
    
    private int _loopStart;
    public int LoopStart
    {
        get => _loopStart;
        set => SetField(ref _loopStart, value);
    }
    
    
    private int _loopEnd;
    public int LoopEnd
    {
        get => _loopEnd;
        set => SetField(ref _loopEnd, value);
    }
    
    private decimal _score;
    public decimal Score
    {
        get => _score;
        set => SetField(ref _score, value);
    }

    private string _tempPath = "";
    public string TempPath
    {
        get => _tempPath;
        set
        {
            SetField(ref _tempPath, value);
            OnPropertyChanged(nameof(CanTestFile));
        }
    }
    
    private string _status = "";
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    private string _duration = "";

    public string Duration
    {
        get => _duration;
        set => SetField(ref _duration, value);
    }
    
    private bool _generated;

    public bool Generated
    {
        get => _generated;
        set => SetField(ref _generated, value);
    }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public bool CanTestFile => !string.IsNullOrEmpty(_tempPath);
    
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