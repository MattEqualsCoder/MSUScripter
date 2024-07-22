using System;
using System.Collections.ObjectModel;
using System.Linq;
using MSUScripter.Models;
using ReactiveUI;

namespace MSUScripter.ViewModels;

public class MsuTrackInfoViewModel : ViewModelBase
{
    public MsuTrackInfoViewModel()
    {
        Songs.CollectionChanged += (_, _) =>
        {
            this.RaisePropertyChanged(nameof(Songs));
            LastModifiedDate = DateTime.Now;
        };
    }
    
    public int TrackNumber { get; init; }

    public string TrackName { get; init; } = "";
    
    public DateTime LastModifiedDate { get; set; }
    
    [SkipConvert] public string? Description { get; set; }

    [SkipConvert] public bool HasDescription => !string.IsNullOrEmpty(Description); 

    [SkipConvert] public MsuProjectViewModel Project { get; set; } = new();
    
    [SkipConvert] public ObservableCollection<MsuSongInfoViewModel> Songs { get; init; } = [];

    [SkipConvert] public string Display => ToString();
    
    public bool HasChangesSince(DateTime time)
    {
        return Songs.Any(x => x.HasChangesSince(time)) || LastModifiedDate > time;
    }

    public override string ToString()
    {
        return $"Track #{TrackNumber} - {TrackName}";
    }

    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}