using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MSUScripter.Configs;

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

    public string? _previousPath;
    public string? PreviousPath
    {
        
        get => _previousPath;
        set => SetField(ref _previousPath, value);
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