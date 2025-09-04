using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using AvaloniaControls.Models;
using Material.Icons;
using MSUScripter.Configs;
using MSUScripter.Models;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

[SkipLastModified]
public class MsuProjectWindowViewModelTreeData : TranslatedViewModelBase
{
    public static IBrush HighlightColor { get; set; } = Brushes.SlateGray;

    [Reactive, SkipLastModified] public required MaterialIconKind CollapseIcon { get; set; }
    [Reactive, SkipLastModified] public double CollapseIconOpacity { get; set; } = 1;
    [Reactive, SkipLastModified] public MaterialIconKind CompletedIconKind { get; set; } = MaterialIconKind.FlagOutline;
    [Reactive, SkipLastModified] public IBrush CompletedIconColor { get; set; } = Brushes.DimGray;
    [Reactive, SkipLastModified] public MaterialIconKind HasSongIconKind { get; set; } = MaterialIconKind.VolumeSource;
    [Reactive, SkipLastModified] public IBrush HasSongIconColor { get; set; } = Brushes.DimGray;
    [Reactive, SkipLastModified] public MaterialIconKind CheckCopyrightIconKind { get; set; } = MaterialIconKind.Video;
    [Reactive, SkipLastModified] public IBrush CheckCopyrightIconColor { get; set; } = Brushes.DimGray;
    [Reactive, SkipLastModified] public MaterialIconKind CopyrightSafeIconKind { get; set; } = MaterialIconKind.Copyright;
    [Reactive, SkipLastModified] public IBrush CopyrightSafeIconColor { get; set; } = Brushes.DimGray;

    [Reactive] public required string Name { get; set; }
    public required int LeftSpacing { get; init; }
    public required bool ShowCheckbox { get; init; }
    [Reactive, SkipLastModified] public IBrush GridBackground { get; set; } = Brushes.Transparent;
    [Reactive, SkipLastModified] public IBrush BorderColor { get; set; } = Brushes.Transparent;

    [Reactive, ReactiveLinkedProperties(nameof(IsVisible)), SkipLastModified]
    public bool IsCollapsed { get; set; }

    [Reactive, ReactiveLinkedProperties(nameof(IsVisible)), SkipLastModified]
    public bool IsFilteredOut { get; set; }

    public bool IsVisible => !IsCollapsed && !IsFilteredOut;
    public int SortIndex { get; set; }
    public int ParentIndex { get; init; }

    public bool MsuDetails { get; set; }
    public bool Track { get; set; }
    public bool Song => SongInfo != null;
    public bool IsSongOrTrack => TrackInfo != null;

    [Reactive] public bool DisplayHasSongIcon { get; set; }
    [Reactive] public bool DisplayCheckCopyrightIcon { get; set; }
    [Reactive] public bool DisplayCopyrightSafeIcon { get; set; }
    [Reactive] public bool DisplayIsCompleteIcon { get; set; }
    [Reactive] public bool CanDelete { get; set; }

    [Reactive] public bool ShowAddButton { get; set; }
    [Reactive] public bool ShowMenuButton { get; set; }
    [Reactive] public bool IsComplete { get; set; }
    [Reactive] public bool IsInTestVideo { get; set; }
    [Reactive] public bool IsNotCopyrightTested { get; set; }
    [Reactive] public bool IsNotCopyrightSafe { get; set; }
    [Reactive] public bool IsCopyrightSafe { get; set; }

    public MsuTrackInfo? TrackInfo { get; set; }
    public MsuSongInfo? SongInfo { get; set; }
    public MsuProjectWindowViewModelTreeData? ParentTreeData { get; set; }
    public List<MsuProjectWindowViewModelTreeData> ChildTreeData { get; set; } = [];

    public override ViewModelBase DesignerExample()
    {
        throw new System.NotImplementedException();
    }

    public void ToggleAsParent(bool isParent, bool isCollapsed)
    {
        if (isParent)
        {
            CollapseIcon = isCollapsed ? MaterialIconKind.ChevronRight : MaterialIconKind.ChevronDown;
            CollapseIconOpacity = 1;

            foreach (var item in ChildTreeData)
            {
                item.IsCollapsed = isCollapsed;
            }
        }
        else
        {
            CollapseIcon = MaterialIconKind.MusicNote;
            CollapseIconOpacity = 0.4;
        }
    }

