using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSUScripter.ViewModels;

public class SelectTrackWindowViewModel : INotifyPropertyChanged
{
    public MsuProjectViewModel? Project { get; set; }
    
    public MsuTrackInfoViewModel? PreviousTrack { get; set; }
    
    public MsuSongInfoViewModel? PreviousSong { get; set; }
    public bool IsMove { get; set; }

    private List<string> _trackNames = new();
    public List<string> TrackNames
    {
        get => _trackNames;
        set => SetField(ref _trackNames, value);
    }

    public List<MsuTrackInfoViewModel> Tracks { get; set; } = new();

    private int _selectedIndex;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetField(ref _selectedIndex, value);
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