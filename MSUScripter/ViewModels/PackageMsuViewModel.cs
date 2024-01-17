using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSUScripter.ViewModels;

public class PackageMsuViewModel : INotifyPropertyChanged
{

    private string _buttonText = "Cancel";
    public string ButtonText
    {
        get => _buttonText;
        set => SetField(ref _buttonText, value);
    }

    private MsuProjectViewModel _project = new();

    public MsuProjectViewModel Project
    {
        get => _project;
        set => SetField(ref _project, value);
    }

    private string _response = "";

    public string Response
    {
        get => _response;
        set => SetField(ref _response, value);
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