    public void UpdateCompletedFlag()
    {
        if (ChildTreeData.Count > 0)
        {
            var numComplete = 0;
            var numHasAudio = 0;
            var numCopyrightSafe = 0;
            var numCopyrightUnsafe = 0;
            var numCheckCopyright = 0;

            foreach (var data in ChildTreeData.Select(x => x.SongInfo))
            {
                if (data!.IsComplete)
                {
                    numComplete++;
                }

                if (data.HasAudioFiles())
                {
                    numHasAudio++;
                }

                if (data.IsCopyrightSafe == true)
                {
                    numCopyrightSafe++;
                }
                else if (data.IsCopyrightSafe == false)
                {
                    numCopyrightUnsafe++;
                }

                if (data.CheckCopyright == true)
                {
                    numCheckCopyright++;
                }
            }

            if (numComplete == 0)
            {
                CompletedIconColor = Brushes.IndianRed;
                CompletedIconKind = MaterialIconKind.FlagOutline;
            }
            else if (numComplete == ChildTreeData.Count)
            {
                CompletedIconColor = Brushes.LimeGreen;
                CompletedIconKind = MaterialIconKind.Flag;
            }
            else
            {
                CompletedIconColor = Brushes.Goldenrod;
                CompletedIconKind = MaterialIconKind.FlagOutline;
            }

            if (numHasAudio == 0)
            {
                HasSongIconColor = Brushes.IndianRed;
                HasSongIconKind = MaterialIconKind.VolumeMute;
            }
            else if (numHasAudio == ChildTreeData.Count)
            {
                HasSongIconColor = Brushes.LimeGreen;
                HasSongIconKind = MaterialIconKind.VolumeSource;
            }
            else
            {
                HasSongIconColor = Brushes.Goldenrod;
                HasSongIconKind = MaterialIconKind.VolumeMute;
            }

            if (numCopyrightSafe == ChildTreeData.Count)
            {
                CopyrightSafeIconColor = Brushes.LimeGreen;
                CopyrightSafeIconKind = MaterialIconKind.Copyright;
            }
            else if (numCopyrightUnsafe == ChildTreeData.Count)
            {
                CopyrightSafeIconColor = Brushes.IndianRed;
                CopyrightSafeIconKind = MaterialIconKind.CloseCircleOutline;
            }
            else if (numCopyrightUnsafe + numCopyrightSafe == ChildTreeData.Count)
            {
                CopyrightSafeIconColor = Brushes.Goldenrod;
                CopyrightSafeIconKind = MaterialIconKind.Copyright;
            }
            else
            {
                CopyrightSafeIconColor = Brushes.Goldenrod;
                CopyrightSafeIconKind = MaterialIconKind.QuestionMarkCircleOutline;
            }

            if (numCheckCopyright == 0)
            {
                CheckCopyrightIconColor = Brushes.IndianRed;
                CheckCopyrightIconKind = MaterialIconKind.VideoOutline;
            }
            else if (numCheckCopyright == ChildTreeData.Count)
            {
                CheckCopyrightIconColor = Brushes.LimeGreen;
                CheckCopyrightIconKind = MaterialIconKind.Video;
            }
            else
            {
                CheckCopyrightIconColor = Brushes.Goldenrod;
                CheckCopyrightIconKind = MaterialIconKind.VideoOutline;
            }

            IsComplete = false;
            IsInTestVideo = false;
            IsNotCopyrightTested = false;
            IsNotCopyrightSafe = false;
            IsCopyrightSafe = false;
        }
        else if (SongInfo == null)
        {
            CompletedIconColor = Brushes.DimGray;
            CompletedIconKind = MaterialIconKind.FlagOutline;
            HasSongIconColor = Brushes.DimGray;
            HasSongIconKind = MaterialIconKind.VolumeMute;
            CheckCopyrightIconColor = Brushes.DimGray;
            CheckCopyrightIconKind = MaterialIconKind.VideoOutline;
            CopyrightSafeIconColor = Brushes.DimGray;
            CopyrightSafeIconKind = MaterialIconKind.Copyright;

            IsComplete = false;
            IsInTestVideo = false;
            IsNotCopyrightTested = false;
            IsNotCopyrightSafe = false;
            IsCopyrightSafe = false;
        }
        else
        {
            if (SongInfo.IsComplete)
            {
                CompletedIconColor = Brushes.LimeGreen;
                CompletedIconKind = MaterialIconKind.Flag;
                IsComplete = true;
            }
            else
            {
                CompletedIconColor = Brushes.IndianRed;
                CompletedIconKind = MaterialIconKind.FlagOutline;
                IsComplete = false;
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
                IsInTestVideo = true;
            }
            else
            {
                CheckCopyrightIconColor = Brushes.IndianRed;
                CheckCopyrightIconKind = MaterialIconKind.VideoOutline;
                IsInTestVideo = false;
            }

            if (SongInfo.IsCopyrightSafe == true)
            {
                CopyrightSafeIconColor = Brushes.LimeGreen;
                CopyrightSafeIconKind = MaterialIconKind.Copyright;
                IsCopyrightSafe = false;
                IsNotCopyrightSafe = false;
                IsNotCopyrightTested = false;
            }
            else if (SongInfo.IsCopyrightSafe == false)
            {
                CopyrightSafeIconColor = Brushes.IndianRed;
                CopyrightSafeIconKind = MaterialIconKind.CloseCircleOutline;
                IsCopyrightSafe = false;
                IsNotCopyrightSafe = true;
                IsNotCopyrightTested = false;
            }
            else
            {
                CopyrightSafeIconColor = Brushes.Goldenrod;
                CopyrightSafeIconKind = MaterialIconKind.QuestionMarkCircleOutline;
                IsCopyrightSafe = false;
                IsNotCopyrightSafe = false;
                IsNotCopyrightTested = true;
            }
        }
    }

