using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSUScripter.ViewModels;

public class TrackOverviewViewModel : INotifyPropertyChanged
{
    public List<TrackOverviewRow> Rows { get; set; } = new();

    private string _completedSongDetails = "";
    public string CompletedSongDetails
    {
        
        get => _completedSongDetails;
        set => SetField(ref _completedSongDetails, value);
    }

    private string _completedTrackDetails = "";
    public string CompletedTrackDetails
    {
        
        get => _completedTrackDetails;
        set => SetField(ref _completedTrackDetails, value);
    }

    public int SelectedIndex { get; set; } = 0;
    
    public class TrackOverviewRow
    {
        public bool HasSong { get; set; }
        public MsuSongInfoViewModel? SongInfo { get; set; }
        public required int TrackNumber { get; set; }
        public required string TrackName { get; set; }
        public string Name { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Album { get; set; } = "";
        public string File { get; set; } = "";
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