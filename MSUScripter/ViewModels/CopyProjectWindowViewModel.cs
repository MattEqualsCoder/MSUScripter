using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using MSUScripter.Configs;

namespace MSUScripter.ViewModels;

public class CopyProjectWindowViewModel : INotifyPropertyChanged
{
    private MsuProject? _originalProject;

    public MsuProject? OriginalProject
    {
        get => _originalProject;
        set => SetField(ref _originalProject, value);
    }
    
    private MsuProjectViewModel? _projectViewModel;

    public MsuProjectViewModel? ProjectViewModel
    {
        get => _projectViewModel;
        set => SetField(ref _projectViewModel, value);
    }
    
    private MsuProject? _newProject;

    public MsuProject? NewProject
    {
        get => _newProject;
        set => SetField(ref _newProject, value);
    }
        
    private List<CopyProjectViewModel> _paths = new();

    public List<CopyProjectViewModel> Paths
    {
        get => _paths;
        set => SetField(ref _paths, value);
    }
    
    private bool _isValid;

    public bool IsValid
    {
        get => _isValid;
        set => SetField(ref _isValid, value);
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

public class CopyProjectViewModel : INotifyPropertyChanged
{
    public CopyProjectViewModel(string? path)
    {
        PreviousPath = path ?? "";
        NewPath = path ?? "";
        if (!string.IsNullOrEmpty(PreviousPath))
        {
            var file = new FileInfo(PreviousPath);
            BaseFileName = file.Name;
            Extension = file.Extension;
        }
    }
    
    private string _previousPath = "";

    public string PreviousPath
    {
        get => _previousPath;
        set => SetField(ref _previousPath, value);
    }

    private string _newPath = "";

    public string NewPath
    {
        get => _newPath;
        set => SetField(ref _newPath, value);
    }
    
    private string _extension = "";

    public string Extension
    {
        get => _extension;
        set => SetField(ref _extension, value);
    }
    
    private string _baseFileName = "";

    public string BaseFileName
    {
        get => _baseFileName;
        set => SetField(ref _baseFileName, value);
    }

    private bool _isValid;

    public bool IsValid
    {
        get => _isValid;
        set => SetField(ref _isValid, value);
    }
    
    private string _message = "";

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