    public bool MatchesFilter(string? filterText, bool filterOnlyTracksMissingSongs, bool filterOnlyIncomplete, bool filterOnlyMissingAudio, bool filterOnlyCopyrightUntested)
    {
        var matches = true;

        if (matches && filterText != null)
        {
            matches = Name.Contains(filterText, System.StringComparison.CurrentCultureIgnoreCase);
        }
        
        if (matches && filterOnlyTracksMissingSongs)
        {
            matches = TrackInfo != null && SongInfo == null && ChildTreeData.Count == 0;
        }
        
        if (matches && filterOnlyIncomplete)
        {
            matches = SongInfo != null && !IsComplete;
        }
        
        if (matches && filterOnlyMissingAudio)
        {
            matches = SongInfo != null && !SongInfo.HasAudioFiles();
        }
        
        if (matches && filterOnlyCopyrightUntested)
        {
            matches = SongInfo != null && IsNotCopyrightTested;
        }

        return matches;
    }

}

[SkipLastModified]
public class MsuProjectWindowViewModel : TranslatedViewModelBase
{
    public ObservableCollection<MsuProjectWindowViewModelTreeData> TreeItems { get; set; } = [];
    
    [Reactive] public string SongSummary { get; set; } = "";
    [Reactive] public string TrackSummary { get; set; } = "";
    public string FilterText { get; set; } = "";
    public bool IsDraggingItem { get; set; }

    public bool IsViewingSongData => MsuSongViewModel.IsEnabled;
    [Reactive] public bool DisplayHasSongIcon { get; set; }
    [Reactive] public bool DisplayCheckCopyrightIcon { get; set; }
    [Reactive] public bool DisplayCopyrightSafeIcon { get; set; }
    [Reactive] public bool DisplayIsCompleteIcon { get; set; }
    [Reactive] public MsuProjectWindowViewModelTreeData? SelectedTreeItem { get; set; }
    [Reactive] public bool FilterOnlyTracksMissingSongs { get; set; }
    [Reactive] public bool FilterOnlyIncomplete { get; set; }
    [Reactive] public bool FilterOnlyMissingAudio { get; set; }
    [Reactive] public bool FilterOnlyCopyrightUntested { get; set; }
    [Reactive] public MaterialIconKind FilterEyeIcon { get; set; } = MaterialIconKind.Eye;
    [Reactive] public string StatusBarText { get; set; } = "Loaded Project";
    [Reactive] public string WindowTitle { get; set; } = "MSU Scripter";
    
    public MsuProjectWindowViewModelTreeData? CurrentTreeItem { get; set; }
    public MsuProject? MsuProject { get; set; }
    public MsuSongOuterPanelViewModel MsuSongViewModel { get; set; } = new();
    public MsuBasicInfoViewModel BasicInfoViewModel { get; set; } = new();
    [Reactive] public bool DisplayBasicPanel { get; set; } = true;
    [Reactive] public bool IsBusy { get; set; } = false;
    public List<RecentProject> RecentProjects { get; set; } = [];
    public DefaultSongPanel DefaultSongPanel { get; set; }
    
    public override ViewModelBase DesignerExample()
    {
        SongSummary = "2/5 Songs Complete";
        TrackSummary = "1/102 Tracks Complete";
        TreeItems = 
        [
            new MsuProjectWindowViewModelTreeData
            {
                CollapseIcon = MaterialIconKind.Note,
                Name = "MSU Details",
                LeftSpacing = 0,
                ShowCheckbox = false,
            },
            new MsuProjectWindowViewModelTreeData
            {
                CollapseIcon = MaterialIconKind.ChevronRight,
                Name = "Scratch Pad",
                LeftSpacing = 0,
                ShowCheckbox = true,
                TrackInfo = new MsuTrackInfo(),
            },
            new MsuProjectWindowViewModelTreeData()
            {
                CollapseIcon = MaterialIconKind.ChevronDown,
                Name = "#1 With a Really Long Name",
                LeftSpacing = 0,
                ShowCheckbox = true,
                
                TrackInfo = new MsuTrackInfo(),
            },
            new MsuProjectWindowViewModelTreeData()
            {
                CollapseIcon = MaterialIconKind.MusicNote,
                Name = "Song 1",
                LeftSpacing = 12,
                ShowCheckbox = true,
                TrackInfo = new MsuTrackInfo(),
            },
        ];

        for (var i = 2; i < 102; i++)
        {
            TreeItems.Add(new MsuProjectWindowViewModelTreeData
            {
                CollapseIcon = MaterialIconKind.ChevronDown,
                Name = $"#{i} Eastern Palace",
                LeftSpacing = 0,
                ShowCheckbox = true,
                TrackInfo = new MsuTrackInfo(),
            });
        }

        return this;
    }
}