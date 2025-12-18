using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using AvaloniaControls.Models;
using Material.Icons;
using MSUScripter.Configs;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class TrackOverviewPanelViewModel : ViewModelBase
{
    [Reactive] public List<TrackOverviewRow> Rows { get; set; } = [];
    [Reactive] public bool IsVisible { get; set; }
    [Reactive] public int SelectedIndex { get; set; }
    [Reactive] public bool ShowCompleteColumn { get; set; } = true;
    [Reactive] public bool ShowCopyrightSafeColumn { get; set; } = false;
    [Reactive] public bool ShowCheckCopyrightColumn { get; set; } = false;
    [Reactive] public bool ShowHasAudioColumn { get; set; } = false;
    public Settings Settings { get; private set; }

    public void UpdateModel(MsuProject project, Settings settings)
    {
        List<TrackOverviewRow> newRows = [];

        foreach (var track in project.Tracks.Where(x => !x.IsScratchPad))
        {
            if (track.Songs.Count != 0)
            {
                newRows.AddRange(track.Songs.Select(x => new TrackOverviewRow(track, x)));
            }
            else
            {
                newRows.Add(new TrackOverviewRow(track));
            }
        }

        Settings = settings;
        ShowCompleteColumn = settings.TrackOverviewShowIsCompleteIcon;
        ShowCopyrightSafeColumn = settings.TrackOverviewShowCopyrightSafeIcon;
        ShowCheckCopyrightColumn = settings.TrackOverviewShowCheckCopyrightIcon;
        ShowHasAudioColumn = settings.TrackOverviewShowHasSongIcon;

        SelectedIndex = 0;
        Rows = newRows;
    }

    public class TrackOverviewRow : ViewModelBase
    {
        public TrackOverviewRow(MsuTrackInfo track, MsuSongInfo? song = null)
        {
            Track = track;
            SongInfo = song;
            UpdateIcons();
        }

        public MsuTrackInfo Track { get; }
        public int TrackNumber => Track.TrackNumber;
        public string TrackName => Track.TrackName;
        public MsuSongInfo? SongInfo { get; }
        public bool HasSong => SongInfo != null;
        public string Name => SongInfo?.SongName ?? "";
        public string Artist => SongInfo?.Artist ?? "";
        public string Album => SongInfo?.Album ?? "";


        [Reactive] public MaterialIconKind CompletedIconKind { get; set; } = MaterialIconKind.FlagOutline;
        [Reactive] public IBrush CompletedIconColor { get; set; } = Brushes.DimGray;
        [Reactive] public MaterialIconKind HasSongIconKind { get; set; } = MaterialIconKind.VolumeSource;
        [Reactive] public IBrush HasSongIconColor { get; set; } = Brushes.DimGray;
        [Reactive] public MaterialIconKind CheckCopyrightIconKind { get; set; } = MaterialIconKind.Video;
        [Reactive] public IBrush CheckCopyrightIconColor { get; set; } = Brushes.DimGray;
        [Reactive] public MaterialIconKind CopyrightSafeIconKind { get; set; } = MaterialIconKind.Copyright;
        [Reactive] public IBrush CopyrightSafeIconColor { get; set; } = Brushes.DimGray;

        public string File =>
            SongInfo == null
                ? ""
                : !SongInfo.MsuPcmInfo.HasFiles()
                    ? ""
                    : SongInfo.MsuPcmInfo.GetFiles().Count == 1
                        ? SongInfo.MsuPcmInfo.GetFiles().First()
                        : $"{SongInfo.MsuPcmInfo.GetFiles().Count} files";

        public void UpdateIcons()
        {
            if (SongInfo == null)
            {
                return;
            }
            
            if (SongInfo.IsComplete)
            {
                CompletedIconColor = Brushes.LimeGreen;
                CompletedIconKind = MaterialIconKind.Flag;
            }
            else
            {
                CompletedIconColor = Brushes.IndianRed;
                CompletedIconKind = MaterialIconKind.FlagOutline;
            }

            if (SongInfo.HasAudioFiles())
            {
                HasSongIconColor = Brushes.LimeGreen;
                HasSongIconKind = MaterialIconKind.VolumeSource;
            }
            else
            {
                HasSongIconColor = Brushes.IndianRed;
                HasSongIconKind = MaterialIconKind.VolumeMute;
            }

            if (SongInfo.CheckCopyright == true)
            {
                CheckCopyrightIconColor = Brushes.LimeGreen;
                CheckCopyrightIconKind = MaterialIconKind.Video;
            }
            else
            {
                CheckCopyrightIconColor = Brushes.IndianRed;
                CheckCopyrightIconKind = MaterialIconKind.VideoOutline;
            }

            if (SongInfo.IsCopyrightSafe == true)
            {
                CopyrightSafeIconColor = Brushes.LimeGreen;
                CopyrightSafeIconKind = MaterialIconKind.Copyright;
            }
            else if (SongInfo.IsCopyrightSafe == false)
            {
                CopyrightSafeIconColor = Brushes.IndianRed;
                CopyrightSafeIconKind = MaterialIconKind.CloseCircleOutline;
            }
            else
            {
                CopyrightSafeIconColor = Brushes.Goldenrod;
                CopyrightSafeIconKind = MaterialIconKind.QuestionMarkCircleOutline;
            }
        }

        public override ViewModelBase DesignerExample()
        {
            return this;
        }
    }
    
    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}