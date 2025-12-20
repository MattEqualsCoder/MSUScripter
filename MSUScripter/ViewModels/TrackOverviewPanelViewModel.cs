using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Material.Icons;
using MSUScripter.Configs;
using ReactiveUI.SourceGenerators;

namespace MSUScripter.ViewModels;

public partial class TrackOverviewPanelViewModel : ViewModelBase
{
    [Reactive] public partial List<TrackOverviewRow> Rows { get; set; }
    [Reactive] public partial bool IsVisible { get; set; }
    [Reactive] public partial int SelectedIndex { get; set; }
    [Reactive] public partial bool ShowCompleteColumn { get; set; }
    [Reactive] public partial bool ShowCopyrightSafeColumn { get; set; }
    [Reactive] public partial bool ShowCheckCopyrightColumn { get; set; }
    [Reactive] public partial bool ShowHasAudioColumn { get; set; }
    public Settings Settings { get; private set; } = new();

    public TrackOverviewPanelViewModel()
    {
        ShowCompleteColumn = true;
        Rows = [];
    }
    
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

    public partial class TrackOverviewRow : ViewModelBase
    {
        public TrackOverviewRow(MsuTrackInfo track, MsuSongInfo? song = null)
        {
            CompletedIconColor = Brushes.DimGray;
            HasSongIconColor = Brushes.DimGray;
            CheckCopyrightIconColor = Brushes.DimGray;
            CopyrightSafeIconColor = Brushes.DimGray;
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
        
        [Reactive] public partial MaterialIconKind CompletedIconKind { get; set; }
        [Reactive] public partial IBrush CompletedIconColor { get; set; }
        [Reactive] public partial MaterialIconKind HasSongIconKind { get; set; }
        [Reactive] public partial IBrush HasSongIconColor { get; set; }
        [Reactive] public partial MaterialIconKind CheckCopyrightIconKind { get; set; }
        [Reactive] public partial IBrush CheckCopyrightIconColor { get; set; }
        [Reactive] public partial MaterialIconKind CopyrightSafeIconKind { get; set; }
        [Reactive] public partial IBrush CopyrightSafeIconColor { get; set; }
        
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