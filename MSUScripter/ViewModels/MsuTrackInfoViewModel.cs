using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using MSUScripter.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

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
    
    [SkipConvert, Reactive] public string? Description { get; set; }

    [SkipConvert] public bool HasDescription => !string.IsNullOrEmpty(Description); 

    [SkipConvert] public MsuProjectViewModel Project { get; set; } = new();
    
    [SkipConvert] public ObservableCollection<MsuSongInfoViewModel> Songs { get; init; } = [];

    [SkipConvert] public string Display => ToString();
    
    public bool IsScratchPad { get; set; }
    
    public bool HasChangesSince(DateTime time)
    {
        return Songs.Any(x => x.HasChangesSince(time)) || LastModifiedDate > time;
    }
    
    public void FixTrackSuffixes(bool? canPlaySongs = null)
    {
        var msu = new FileInfo(Project.MsuPath);

        canPlaySongs ??= Songs.Any(x => x.CanPlaySongs);
        
        for (var i = 0; i < Songs.Count; i++)
        {
            var songInfo = Songs[i];
            
            if (i == 0)
            {
                songInfo.OutputPath = msu.FullName.Replace(msu.Extension, $"-{TrackNumber}.pcm");
            }
            else
            {
                var altSuffix = i == 1 ? "alt" : $"alt{i}";
                songInfo.OutputPath =
                    msu.FullName.Replace(msu.Extension, $"-{TrackNumber}_{altSuffix}.pcm");
            }
            
            songInfo.ApplyCascadingSettings(Project, this, i > 0, canPlaySongs == true, true, true);
        }
    }

    public override string ToString()
    {
        return IsScratchPad ? "Scratch Pad" : $"Track #{TrackNumber} - {TrackName}";
    }

    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}