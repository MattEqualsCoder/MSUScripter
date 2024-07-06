using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using MSUScripter.Configs;
using MSUScripter.Models;

namespace MSUScripter.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private string? _msuPcmPath;

    public string? MsuPcmPath
    {
        get => _msuPcmPath;
        set => SetField(ref _msuPcmPath, value);
    }

    private ICollection<RecentProject> _recentProjects = new List<RecentProject>();

    public ICollection<RecentProject> RecentProjects
    {
        
        get => _recentProjects;
        set => SetField(ref _recentProjects, value);
    }

    private double _volume;
    
    public double Volume 
    {
        
        get => _volume;
        set => SetField(ref _volume, value);
    }
    
    private decimal _uiScaling;
    
    public decimal UiScaling 
    {
        
        get => _uiScaling;
        set => SetField(ref _uiScaling, value);
    }

    private int _loopDuration = 5;
    public int LoopDuration
    {
        get => _loopDuration;
        set => SetField(ref _loopDuration, value);
    }

    private bool _promptOnUpdate;
    
    public bool PromptOnUpdate
    {
        
        get => _promptOnUpdate;
        set => SetField(ref _promptOnUpdate, value);
    }
    
    private bool _promptOnPreRelease;
    
    public bool PromptOnPreRelease
    {
        
        get => _promptOnPreRelease;
        set => SetField(ref _promptOnPreRelease, value);
    }
    
    private bool _darkTheme;
    
    public bool DarkTheme
    {
        
        get => _darkTheme;
        set => SetField(ref _darkTheme, value);
    }

    private string? _previousPath;
    public string? PreviousPath
    {
        
        get => _previousPath;
        set => SetField(ref _previousPath, value);
    }
    
    private bool _automaticallyRunPyMusicLooper;
    public bool AutomaticallyRunPyMusicLooper
    {
        
        get => _automaticallyRunPyMusicLooper;
        set => SetField(ref _automaticallyRunPyMusicLooper, value);
    }
    
    public bool HideSubTracksSubChannelsWarning { get; set; }

    private WindowRestoreDetails? _mainWindowRestoreDetails;

    public WindowRestoreDetails? MainWindowRestoreDetails
    {
        get => _mainWindowRestoreDetails;
        set => SetField(ref _mainWindowRestoreDetails, value);
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