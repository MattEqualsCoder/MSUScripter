﻿using System.Collections.Generic;
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

    public double _volume;
    
    public double Volume 
    {
        
        get => _volume;
        set => SetField(ref _volume, value);
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