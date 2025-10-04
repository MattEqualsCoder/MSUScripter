using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaControls.Controls;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Services;
using DynamicData;
using Material.Icons;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.Events;
using MSUScripter.Models;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

// ReSharper disable once ClassNeverInstantiated.Global
public class MsuProjectWindowService(
    ConverterService converterService,
    YamlService yamlService,
    StatusBarService statusBarService,
    ProjectService projectService,
    TrackListService trackListService,
    SettingsService settingsService,
    IAudioPlayerService audioPlayerService,
    MsuPcmService msuPcmService,
    PythonCompanionService pythonCompanionService,
    ILogger<MsuProjectWindowService> logger) : ControlService
{
    private Settings Settings => settingsService.Settings;
    
    private MsuProjectWindowViewModel _viewModel = null!;
    private MsuProject _project = null!;
    private MsuProjectWindowViewModelTreeData? _draggedItem;
    private MsuProjectWindowViewModelTreeData? _hoveredItem;

    public MsuProjectWindowViewModel InitViewModel(MsuProject project)
    {
        _project = project;

        var windowTitle = "MSU Scripter";
        if (!string.IsNullOrEmpty(project.BasicInfo.PackName))
        {
            windowTitle = $"{project.BasicInfo.PackName} - MSU Scripter";
        }
        else if(!string.IsNullOrEmpty(project.MsuPath))
        {
            var baseName = Path.GetFileName(project.MsuPath);
            windowTitle = $"{baseName} - MSU Scripter";
        }

        var sidebarItems = new List<MsuProjectWindowViewModelTreeData>
        {
            new()
            {
                Name = "MSU Details",
                CollapseIcon = MaterialIconKind.Note,
                LeftSpacing = 0,
                ShowCheckbox = false,
                SortIndex = -10000,
                MsuDetails = true
            }
        };

        var totalTracks = 0;
        var completedTracks = 0;
        var totalSongs = 0;
        var completedSongs = 0;

        foreach (var track in _project.Tracks)
        {
            var trackSortIndex = track.IsScratchPad ? -1000 : track.TrackNumber * 1000;

            if (!track.IsScratchPad)
            {
                totalTracks++;
            }

            var trackTreeData = new MsuProjectWindowViewModelTreeData
            {
                Name = track.IsScratchPad ? "Scratch Pad" : $"#{track.TrackNumber} {track.TrackName}",
                CollapseIcon = MaterialIconKind.MusicNote,
                CollapseIconOpacity = 0.4,
                TrackInfo = track,
                LeftSpacing = 0,
                SortIndex = trackSortIndex,
                ShowCheckbox = true,
                Track = true,
            };
            sidebarItems.Add(trackTreeData);

            if (track is { IsScratchPad: false, Songs.Count: > 0 })
            {
                completedTracks++;
            }

            for (var i = 0; i < track.Songs.Count; i++)
            {
                var song = track.Songs[i];

                totalSongs++;

                if (song.IsComplete)
                {
                    completedSongs++;
                }

                if (track.Songs.Count > 1)
                {
                    var songTreeData = new MsuProjectWindowViewModelTreeData
                    {
                        Name = string.IsNullOrEmpty(song.SongName) ? $"Song {i + 1}" : song.SongName,
                        CollapseIcon = MaterialIconKind.MusicNote,
                        CollapseIconOpacity = 0.4,
                        LeftSpacing = 12,
                        SortIndex = trackSortIndex + 1 + i,
                        ParentIndex = trackSortIndex,
                        ParentTreeData = trackTreeData,
                        ShowCheckbox = true,
                        TrackInfo = track,
                        SongInfo = song,
                    };
                    songTreeData.UpdateCompletedFlag();
                    trackTreeData.CollapseIcon = MaterialIconKind.ChevronDown;
                    trackTreeData.CollapseIconOpacity = 1;
                    trackTreeData.ChildTreeData.Add(songTreeData);
                    sidebarItems.Add(songTreeData);
                }
                else
                {
                    trackTreeData.SongInfo = song;
                    trackTreeData.CollapseIcon = MaterialIconKind.MusicNote;
                    trackTreeData.CollapseIconOpacity = 0.4;
                }
            }

            trackTreeData.UpdateCompletedFlag();
        }

        _viewModel = new MsuProjectWindowViewModel()
        {
            MsuProject = project,
            SongSummary = $"{completedSongs}/{totalSongs} Songs Completed",
            TrackSummary = $"{completedTracks}/{totalTracks} Tracks With Songs Added",
            WindowTitle = windowTitle,
            PreviousVideoPath = settingsService.Settings.PreviousVideoPath
        };
        
        _viewModel.BasicInfoViewModel.UpdateModel(project);

        _viewModel.TreeItems.AddRange(sidebarItems.OrderBy(x => x.SortIndex));

        if (Settings.ProjectTreeDisplayIsCompleteIcon)
        {
            ToggleCompletedIcons(true);
        }
        
        if (Settings.ProjectTreeDisplayCheckCopyrightIcon)
        {
            ToggleCheckCopyrightIcons(true);
        }
        
        if (Settings.ProjectTreeDisplayCopyrightSafeIcon)
        {
            ToggleCopyrightStatusIcons(true);
        }
        
        if (Settings.ProjectTreeDisplayHasSongIcon)
        {
            ToggleHasAudioIcons(true);
        }

        _viewModel.FilterOnlyTracksMissingSongs = Settings.ProjectTreeFilterOnlyTracksMissingSongs;
        _viewModel.FilterOnlyCopyrightUntested = Settings.ProjectTreeFilterOnlyCopyrightUntested;
        _viewModel.FilterOnlyIncomplete = Settings.ProjectTreeFilterOnlyIncomplete;
        _viewModel.FilterOnlyMissingAudio = Settings.ProjectTreeFilterOnlyMissingAudio;
        FilterTree();

        statusBarService.StatusBarTextUpdated += StatusBarServiceOnStatusBarTextUpdated;
        msuPcmService.GeneratingPcm += MsuPcmServiceOnGeneratingPcm;

        _viewModel.RecentProjects = settingsService.Settings.RecentProjects.Where(x => x.ProjectPath != project.ProjectFilePath)
            .ToList();
        
        LoadSettings();

        _viewModel.LastModifiedDate = project.LastSaveTime;

        _viewModel.BasicInfoViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == "PackName")
            {
                if (!string.IsNullOrEmpty(_viewModel.BasicInfoViewModel.PackName))
                {
                    _viewModel.WindowTitle = $"{_viewModel.BasicInfoViewModel.PackName} - MSU Scripter";
                }
                else if(!string.IsNullOrEmpty(project.MsuPath))
                {
                    var baseName = Path.GetFileName(project.MsuPath);
                    _viewModel.WindowTitle = $"{baseName} - MSU Scripter";
                }
            }
        };

        return _viewModel;
    }

    private void MsuPcmServiceOnGeneratingPcm(object? sender, bool e)
    {
        _viewModel.MsuSongViewModel.IsGeneratingPcmFiles = e;
        _viewModel.MsuSongViewModel.AdvancedPanelViewModel.IsGeneratingPcmFile = e;
    }

    private void StatusBarServiceOnStatusBarTextUpdated(object? sender, ValueEventArgs<string> e)
    {
        _viewModel.StatusBarText = e.Data;
    }

    public void LoadSettings()
    {
        _viewModel.DefaultSongPanel = settingsService.Settings.DefaultSongPanel;
        _viewModel.MsuSongViewModel.BasicPanelViewModel.PyMusicLooperEnabled = pythonCompanionService.IsValid;
    }

    public void UpdateCompletedSummary()
    {
        var totalTracks = 0;
        var completedTracks = 0;
        var totalSongs = 0;
        var completedSongs = 0;
        
        foreach (var track in _project.Tracks)
        {
            if (!track.IsScratchPad)
            {
                totalTracks++;
                if (track.Songs.Count > 0) completedTracks++;
            }
            
            totalSongs += track.Songs.Count;
            completedSongs += track.Songs.Count(x => x.IsComplete);
        }

        _viewModel.SongSummary = $"{completedSongs}/{totalSongs} Songs Completed";
        _viewModel.TrackSummary = $"{completedTracks}/{totalTracks} Tracks With Songs Added";
    }

    public void ToggleFilterTracksMissingSongs()
    {
        _viewModel.FilterOnlyTracksMissingSongs = !_viewModel.FilterOnlyTracksMissingSongs;
        FilterTree();
        settingsService.Settings.ProjectTreeFilterOnlyTracksMissingSongs = _viewModel.FilterOnlyTracksMissingSongs;
        settingsService.TrySaveSettings();
    }
    
    public void ToggleFilterIncomplete()
    {
        _viewModel.FilterOnlyIncomplete = !_viewModel.FilterOnlyIncomplete;
        FilterTree();
        settingsService.Settings.ProjectTreeFilterOnlyIncomplete = _viewModel.FilterOnlyIncomplete;
        settingsService.TrySaveSettings();
    }

    public void ToggleFilterMissingAudio()
    {
        _viewModel.FilterOnlyMissingAudio = !_viewModel.FilterOnlyMissingAudio;
        FilterTree();
        settingsService.Settings.ProjectTreeFilterOnlyMissingAudio = _viewModel.FilterOnlyMissingAudio;
        settingsService.TrySaveSettings();
    }

    public void ToggleFilterCopyrightUntested()
    {
        _viewModel.FilterOnlyCopyrightUntested = !_viewModel.FilterOnlyCopyrightUntested;
        FilterTree();
        settingsService.Settings.ProjectTreeFilterOnlyCopyrightUntested = _viewModel.FilterOnlyCopyrightUntested;
        settingsService.TrySaveSettings();
    }

    public void FilterTree()
    {
        var hasFilterToggle = _viewModel.FilterOnlyTracksMissingSongs || _viewModel.FilterOnlyIncomplete ||
                              _viewModel.FilterOnlyMissingAudio || _viewModel.FilterOnlyCopyrightUntested;
        _viewModel.FilterEyeIcon = hasFilterToggle ? MaterialIconKind.EyeCheck : MaterialIconKind.Eye;
        List<MsuProjectWindowViewModelTreeData> parentTreeItems = [];
        var filterText = string.IsNullOrEmpty(_viewModel.FilterText) ? null : _viewModel.FilterText.ToLower();
        foreach (var treeData in _viewModel.TreeItems)
        {
            if (treeData.ChildTreeData.Count > 0)
            {
                parentTreeItems.Add(treeData);
            }
            else
            {
                var matches = treeData.MatchesFilter(filterText, _viewModel.FilterOnlyTracksMissingSongs, _viewModel.FilterOnlyIncomplete,
                    _viewModel.FilterOnlyMissingAudio, _viewModel.FilterOnlyCopyrightUntested);
                treeData.IsFilteredOut = !matches;
            }
        }

        foreach (var treeData in parentTreeItems)
        {
            if (treeData.MatchesFilter(filterText, _viewModel.FilterOnlyTracksMissingSongs, _viewModel.FilterOnlyIncomplete, _viewModel.FilterOnlyMissingAudio, _viewModel.FilterOnlyCopyrightUntested))
            {
                treeData.IsFilteredOut = false;
                foreach (var childData in treeData.ChildTreeData)
                {
                    childData.IsFilteredOut = false;
                }
            }
            else
            {
                treeData.IsFilteredOut = treeData.ChildTreeData.All(x => x.IsFilteredOut);
            }
        }
    }
    
    public void SelectedTreeItem(MsuProjectWindowViewModelTreeData treeData, bool isIconClick)
    {
        if (treeData.ChildTreeData.Count > 0)
        {
            if (isIconClick)
            {
                treeData.ToggleAsParent(true, treeData.CollapseIcon == MaterialIconKind.ChevronDown);
            }
            else
            {
                SaveCurrentPanel();
                _viewModel.CurrentTreeItem = treeData;
                _viewModel.BasicInfoViewModel.IsVisible = false;
                _viewModel.MsuSongViewModel.BasicPanelViewModel.PyMusicLooperEnabled = pythonCompanionService.IsValid;
                _viewModel.MsuSongViewModel.UpdateViewModel(_project, treeData.TrackInfo!, null, treeData);
            }
        }
        else if (treeData.IsSongOrTrack)
        {
            SaveCurrentPanel();
            _viewModel.CurrentTreeItem = treeData;
            _viewModel.BasicInfoViewModel.IsVisible = false;
            _viewModel.MsuSongViewModel.BasicPanelViewModel.PyMusicLooperEnabled = pythonCompanionService.IsValid;
            _viewModel.MsuSongViewModel.UpdateViewModel(_project, treeData.TrackInfo!, treeData.SongInfo, treeData);
        }
        else if (treeData.MsuDetails)
        {
            if (_viewModel.MsuSongViewModel.IsEnabled)
            {
                _viewModel.MsuSongViewModel.SaveChanges();
                _viewModel.MsuSongViewModel.IsEnabled = false;
            }
            _viewModel.CurrentTreeItem = treeData;
            _viewModel.BasicInfoViewModel.UpdateModel(_project);
            _viewModel.BasicInfoViewModel.IsVisible = true;
        }
    }

    public bool SaveCurrentPanel()
    {
        if (_viewModel.MsuSongViewModel.IsEnabled)
        {
            _viewModel.MsuSongViewModel.SaveChanges();
            if (_viewModel.MsuSongViewModel.LastModifiedDate > _viewModel.LastModifiedDate)
            {
                _viewModel.LastModifiedDate = _viewModel.MsuSongViewModel.LastModifiedDate;
                return true;
            }
        }
        else if (_viewModel.BasicInfoViewModel.IsVisible)
        {
            _viewModel.BasicInfoViewModel.SaveChanges();
            if (_viewModel.BasicInfoViewModel.LastModifiedDate > _viewModel.LastModifiedDate)
            {
                _viewModel.LastModifiedDate = _viewModel.BasicInfoViewModel.LastModifiedDate;
                return true;
            }
        }

        return false;
    }

    public void SaveProject(bool isBackup = false)
    {
        SaveCurrentPanel();
        projectService.SaveMsuProject(_project, isBackup);
    }
    
    public bool CreateYamlFile(out string? error)
    {
        SaveCurrentPanel();
        projectService.ExportMsuRandomizerYaml(_project, out error);
        if (!string.IsNullOrEmpty(error))
        {
            return false;
        }

        return _project.BasicInfo is not { IsSmz3Project: true, CreateSplitSmz3Script: true } ||
               projectService.CreateSmz3SplitRandomizerYaml(_project, false, false, out error);
    }
    
    public void CreateTrackList()
    {
        SaveCurrentPanel();
        trackListService.WriteTrackListFile(_project);
    }
    
    public bool CreateScriptFiles()
    {
        SaveCurrentPanel();
        if (!projectService.CreateAltSwapperFile(_project))
        {
            return false;
        }

        if (_project.BasicInfo is { IsSmz3Project: true, CreateSplitSmz3Script: true })
        {
            return projectService.CreateSmz3SplitScript(_project);
        }

        return true;
    }

    public void AddNewSong(MsuProjectWindowViewModelTreeData? treeData = null, bool duplicate = false, bool advancedMode = false, bool rememberSetting = false, string? initialFile = null)
    {
        if (rememberSetting)
        {
            settingsService.Settings.DefaultSongPanel = advancedMode ? DefaultSongPanel.Advanced : DefaultSongPanel.Basic;
            settingsService.SaveSettings();
            _viewModel.DefaultSongPanel = settingsService.Settings.DefaultSongPanel;
        }
        
        treeData ??= _viewModel.CurrentTreeItem;
        if (treeData?.TrackInfo == null || _viewModel.MsuProject == null)
        {
            throw new InvalidOperationException("Invalid tree data item for adding a song");
        }

        var trackInfo = treeData.TrackInfo;
        var index = 0;
        if (treeData.SongInfo != null)
        {
            index = trackInfo.Songs.IndexOf(treeData.SongInfo) + 1;
        }
        var newSong = trackInfo.AddSong(_viewModel.MsuProject, index, advancedMode);

        if (duplicate && treeData.SongInfo != null)
        {
            var outputPath = newSong.OutputPath ?? newSong.MsuPcmInfo.Output;
            if (converterService.CloneModel(treeData.SongInfo, newSong) &&
                converterService.CloneModel(treeData.SongInfo.MsuPcmInfo, newSong.MsuPcmInfo))
            {
                newSong.Id = Guid.NewGuid().ToString("N");
                newSong.OutputPath = outputPath;
                newSong.MsuPcmInfo.Output = outputPath;
            }
        }
        
        var parentSortIndex = treeData.ParentIndex != 0 ? treeData.ParentIndex : treeData.SortIndex;
        var parentTreeData = _viewModel.TreeItems.First(x => x.SortIndex == parentSortIndex);
        var parentIndex = _viewModel.TreeItems.IndexOf(parentTreeData);

        // Added the first song, so just use the same tree data
        if (trackInfo.Songs.Count == 1)
        {
            treeData.SongInfo = newSong;
        }
        // Added a song to a node that already has multiple songs
        else if (parentTreeData.ChildTreeData.Count > 0)
        {
            parentTreeData.SongInfo = null;

            var songTreeData = new MsuProjectWindowViewModelTreeData
            {
                Name = string.IsNullOrEmpty(newSong.SongName) ? $"Song {trackInfo.Songs.Count}" : newSong.SongName,
                CollapseIcon = MaterialIconKind.MusicNote,
                CollapseIconOpacity = 0.4,
                LeftSpacing = 12,
                SortIndex = parentSortIndex + 1 + index,
                ParentIndex = parentSortIndex,
                ParentTreeData = parentTreeData,
                ShowCheckbox = true,
                TrackInfo = trackInfo,
                SongInfo = newSong,
            };
            
            parentTreeData.ToggleAsParent(true, false);
            parentTreeData.ChildTreeData.Insert(index, songTreeData);
            _viewModel.TreeItems.Insert(parentIndex + index + 1, songTreeData);
            
            for (var i = index + 1; i < treeData.TrackInfo.Songs.Count; i++)
            {
                parentTreeData.ChildTreeData[i].SortIndex++;
            }

            songTreeData.UpdateCompletedFlag();
            songTreeData.DisplayIsCompleteIcon = _viewModel.DisplayIsCompleteIcon;
            songTreeData.DisplayHasSongIcon = _viewModel.DisplayHasSongIcon;
            songTreeData.DisplayCopyrightSafeIcon = _viewModel.DisplayCopyrightSafeIcon;
            songTreeData.DisplayCheckCopyrightIcon = _viewModel.DisplayCheckCopyrightIcon;
            parentTreeData.UpdateCompletedFlag();
            treeData = songTreeData;
        }
        // Add a second song, meaning we need to switch the track to be a collapsible 
        else
        {
            parentTreeData.SongInfo = null;

            for (var i = 0; i < trackInfo.Songs.Count; i++)
            {
                var song = trackInfo.Songs[i];
                
                var songTreeData = new MsuProjectWindowViewModelTreeData
                {
                    Name = string.IsNullOrEmpty(song.SongName) ? $"Song {i + 1}" : song.SongName,
                    CollapseIcon = MaterialIconKind.MusicNote,
                    CollapseIconOpacity = 0.4,
                    LeftSpacing = 12,
                    SortIndex = parentSortIndex + 1 + i,
                    ParentIndex = parentSortIndex,
                    ParentTreeData = parentTreeData,
                    ShowCheckbox = true,
                    TrackInfo = trackInfo,
                    SongInfo = song,
                };
                
                songTreeData.UpdateCompletedFlag();
                songTreeData.DisplayIsCompleteIcon = _viewModel.DisplayIsCompleteIcon;
                songTreeData.DisplayHasSongIcon = _viewModel.DisplayHasSongIcon;
                songTreeData.DisplayCopyrightSafeIcon = _viewModel.DisplayCopyrightSafeIcon;
                songTreeData.DisplayCheckCopyrightIcon = _viewModel.DisplayCheckCopyrightIcon;
                treeData = songTreeData;
                
                parentTreeData.ToggleAsParent(true, false);
                parentTreeData.ChildTreeData.Add(songTreeData);
                _viewModel.TreeItems.Insert(parentIndex + i + 1, songTreeData);
            }
            
            parentTreeData.UpdateCompletedFlag();
        }
        
        _viewModel.MsuSongViewModel.UpdateViewModel(_project, treeData.TrackInfo, newSong, treeData);
        _viewModel.SelectedTreeItem = treeData;
        treeData.UpdateCompletedFlag();
        UpdateCompletedSummary();

        if (_viewModel.MsuSongViewModel.BasicPanelViewModel.IsEnabled && !string.IsNullOrEmpty(initialFile))
        {
            _viewModel.MsuSongViewModel.BasicPanelViewModel.DragDropFile(initialFile);    
        }
        else if (_viewModel.MsuSongViewModel.AdvancedPanelViewModel.IsEnabled && !string.IsNullOrEmpty(initialFile))
        {
            _viewModel.MsuSongViewModel.AdvancedPanelViewModel.DragDropFile(initialFile);    
        }
        
        logger.LogInformation("Successfully added new song");
        
        _viewModel.LastModifiedDate = DateTime.Now;
    }

    public void RemoveSong(MsuProjectWindowViewModelTreeData treeData)
    {
        if (treeData.SongInfo == null || treeData.TrackInfo == null)
        {
            throw new InvalidOperationException("Attempted to delete tree data without song information");
        }
        
        var trackInfo = treeData.TrackInfo;
        var songInfo = treeData.SongInfo;
        var parentSortIndex = treeData.ParentIndex != 0 ? treeData.ParentIndex : treeData.SortIndex;
        var parentTreeData = _viewModel.TreeItems.First(x => x.SortIndex == parentSortIndex);
        
        trackInfo.RemoveSong(songInfo);

        if (trackInfo.Songs.Count <= 1)
        {
            var toRemove = _viewModel.TreeItems.Where(x => x.ParentIndex == parentSortIndex).ToList();
            foreach (var itemToRemove in toRemove)
            {
                _viewModel.TreeItems.Remove(itemToRemove);
            }

            parentTreeData.SongInfo = trackInfo.Songs.Count == 1 ? trackInfo.Songs[0] : null;
            parentTreeData.ChildTreeData.Clear();
            parentTreeData.ToggleAsParent(false, false);
            parentTreeData.UpdateCompletedFlag();
            _viewModel.MsuSongViewModel.UpdateViewModel(_project, trackInfo, parentTreeData.SongInfo, treeData);
            _viewModel.SelectedTreeItem = parentTreeData;
        }
        else
        {
            _viewModel.TreeItems.Remove(treeData);
            parentTreeData.ChildTreeData.Remove(treeData);
            parentTreeData.UpdateCompletedFlag();
            _viewModel.MsuSongViewModel.UpdateViewModel(_project, trackInfo, parentTreeData.ChildTreeData.First().SongInfo, treeData);
            _viewModel.SelectedTreeItem = parentTreeData.ChildTreeData.First();
        }
        
        logger.LogInformation("Removed song");
        UpdateCompletedSummary();
        _viewModel.LastModifiedDate = DateTime.Now;
    }

    public void UpdateDrag(MsuProjectWindowViewModelTreeData? treeData)
    {
        if (_hoveredItem != null)
        {
            _hoveredItem.GridBackground = Brushes.Transparent;
            _hoveredItem.BorderColor = Brushes.Transparent;
        }

        if (treeData == null)
        {
            if (_draggedItem != null && _hoveredItem != null && _draggedItem != _hoveredItem && !(_draggedItem.ParentIndex > 0 && _hoveredItem.ParentIndex > 0 &&
                    _draggedItem.ParentIndex == _hoveredItem.ParentIndex &&
                    _draggedItem.SortIndex == _hoveredItem.SortIndex + 1))
            {
                HandleDragged(_draggedItem, _hoveredItem);
            }
            
            _hoveredItem = null;
            _draggedItem = null;
            _viewModel.IsDraggingItem = false;
        }
        else if (treeData.SongInfo != null)
        {
            _hoveredItem = null;
            _draggedItem = treeData;
            _viewModel.IsDraggingItem = true;
        }
    }

    public void UpdateCompletedFlag(MsuProjectWindowViewModelTreeData treeData)
    {
        if (treeData.SongInfo == null)
        {
            return;
        }
        
        if (_viewModel.SelectedTreeItem == treeData && _viewModel.MsuSongViewModel.SongInfo == treeData.SongInfo)
        {
            _viewModel.MsuSongViewModel.IsComplete = !_viewModel.MsuSongViewModel.IsComplete;
        }
        else
        {
            treeData.SongInfo.IsComplete = !treeData.SongInfo.IsComplete;
            treeData.UpdateCompletedFlag();
            treeData.ParentTreeData?.UpdateCompletedFlag();
        }
        
        logger.LogInformation("Updated completed flag");
        _viewModel.LastModifiedDate = DateTime.Now;
    }

    public void UpdateCheckCopyright(MsuProjectWindowViewModelTreeData treeData)
    {
        if (treeData.SongInfo == null)
        {
            return;
        }
        
        if (_viewModel.SelectedTreeItem == treeData && _viewModel.MsuSongViewModel.SongInfo == treeData.SongInfo)
        {
            if (_viewModel.MsuSongViewModel.BasicPanelViewModel.IsEnabled)
            {
                _viewModel.MsuSongViewModel.BasicPanelViewModel.CheckCopyright =
                    _viewModel.MsuSongViewModel.BasicPanelViewModel.CheckCopyright != true;
            }
            else if (_viewModel.MsuSongViewModel.AdvancedPanelViewModel.IsEnabled)
            {
                _viewModel.MsuSongViewModel.AdvancedPanelViewModel.CheckCopyright = 
                    _viewModel.MsuSongViewModel.AdvancedPanelViewModel.CheckCopyright != true;
            }
        }
        else
        {
            treeData.SongInfo.CheckCopyright = treeData.SongInfo.CheckCopyright != true;
            treeData.UpdateCompletedFlag();
            treeData.ParentTreeData?.UpdateCompletedFlag();
        }
        
        _viewModel.LastModifiedDate = DateTime.Now;
    }
    
    public void UpdateCopyrightSafe(MsuProjectWindowViewModelTreeData treeData)
    {
        if (treeData.SongInfo == null)
        {
            return;
        }
        
        if (_viewModel.SelectedTreeItem == treeData && _viewModel.MsuSongViewModel.SongInfo == treeData.SongInfo)
        {
            if (_viewModel.MsuSongViewModel.BasicPanelViewModel.IsEnabled)
            {
                _viewModel.MsuSongViewModel.BasicPanelViewModel.IsCopyrightSafe =
                    _viewModel.MsuSongViewModel.BasicPanelViewModel.IsCopyrightSafe switch
                    {
                        true => null,
                        false => true,
                        null => false
                    };
            }
            else if (_viewModel.MsuSongViewModel.AdvancedPanelViewModel.IsEnabled)
            {
                _viewModel.MsuSongViewModel.AdvancedPanelViewModel.IsCopyrightSafe =
                    _viewModel.MsuSongViewModel.AdvancedPanelViewModel.IsCopyrightSafe switch
                    {
                        true => null,
                        false => true,
                        null => false
                    };
            }
        }
        else
        {
            if (treeData.SongInfo.IsCopyrightSafe == true)
            {
                treeData.SongInfo.IsCopyrightSafe = null;
            }
            else if (treeData.SongInfo.IsCopyrightSafe == false)
            {
                treeData.SongInfo.IsCopyrightSafe = true;
            }
            else
            {
                treeData.SongInfo.IsCopyrightSafe = false;
            }
            
            treeData.UpdateCompletedFlag();
            treeData.ParentTreeData?.UpdateCompletedFlag();
        }
        
        _viewModel.LastModifiedDate = DateTime.Now;
    }

    public async Task CreateVideo(List<MsuSongInfo> songs, string videoPath, MessageWindow progressWindow, CancellationToken cancellationToken)
    {
        await ITaskService.Run(async () =>
        {
            await Parallel.ForEachAsync(songs,
                new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = cancellationToken }, async (song, _) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    
                    try
                    {
                        await msuPcmService.CreatePcm(_viewModel.MsuProject!, song, false, true, true);
                    }
                    catch (Exception)
                    {
                        // Do nothing
                    }
                });

            var pcmFiles = songs.Select(x => x.OutputPath ?? "").Where(x => !string.IsNullOrEmpty(x) && File.Exists(x)).ToList();
            
            var response = await pythonCompanionService.CreateVideoAsync(new CreateVideoRequest
            {
                Files = pcmFiles,
                OutputVideo = videoPath
            }, progress =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    progressWindow.UpdateProgressBar(progress * 100);
                });
            }, cancellationToken);
        
            Dispatcher.UIThread.Invoke(() =>
            {
                progressWindow.UpdateMessageText(response.Successful
                    ? "Video generation successful"
                    : "Error generating video");
                progressWindow.UpdatePrimaryButtonText("Close");
            });

            settingsService.Settings.PreviousVideoPath = videoPath;
            settingsService.TrySaveSettings();

        }, cancellationToken);
    }

    private void HandleDragged(MsuProjectWindowViewModelTreeData from, MsuProjectWindowViewModelTreeData to)
    {
        if (_viewModel.MsuProject == null || from.TrackInfo == null || to.TrackInfo == null || from.SongInfo == null)
        {
            return;
        }

        var currentParent = from.ParentIndex != 0
            ? _viewModel.TreeItems.First(x => x.SortIndex == from.ParentIndex)
            : from;
        var destinationParent = to.ParentIndex != 0
            ? _viewModel.TreeItems.First(x => x.SortIndex == to.ParentIndex)
            : to;

        if (currentParent.TrackInfo == null || destinationParent.TrackInfo == null)
        {
            return;
        }
        
        var destinationTrack = to.TrackInfo;
        int destinationIndex;
        if (to.ParentIndex == 0)
        {
            destinationIndex = to.SongInfo == null ? 0 : 1;
        }
        else
        {
            destinationIndex = destinationParent.ChildTreeData.IndexOf(to) + 1;
        }
        
        logger.LogInformation("Dragged song {Name} to {Destination} #{Index}", from.SongInfo.SongName, to.TrackInfo.TrackName, destinationIndex);

        if (currentParent == destinationParent && destinationIndex > destinationParent.ChildTreeData.IndexOf(from))
        {
            destinationIndex--;
        }
        
        destinationTrack.MoveSong(_viewModel.MsuProject, from.SongInfo, destinationIndex);

        if (currentParent == destinationParent)
        {
            var currentSongIndex = destinationParent.ChildTreeData.IndexOf(from);
            var destinationTreeIndex = _viewModel.TreeItems.IndexOf(to) + 1;
            if (currentSongIndex < destinationIndex)
            {
                destinationTreeIndex--;
                destinationIndex--;
            }

            destinationParent.ChildTreeData.Remove(from);
            destinationParent.ChildTreeData.Insert(destinationIndex, from);
            _viewModel.TreeItems.Remove(from);
            _viewModel.TreeItems.Insert(destinationTreeIndex, from);
            for (var i = 0; i < destinationParent.ChildTreeData.Count; i++)
            {
                destinationParent.ChildTreeData[i].SortIndex = destinationParent.SortIndex + i + 1;
            }
            
            _viewModel.MsuSongViewModel.UpdateViewModel(_project, destinationTrack, from.SongInfo, from);
            _viewModel.SelectedTreeItem = from;
        }
        else
        {
            var newTreeData = from;
            
            foreach (var previousTree in currentParent.ChildTreeData.Concat(destinationParent.ChildTreeData))
            {
                _viewModel.TreeItems.Remove(previousTree);
            }
            
            currentParent.ChildTreeData.Clear();
            destinationParent.ChildTreeData.Clear();

            if (currentParent.TrackInfo.Songs.Count <= 1)
            {
                currentParent.SongInfo = currentParent.TrackInfo.Songs.FirstOrDefault(); 
                currentParent.UpdateCompletedFlag();
                currentParent.ToggleAsParent(false, false);
            }
            else
            {
                for (var i = 0; i < currentParent.TrackInfo.Songs.Count; i++)
                {
                    var song = currentParent.TrackInfo.Songs[i];

                    var songTreeData = new MsuProjectWindowViewModelTreeData
                    {
                        Name = string.IsNullOrEmpty(song.SongName) ? $"Song {i + 1}" : song.SongName,
                        CollapseIcon = MaterialIconKind.MusicNote,
                        CollapseIconOpacity = 0.4,
                        LeftSpacing = 12,
                        SortIndex = currentParent.SortIndex + 1 + i,
                        ParentIndex = currentParent.SortIndex,
                        ParentTreeData = currentParent,
                        ShowCheckbox = true,
                        TrackInfo = currentParent.TrackInfo,
                        SongInfo = song,
                    };

                    songTreeData.UpdateCompletedFlag();
                    songTreeData.DisplayIsCompleteIcon = _viewModel.DisplayIsCompleteIcon;
                    songTreeData.DisplayHasSongIcon = _viewModel.DisplayHasSongIcon;
                    songTreeData.DisplayCopyrightSafeIcon = _viewModel.DisplayCopyrightSafeIcon;
                    songTreeData.DisplayCheckCopyrightIcon = _viewModel.DisplayCheckCopyrightIcon;

                    currentParent.ChildTreeData.Add(songTreeData);
                    _viewModel.TreeItems.Insert(_viewModel.TreeItems.IndexOf(currentParent) + i + 1, songTreeData);
                }

                currentParent.SongInfo = null;
                currentParent.UpdateCompletedFlag();
                currentParent.ToggleAsParent(true, false);
            }
            
            if (destinationParent.TrackInfo.Songs.Count <= 1)
            {
                destinationParent.SongInfo = destinationParent.TrackInfo.Songs.FirstOrDefault(); 
                destinationParent.UpdateCompletedFlag();
                destinationParent.ToggleAsParent(false, false);
                newTreeData = destinationParent;
            }
            else
            {
                for (var i = 0; i < destinationParent.TrackInfo.Songs.Count; i++)
                {
                    var song = destinationParent.TrackInfo.Songs[i];
                
                    var songTreeData = new MsuProjectWindowViewModelTreeData
                    {
                        Name = string.IsNullOrEmpty(song.SongName) ? $"Song {i + 1}" : song.SongName,
                        CollapseIcon = MaterialIconKind.MusicNote,
                        CollapseIconOpacity = 0.4,
                        LeftSpacing = 12,
                        SortIndex = destinationParent.SortIndex + 1 + i,
                        ParentIndex = destinationParent.SortIndex,
                        ParentTreeData = destinationParent,
                        ShowCheckbox = true,
                        TrackInfo = destinationParent.TrackInfo,
                        SongInfo = song,
                    };

                    if (song == from.SongInfo)
                    {
                        newTreeData = songTreeData;
                    }
                
                    songTreeData.UpdateCompletedFlag();
                    songTreeData.DisplayIsCompleteIcon = _viewModel.DisplayIsCompleteIcon;
                    songTreeData.DisplayHasSongIcon = _viewModel.DisplayHasSongIcon;
                    songTreeData.DisplayCopyrightSafeIcon = _viewModel.DisplayCopyrightSafeIcon;
                    songTreeData.DisplayCheckCopyrightIcon = _viewModel.DisplayCheckCopyrightIcon;
                    
                    destinationParent.ChildTreeData.Add(songTreeData);
                    _viewModel.TreeItems.Insert(_viewModel.TreeItems.IndexOf(destinationParent) + i + 1, songTreeData);
                }
                
                destinationParent.SongInfo = null;
                destinationParent.UpdateCompletedFlag();
                destinationParent.ToggleAsParent(true, false);
            }
            
            _viewModel.MsuSongViewModel.UpdateViewModel(_project, destinationTrack, from.SongInfo, newTreeData);
            _viewModel.SelectedTreeItem = newTreeData;
        }

        if (_viewModel.SelectedTreeItem.SongInfo != null)
        {
            _viewModel.LastModifiedDate = DateTime.Now;
        }
        
        logger.LogInformation("HandleDragged complete");
    }

    public void UpdateHover(MsuProjectWindowViewModelTreeData? treeData)
    {
        if (_hoveredItem != null)
        {
            _hoveredItem.GridBackground = Brushes.Transparent;
            _hoveredItem.BorderColor = Brushes.Transparent;
        }
        
        _hoveredItem = treeData;

        if (treeData != null)
        {
            treeData.BorderColor = MsuProjectWindowViewModelTreeData.HighlightColor;
        }
    }

    public void ToggleCompletedIcons(bool? value = null)
    {
        bool newValue;
        if (value == null)
        {
            newValue = _viewModel.DisplayIsCompleteIcon = !_viewModel.DisplayIsCompleteIcon;
        }
        else
        {
            newValue = _viewModel.DisplayIsCompleteIcon = value.Value;
        }
        
        foreach (var treeData in _viewModel.TreeItems)
        {
            if (treeData.IsSongOrTrack)
            {
                treeData.DisplayIsCompleteIcon = newValue;
            }
        }
        
        settingsService.Settings.ProjectTreeDisplayIsCompleteIcon = _viewModel.DisplayIsCompleteIcon;
        settingsService.TrySaveSettings();
    }
    
    public void ToggleHasAudioIcons(bool? value = null)
    {
        bool newValue;
        if (value == null)
        {
            newValue = _viewModel.DisplayHasSongIcon = !_viewModel.DisplayHasSongIcon;
        }
        else
        {
            newValue = _viewModel.DisplayHasSongIcon = value.Value;
        }
        
        foreach (var treeData in _viewModel.TreeItems)
        {
            if (treeData.IsSongOrTrack)
            {
                treeData.DisplayHasSongIcon = newValue;
            }
        }
        
        settingsService.Settings.ProjectTreeDisplayHasSongIcon = _viewModel.DisplayHasSongIcon;
        settingsService.TrySaveSettings();
    }
    
    public void ToggleCheckCopyrightIcons(bool? value = null)
    {
        bool newValue;
        if (value == null)
        {
            newValue = _viewModel.DisplayCheckCopyrightIcon = !_viewModel.DisplayCheckCopyrightIcon;
        }
        else
        {
            newValue = _viewModel.DisplayCheckCopyrightIcon = value.Value;
        }
        
        foreach (var treeData in _viewModel.TreeItems)
        {
            if (treeData.IsSongOrTrack)
            {
                treeData.DisplayCheckCopyrightIcon = newValue;
            }
        }
        
        settingsService.Settings.ProjectTreeDisplayCheckCopyrightIcon = _viewModel.DisplayCheckCopyrightIcon;
        settingsService.TrySaveSettings();
    }
    
    public void ToggleCopyrightStatusIcons(bool? value = null)
    {
        bool newValue;
        if (value == null)
        {
            newValue = _viewModel.DisplayCopyrightSafeIcon = !_viewModel.DisplayCopyrightSafeIcon;
        }
        else
        {
            newValue = _viewModel.DisplayCopyrightSafeIcon = value.Value;
        }
        
        foreach (var treeData in _viewModel.TreeItems)
        {
            if (treeData.IsSongOrTrack)
            {
                treeData.DisplayCopyrightSafeIcon = newValue;
            }
        }
        
        settingsService.Settings.ProjectTreeDisplayCopyrightSafeIcon = _viewModel.DisplayCopyrightSafeIcon;
        settingsService.TrySaveSettings();
    }

    public string? GetSongCopyDetails(MsuProjectWindowViewModelTreeData treeData)
    {
        
        MsuSongInfo output = new();
        if (treeData.SongInfo == null || !converterService.CloneModel(treeData.SongInfo, output) || !converterService.CloneModel(treeData.SongInfo.MsuPcmInfo, output.MsuPcmInfo))
        {
            return null;
        }
    
        output.TrackNumber = 0;
        output.TrackName = null;
        output.OutputPath = null;
        output.LastModifiedDate = new DateTime();
        output.IsComplete = false;
        output.MsuPcmInfo.ClearFieldsForYaml();
        output.IsAlt = false;
        var yamlText = yamlService.ToYaml(output, YamlType.PascalIgnoreDefaults);
    
        return
            """
            # yaml-language-server: $schema=https://raw.githubusercontent.com/MattEqualsCoder/MSUScripter/main/Schemas/MsuSongInfo.json
            # Use Visual Studio Code with the YAML plugin from redhat for schema support (make sure the language is set to YAML)

            """ + yamlText;
    }

    public string? PasteSongDetails(MsuProjectWindowViewModelTreeData treeData, string yaml)
    {
        if (yamlService.FromYaml<MsuSongInfo>(yaml, YamlType.PascalIgnoreDefaults, out var songInfo, out var error))
        {
            treeData.SongInfo = songInfo;

            if (treeData.ParentTreeData != null && !string.IsNullOrEmpty(treeData.SongInfo?.SongName))
            {
                treeData.Name = treeData.SongInfo.SongName;
            }
            
            UpdateCompletedSummary();
            
            return null;
        }

        return error ?? "Unable to copy data";
    }

    public void InputFileUpdated()
    {
        _viewModel.CurrentTreeItem?.UpdateCompletedFlag();
    }

    public void OnClose()
    {
        statusBarService.StatusBarTextUpdated -= StatusBarServiceOnStatusBarTextUpdated;
        msuPcmService.GeneratingPcm -= MsuPcmServiceOnGeneratingPcm;
        _ = audioPlayerService.StopSongAsync();
    }

    public void DragDropFile(string filePath)
    {
        if (_viewModel.CurrentTreeItem?.TrackInfo == null || _viewModel.CurrentTreeItem?.SongInfo == null)
        {
            return;
        }
        
        logger.LogInformation("Dragged {File} to {Track}", filePath, _viewModel.MsuSongViewModel.TrackInfo!.TrackNumber);
        
        if (_viewModel.MsuSongViewModel.BasicPanelViewModel.IsEnabled)
        {
            _viewModel.MsuSongViewModel.BasicPanelViewModel.DragDropFile(filePath);  
        }
        else if (_viewModel.MsuSongViewModel.AdvancedPanelViewModel is { IsEnabled: true, CurrentTreeItem.MsuPcmInfo: not null })
        {
            _viewModel.MsuSongViewModel.AdvancedPanelViewModel.DragDropFile(filePath);
        }
    }

    public bool CanCreateVideos()
    {
        return pythonCompanionService.IsValid;
    }

    public void LogError(Exception ex, string message)
    {
        logger.LogError(ex, "{Message}", message);
    }
}