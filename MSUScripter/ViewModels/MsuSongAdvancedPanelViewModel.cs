using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaControls.Models;
using Material.Icons;
using MSUScripter.Configs;
using MSUScripter.Models;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class MsuSongAdvancedPanelViewModel : SavableViewModelBase
{
    [Reactive, SkipLastModified] public bool IsScratchPad { get; set; }
    
    [Reactive] public string? SongName { get; set; }

    [Reactive] public string? ArtistName { get; set; }

    [Reactive] public string? Album { get; set; }

    [Reactive] public string? Url { get; set; }

    [Reactive] public bool IsAlt { get; set; }
    [Reactive] public bool? CheckCopyright { get; set; }
    
    [Reactive] public bool? IsCopyrightSafe { get; set; }
    
    [Reactive, SkipLastModified] public bool IsEnabled { get; set; }
    [Reactive, SkipLastModified] public bool IsAdvancedMode { get; set; } = true;
    
    [Reactive] public int? Loop { get; set; }

    [Reactive] public int? TrimStart { get; set; }

    [Reactive] public int? TrimEnd { get; set; }

    [Reactive] public int? FadeIn { get; set; }

    [Reactive] public int? FadeOut { get; set; }

    [Reactive] public int? CrossFade { get; set; }

    [Reactive] public int? PadStart { get; set; }

    [Reactive] public int? PadEnd { get; set; }

    [Reactive] public double? Tempo { get; set; }

    [Reactive] public double? Normalization { get; set; }

    [Reactive] public bool? Compression { get; set; }
    
    [Reactive] public bool? Dither { get; set; }
    [Reactive] public bool ShowDither { get; set; }

    [Reactive] public string? Output { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(CanPressPyMusicLooperButton))] public string? Input { get; set; }
    
    [Reactive, SkipLastModified] public bool DisplaySampleRateWarning { get; set; }
    [Reactive, SkipLastModified] public bool DisplayMultipleTracksWarning { get; set; }
    [Reactive, SkipLastModified] public bool DisplayDualTrackTypeWarning { get; set; }
    
    [Reactive, SkipLastModified] public bool DisplayOutputPcmFile { get; set; }
    [Reactive, SkipLastModified] public bool CanUpdateOutputPcmFile { get; set; }
    [Reactive, SkipLastModified, ReactiveLinkedProperties(nameof(CanPressPyMusicLooperButton))] public bool IsGeneratingPcmFile { get; set; }
    public MsuProject Project { get; set; } = null!;

    public MsuSongAdvancedPanelViewModelModelTreeData CurrentTreeItem { get; set; } = null!;

    public MsuSongAdvancedPanelViewModelModelTreeData? DraggedItem => _draggedItem;
    public bool IsDraggingItem => _draggedItem != null;
    
    public ObservableCollection<MsuSongAdvancedPanelViewModelModelTreeData> TreeItems { get; set; } = [];
    [Reactive, SkipLastModified] public MsuSongAdvancedPanelViewModelModelTreeData? SelectedTreeItem { get; set; }
    public bool CanPressPyMusicLooperButton => !string.IsNullOrEmpty(Input) && !IsGeneratingPcmFile;
    
    public ContextMenu? CurrentContextMenu { get; set; }
    
    public event EventHandler? ViewModelUpdated;
    public event EventHandler? FileDragDropped;
    
    private MsuTrackInfo? _currentTrackInfo;
    public MsuSongInfo? CurrentSongInfo { get; private set; }
    private MsuSongMsuPcmInfo? _currentSongMsuPcmInfo;
    private MsuProjectWindowViewModelTreeData? _treeData;
    private MsuSongAdvancedPanelViewModelModelTreeData? _hoveredItem;
    private MsuSongAdvancedPanelViewModelModelTreeData? _draggedItem;
    private bool _isTopLevelMsuPcmInfo;
    private bool _updatingModel;

    public MsuSongAdvancedPanelViewModel()
    {
        PropertyChanged += OnPropertyChanged;
        return;

        void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_updatingModel) return;
            if (e.PropertyName == nameof(SongName) && _treeData is { ParentTreeData: not null } && !string.IsNullOrEmpty(SongName))
            {
                _treeData.Name = SongName ?? "Test";
            }
            else if (e.PropertyName is nameof(CheckCopyright) or nameof(IsCopyrightSafe))
            {
                SaveChanges();
                _treeData?.UpdateCompletedFlag();
                _treeData?.ParentTreeData?.UpdateCompletedFlag();
            }
        }
    }
    
    public void UpdateViewModel(MsuProject project, MsuTrackInfo trackInfo, MsuSongInfo songInfo, MsuProjectWindowViewModelTreeData treeData)
    {
        Project = project;
        _currentSongMsuPcmInfo = null;
        _updatingModel = true;
        _currentTrackInfo = trackInfo;
        CurrentSongInfo = songInfo;
        _treeData = treeData;
        
        SongName = songInfo.SongName;
        ArtistName = songInfo.Artist;
        Album = songInfo.Album;
        Url = songInfo.Url;
        CheckCopyright = songInfo.CheckCopyright;
        IsCopyrightSafe = songInfo.IsCopyrightSafe;
        IsScratchPad = trackInfo.IsScratchPad;
        Dither = songInfo.MsuPcmInfo.Dither;
        
        TreeItems.Clear();
        AddTreeItem(songInfo.MsuPcmInfo, 0, false, 0, 0, -1, null);
        SetSelectedTreeData(TreeItems[0]);
        
        IsAdvancedMode = true;
        IsEnabled = true;
        HasBeenModified = false;
        _updatingModel = false;
        LastModifiedDate = songInfo.LastModifiedDate;
        ViewModelUpdated?.Invoke(this, EventArgs.Empty);
    }
    
    private int AddTreeItem(MsuSongMsuPcmInfo msuPcmInfo, int level, bool isChannel, int index, int sortIndex, int parentSortIndex, MsuSongAdvancedPanelViewModelModelTreeData? parentTreeData)
    {
        var currentIndex = sortIndex;
        
        string name;
        if (level == 0)
        {
            name = "Top Level";
        }
        else if (isChannel)
        {
            var fileName = string.IsNullOrEmpty(msuPcmInfo.File) ? "" : Path.GetFileName(msuPcmInfo.File);
            name = $"SC{index + 1} {fileName}";
        }
        else
        {
            var fileName = string.IsNullOrEmpty(msuPcmInfo.File) ? "" : Path.GetFileName(msuPcmInfo.File);
            name = $"ST{index + 1} {fileName}";
        }

        var mainTreeData = new MsuSongAdvancedPanelViewModelModelTreeData()
        {
            Name = name,
            Level = level,
            IsVisible = true,
            ChevronIcon = MaterialIconKind.ChevronDown,
            Icon = MaterialIconKind.Note,
            SortIndex = currentIndex,
            MsuPcmInfo = msuPcmInfo,
            ParentIndex = parentSortIndex,
            ShowOutput = level == 0,
            SongInfo = CurrentSongInfo,
            ParentTreeData = parentTreeData,
            IsSubChannel = parentTreeData != null && isChannel,
            IsSubTrack = parentTreeData != null && !isChannel,
        };

        TreeItems.Insert(sortIndex, mainTreeData);
        if (TreeItems.Count == 1)
        {
            SelectedTreeItem = TreeItems[0];
        }
        if (parentTreeData != null)
        {
            parentTreeData.ChildrenTreeData.Insert(index, mainTreeData);
        }

        currentIndex++;

        var subTrackTreeData = new MsuSongAdvancedPanelViewModelModelTreeData()
        {
            Name = "Sub Tracks",
            Level = level + 1,
            IsVisible = true,
            ChevronIcon = MaterialIconKind.ChevronDown,
            Icon = MaterialIconKind.Note,
            SortIndex = currentIndex,
            ParentIndex = sortIndex,
            ParentTreeData = mainTreeData,
            IsSubTrack = true,
        };
        
        mainTreeData.ChildrenTreeData.Add(subTrackTreeData);
        TreeItems.Insert(currentIndex, subTrackTreeData);

        currentIndex++;

        for (var i = 0; i < msuPcmInfo.SubTracks.Count; i++)
        {
            currentIndex = AddTreeItem(msuPcmInfo.SubTracks[i], level + 2, false, i, currentIndex, sortIndex, subTrackTreeData);
        }

        var subChannelTreeData = new MsuSongAdvancedPanelViewModelModelTreeData()
        {
            Name = "Sub Channels",
            Level = level + 1,
            IsVisible = true,
            ChevronIcon = MaterialIconKind.ChevronDown,
            Icon = MaterialIconKind.Note,
            SortIndex = currentIndex,
            ParentIndex = sortIndex,
            ParentTreeData = mainTreeData,
            IsSubChannel = true,
        };
        
        mainTreeData.ChildrenTreeData.Add(subChannelTreeData);
        TreeItems.Insert(currentIndex, subChannelTreeData);

        currentIndex++;
        
        for (var i = 0; i < msuPcmInfo.SubChannels.Count; i++)
        {
            currentIndex = AddTreeItem(msuPcmInfo.SubChannels[i], level + 2, true, i, currentIndex, sortIndex, subChannelTreeData);
        }
        
        return currentIndex;
    }

    public void SetSelectedTreeData(MsuSongAdvancedPanelViewModelModelTreeData treeData)
    {
        if (treeData.MsuPcmInfo == null) return;

        if (_currentSongMsuPcmInfo != null)
        {
            SaveChanges();
        }
        
        var currentLastModifiedTime = LastModifiedDate;
        
        Loop = treeData.MsuPcmInfo.Loop;
        TrimStart = treeData.MsuPcmInfo.TrimStart;
        TrimEnd = treeData.MsuPcmInfo.TrimEnd;
        FadeIn = treeData.MsuPcmInfo.FadeIn;
        FadeOut = treeData.MsuPcmInfo.FadeOut;
        CrossFade = treeData.MsuPcmInfo.CrossFade;
        PadStart = treeData.MsuPcmInfo.PadStart;
        PadEnd = treeData.MsuPcmInfo.PadEnd;
        Tempo = treeData.MsuPcmInfo.Tempo;
        Normalization = treeData.MsuPcmInfo.Normalization;
        Compression = treeData.MsuPcmInfo.Compression;
        _isTopLevelMsuPcmInfo = treeData.ParentIndex < 0;
        if (_isTopLevelMsuPcmInfo)
        {
            Output = CurrentSongInfo?.OutputPath ?? treeData.MsuPcmInfo.Output;
            ShowDither = Project.BasicInfo.DitherType is DitherType.DefaultOff or DitherType.DefaultOn;
        }
        else
        {
            Output = "";
            ShowDither = false;
        }
        Input = treeData.MsuPcmInfo.File;
        DisplayOutputPcmFile = treeData.ShowOutput && !IsScratchPad;;
        CanUpdateOutputPcmFile = treeData.SongInfo?.IsAlt == true;
        _currentSongMsuPcmInfo = treeData.MsuPcmInfo;
        CurrentTreeItem = treeData;
        
        LastModifiedDate = currentLastModifiedTime;
    }
    
    public override void SaveChanges()
    {
        if (CurrentSongInfo == null || _currentSongMsuPcmInfo == null) return;
        CurrentSongInfo.SongName = SongName;
        CurrentSongInfo.Artist = ArtistName;
        CurrentSongInfo.Album = Album;
        CurrentSongInfo.Url = Url;
        CurrentSongInfo.CheckCopyright = CheckCopyright;
        CurrentSongInfo.IsCopyrightSafe = IsCopyrightSafe;
        _currentSongMsuPcmInfo.Loop = Loop;
        _currentSongMsuPcmInfo.TrimStart = TrimStart;
        _currentSongMsuPcmInfo.TrimEnd = TrimEnd;
        _currentSongMsuPcmInfo.FadeIn = FadeIn;
        _currentSongMsuPcmInfo.FadeOut = FadeOut;
        _currentSongMsuPcmInfo.CrossFade = CrossFade;
        _currentSongMsuPcmInfo.PadStart = PadStart;
        _currentSongMsuPcmInfo.PadEnd = PadEnd;
        _currentSongMsuPcmInfo.Tempo = Tempo;
        _currentSongMsuPcmInfo.Normalization = Normalization;
        _currentSongMsuPcmInfo.Compression = Compression;
        _currentSongMsuPcmInfo.Dither = Dither;

        if (_isTopLevelMsuPcmInfo)
        {
            CurrentSongInfo.OutputPath = CurrentSongInfo.OutputPath;
            _currentSongMsuPcmInfo.Output = CurrentSongInfo.OutputPath;
        }
        
        _currentSongMsuPcmInfo.File = Input;
        HasBeenModified = false;
    }

    public void UpdateDrag(MsuSongAdvancedPanelViewModelModelTreeData? treeData)
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
        }
        else if (treeData.SongInfo != null)
        {
            _hoveredItem = null;
            _draggedItem = treeData;
        }
    }
    
    public void UpdateHover(MsuSongAdvancedPanelViewModelModelTreeData? treeData)
    {
        if (_hoveredItem != null)
        {
            _hoveredItem.GridBackground = Brushes.Transparent;
            _hoveredItem.BorderColor = Brushes.Transparent;
        }
        
        _hoveredItem = treeData;

        if (treeData != null)
        {
            treeData.BorderColor = MsuSongAdvancedPanelViewModelModelTreeData.HighlightColor;
        }
    }

    public void RemoveMsuPcmInfo(MsuSongAdvancedPanelViewModelModelTreeData treeData)
    {
        if (treeData.ParentTreeData != null)
        {
            RemoveFromTree(treeData);
            treeData.ParentTreeData?.ChildrenTreeData.Remove(treeData);
            if (treeData is { IsSubChannel: true, MsuPcmInfo: not null })
            {
                treeData.ParentTreeData?.ParentTreeData?.MsuPcmInfo?.SubChannels.Remove(treeData.MsuPcmInfo);
            }
            else if (treeData is { IsSubTrack: true, MsuPcmInfo: not null })
            {
                treeData.ParentTreeData?.ParentTreeData?.MsuPcmInfo?.SubTracks.Remove(treeData.MsuPcmInfo);
            }
            UpdateSortIndexes();
        }
        else
        {
            var newMsuPcmInfo = new MsuSongMsuPcmInfo()
            {
                Output = treeData.MsuPcmInfo?.Output
            };
            TreeItems.Clear();
            AddTreeItem(newMsuPcmInfo, 0, false, 0, 0, -1, null);
            SetSelectedTreeData(TreeItems[0]);
        }
    }
    
    public void ReplaceMsuPcmInfo(MsuSongAdvancedPanelViewModelModelTreeData treeData, MsuSongMsuPcmInfo pcmInfo)
    {
        var index = treeData.ParentTreeData?.ChildrenTreeData.IndexOf(treeData) ?? 0;
        RemoveFromTree(treeData);

        if (treeData.ParentTreeData?.ParentTreeData?.MsuPcmInfo != null)
        {
            if (treeData.ParentTreeData.Name == "Sub Channels")
            {
                if (treeData.MsuPcmInfo != null)
                {
                    treeData.ParentTreeData.ParentTreeData.MsuPcmInfo.SubChannels.Remove(treeData.MsuPcmInfo);    
                }
                treeData.ParentTreeData.ParentTreeData.MsuPcmInfo.SubChannels.Insert(0, pcmInfo);
            }
            else
            {
                if (treeData.MsuPcmInfo != null)
                {
                    treeData.ParentTreeData.ParentTreeData.MsuPcmInfo.SubTracks.Remove(treeData.MsuPcmInfo);    
                }
                treeData.ParentTreeData.ParentTreeData.MsuPcmInfo.SubTracks.Insert(0, pcmInfo);
            }
            treeData.ParentTreeData?.ChildrenTreeData.Remove(treeData);    
        }
        else if (CurrentSongInfo != null)
        {
            CurrentSongInfo.MsuPcmInfo = pcmInfo;
            _currentSongMsuPcmInfo = pcmInfo;
        }

        _currentSongMsuPcmInfo = null;
        AddTreeItem(pcmInfo, treeData.Level, treeData.ParentTreeData?.Name == "Sub Channels", index, treeData.SortIndex, treeData.ParentIndex, treeData.ParentTreeData);
        UpdateSortIndexes();
        SetSelectedTreeData(TreeItems.First(x => x.MsuPcmInfo == pcmInfo));
    }
    
    public void DragDropFile(string fileName)
    {
        Input = fileName;
        FileDragDropped?.Invoke(this, EventArgs.Empty);
    }

    private void HandleDragged(MsuSongAdvancedPanelViewModelModelTreeData from, MsuSongAdvancedPanelViewModelModelTreeData to)
    {
        var parentTreeData = to.MsuPcmInfo == null ? to : to.ParentTreeData;
        var parent = parentTreeData?.ParentTreeData?.MsuPcmInfo;
        var destinationIndex = to.MsuPcmInfo == null ? 0 : to.ParentTreeData?.ChildrenTreeData.IndexOf(to) + 1;

        if (parentTreeData == null || parent == null || from.MsuPcmInfo == null)
        {
            return;
        }

        var currentToNode = to.ParentTreeData;
        while (currentToNode != null)
        {
            if (currentToNode == from)
            {
                return;
            }

            currentToNode = currentToNode.ParentTreeData;
        }
        
        var updatedIndex = parent.MoveSubInfo(from.MsuPcmInfo, parentTreeData.IsSubTrack, destinationIndex ?? 0, from.ParentTreeData?.ParentTreeData?.MsuPcmInfo);
        
        RemoveFromTree(from);
        InsertAfter(from, to);
        
        from.ParentTreeData?.ChildrenTreeData.Remove(from);
        parentTreeData.ChildrenTreeData.Insert(updatedIndex, from);
        from.ParentTreeData = parentTreeData;
        from.IsSubChannel = parentTreeData.IsSubChannel;
        from.IsSubTrack = parentTreeData.IsSubTrack;

        UpdateTabbing(from);
        UpdateSortIndexes();
        
        LastModifiedDate = DateTime.Now;
    }

    private void UpdateTabbing(MsuSongAdvancedPanelViewModelModelTreeData data)
    {
        data.Level = (data.ParentTreeData?.Level ?? 0) + 1;
        foreach (var child in data.ChildrenTreeData)
        {
            UpdateTabbing(child);
        }
    }
    
    private void RemoveFromTree(MsuSongAdvancedPanelViewModelModelTreeData data)
    {
        TreeItems.Remove(data);
        foreach (var item in data.ChildrenTreeData)
        {
            RemoveFromTree(item);
        }
    }

    private void InsertAfter(MsuSongAdvancedPanelViewModelModelTreeData from,
        MsuSongAdvancedPanelViewModelModelTreeData to)
    {
        var insertIndex = TreeItems.IndexOf(to) + 1;
        if (to.MsuPcmInfo != null)
        {
            while (TreeItems[insertIndex].Level > to.Level && insertIndex < TreeItems.Count)
            {
                insertIndex++;
            }
        }

        InsertAt(insertIndex, from);

        return;

        int InsertAt(int index, MsuSongAdvancedPanelViewModelModelTreeData item)
        {
            TreeItems.Insert(index, item);
            index++;
            
            foreach (var child in item.ChildrenTreeData)
            {
                index = InsertAt(index, child);
            }

            return index;

        }
    }

    private void UpdateSortIndexes()
    {
        var index = 0;
        foreach (var item in TreeItems)
        {
            item.SortIndex = index;
            item.ParentIndex = item.ParentTreeData?.SortIndex ?? -1;
            UpdateTreeItemName(item);
            index++;
        }
    }
    
    public void UpdateTreeItemName(MsuSongAdvancedPanelViewModelModelTreeData? item = null)
    {
        item ??= CurrentTreeItem;
        if (item.ParentTreeData?.IsSubTrack == true && item.MsuPcmInfo != null)
        {
            var subTrackIndex = item.ParentTreeData.ChildrenTreeData.IndexOf(item) + 1;
            var fileName = string.IsNullOrEmpty(item.MsuPcmInfo?.File)
                ? ""
                : Path.GetFileName(item.MsuPcmInfo?.File);
            item.Name = $"ST{subTrackIndex} {fileName}";
        }
        else if (item.ParentTreeData?.IsSubChannel == true && item.MsuPcmInfo != null)
        {
            var subTrackIndex = item.ParentTreeData.ChildrenTreeData.IndexOf(item) + 1;
            var fileName = string.IsNullOrEmpty(item.MsuPcmInfo?.File)
                ? ""
                : Path.GetFileName(item.MsuPcmInfo?.File);
            item.Name = $"SC{subTrackIndex} {fileName}";
        }
    }

    public MsuSongAdvancedPanelViewModelModelTreeData AddMsuPcmInfo(MsuSongAdvancedPanelViewModelModelTreeData to)
    {
        var parentTreeData = to.MsuPcmInfo == null ? to : to.ParentTreeData;
        var parent = parentTreeData?.ParentTreeData?.MsuPcmInfo;
        var destinationIndex = to.MsuPcmInfo == null ? 0 : to.ParentTreeData?.ChildrenTreeData.IndexOf(to) + 1;

        if (parentTreeData == null || destinationIndex == null)
        {
            throw new InvalidOperationException();
        }
        
        var newMsuPcmInfo = new MsuSongMsuPcmInfo();

        var newData = new MsuSongAdvancedPanelViewModelModelTreeData()
        {
            Name = "Temp",
            Level = parentTreeData.Level + 1,
            IsVisible = true,
            ChevronIcon = MaterialIconKind.ChevronDown,
            Icon = MaterialIconKind.Note,
            SortIndex = parentTreeData.SortIndex + destinationIndex.Value,
            MsuPcmInfo = newMsuPcmInfo,
            ParentIndex = parentTreeData.SortIndex,
            ShowOutput = false,
            SongInfo = CurrentSongInfo,
            ParentTreeData = parentTreeData,
            IsSubChannel = parentTreeData.IsSubChannel,
            IsSubTrack = parentTreeData.IsSubTrack,
        };

        newData.ChildrenTreeData.Add(new MsuSongAdvancedPanelViewModelModelTreeData()
        {
            Name = "Sub Tracks",
            Level = parentTreeData.Level + 2,
            IsVisible = true,
            ChevronIcon = MaterialIconKind.ChevronDown,
            Icon = MaterialIconKind.Note,
            SortIndex = parentTreeData.SortIndex + destinationIndex.Value + 1,
            ParentIndex = parentTreeData.SortIndex + destinationIndex.Value,
            ParentTreeData = newData,
            IsSubTrack = true,
        });
            
        newData.ChildrenTreeData.Add(new MsuSongAdvancedPanelViewModelModelTreeData()
        {
            Name = "Sub Channels",
            Level = parentTreeData.Level + 2,
            IsVisible = true,
            ChevronIcon = MaterialIconKind.ChevronDown,
            Icon = MaterialIconKind.Note,
            SortIndex = parentTreeData.SortIndex + destinationIndex.Value + 1,
            ParentIndex = parentTreeData.SortIndex + destinationIndex.Value,
            ParentTreeData = newData,
            IsSubChannel = true
        });
        
        InsertAfter(newData, to);
        
        parentTreeData.ChildrenTreeData.Insert(destinationIndex.Value, newData);
        newData.ParentTreeData = parentTreeData;

        UpdateTabbing(newData);
        UpdateSortIndexes();

        if (parentTreeData.IsSubTrack)
        {
            parentTreeData.ParentTreeData?.MsuPcmInfo?.SubTracks.Insert(destinationIndex.Value, newMsuPcmInfo);
        }
        else if (parentTreeData.IsSubChannel)
        {
            parentTreeData.ParentTreeData?.MsuPcmInfo?.SubChannels.Insert(destinationIndex.Value, newMsuPcmInfo);
        }

        return newData;
    }
    
    public void UpdateTrackWarnings(bool sampleRateWarning, bool multiWarning, bool dualTypes)
    {
        DisplaySampleRateWarning = sampleRateWarning;
        DisplayMultipleTracksWarning = multiWarning;
        DisplayDualTrackTypeWarning = dualTypes;
    }

    public override ViewModelBase DesignerExample()
    {
        return new MsuSongAdvancedPanelViewModel()
        {
            TreeItems =
            [
                new MsuSongAdvancedPanelViewModelModelTreeData()
                {
                    Name = "Top Level",
                    Level = 0,
                    IsVisible = true,
                    ChevronIcon = MaterialIconKind.ChevronDown,
                    Icon = MaterialIconKind.Note
                },
                new MsuSongAdvancedPanelViewModelModelTreeData()
                {
                    Name = "Sub Tracks",
                    Level = 1,
                    IsVisible = true,
                    ChevronIcon = MaterialIconKind.ChevronDown,
                    Icon = MaterialIconKind.DotsHorizontal
                },
                new MsuSongAdvancedPanelViewModelModelTreeData()
                {
                    Name = "test.mp4",
                    Level = 2,
                    IsVisible = true,
                    ChevronIcon = MaterialIconKind.ChevronDown,
                    Icon = MaterialIconKind.Bullet
                },
                new MsuSongAdvancedPanelViewModelModelTreeData()
                {
                    Name = "Sub Tracks",
                    Level = 3,
                    IsVisible = true,
                    ChevronIcon = MaterialIconKind.ChevronDown,
                    Icon = MaterialIconKind.DotsHorizontal
                },
                new MsuSongAdvancedPanelViewModelModelTreeData()
                {
                    Name = "Sub Channels",
                    Level = 3,
                    IsVisible = true,
                    ChevronIcon = MaterialIconKind.ChevronDown,
                    Icon = MaterialIconKind.Equal
                },
                new MsuSongAdvancedPanelViewModelModelTreeData()
                {
                    Name = "test.mp4",
                    Level = 2,
                    IsVisible = true,
                    ChevronIcon = MaterialIconKind.ChevronDown,
                    Icon = MaterialIconKind.Bullet
                },
                new MsuSongAdvancedPanelViewModelModelTreeData()
                {
                    Name = "Sub Tracks",
                    Level = 3,
                    IsVisible = true,
                    ChevronIcon = MaterialIconKind.ChevronDown,
                    Icon = MaterialIconKind.DotsHorizontal
                },
                new MsuSongAdvancedPanelViewModelModelTreeData()
                {
                    Name = "Sub Channels",
                    Level = 3,
                    IsVisible = true,
                    ChevronIcon = MaterialIconKind.ChevronDown,
                    Icon = MaterialIconKind.Equal
                },
                new MsuSongAdvancedPanelViewModelModelTreeData()
                {
                    Name = "Sub Channels",
                    Level = 1,
                    IsVisible = true,
                    ChevronIcon = MaterialIconKind.ChevronDown,
                    Icon = MaterialIconKind.Equal
                },
                new MsuSongAdvancedPanelViewModelModelTreeData()
                {
                    Name = "1 test.mp4",
                    Level = 2,
                    IsVisible = true,
                    ChevronIcon = MaterialIconKind.ChevronDown,
                    Icon = MaterialIconKind.Bullet
                },
                new MsuSongAdvancedPanelViewModelModelTreeData()
                {
                    Name = "Sub Tracks",
                    Level = 3,
                    IsVisible = true,
                    ChevronIcon = MaterialIconKind.ChevronDown,
                    Icon = MaterialIconKind.DotsHorizontal
                },
                new MsuSongAdvancedPanelViewModelModelTreeData()
                {
                    Name = "Sub Channels",
                    Level = 3,
                    IsVisible = true,
                    ChevronIcon = MaterialIconKind.ChevronDown,
                    Icon = MaterialIconKind.Equal
                },
            ]
        };
    }
}

[SkipLastModified]
public class MsuSongAdvancedPanelViewModelModelTreeData : TranslatedViewModelBase
{
    public static IBrush HighlightColor { get; set; } = Brushes.SlateGray;
    
    [Reactive] public required MaterialIconKind ChevronIcon { get; set; }
    [Reactive] public required MaterialIconKind Icon { get; set; }
    
    [Reactive] public required string Name { get; set; }
    
    [ReactiveLinkedProperties(nameof(LeftSpacing))]
    public required int Level { get; set; }
    
    [Reactive] public IBrush GridBackground { get; set; } = Brushes.Transparent;
    [Reactive] public IBrush BorderColor { get; set; } = Brushes.Transparent;
    [Reactive] public bool IsVisible { get; set; } = true;
    [Reactive] public bool ShowAddButton { get; set; }
    [Reactive] public bool ShowMenuButton { get; set; }
    [Reactive] public bool EnableMenuItems { get; set; }
    public int LeftSpacing => Level * 12;
    public int SortIndex { get; set; }
    public int ParentIndex { get; set; }
    public bool IsSubTrack { get; set; }
    public bool IsSubChannel { get; set; }
    public MsuSongMsuPcmInfo? MsuPcmInfo { get; set; }
    
    public MsuTrackInfo? TrackInfo { get; set; }
    public MsuSongInfo? SongInfo { get; set; }
    
    public bool IsCollapsed { get; set; }
    public bool ShowOutput { get; set; }
    
    
    public MsuSongAdvancedPanelViewModelModelTreeData? ParentTreeData { get; set; }
    public List<MsuSongAdvancedPanelViewModelModelTreeData> ChildrenTreeData { get; set; } = [];
    
    public override ViewModelBase DesignerExample()
    {
        throw new System.NotImplementedException();
    }

    public void ToggleCollapsed(bool? newVal = null)
    {
        IsCollapsed = newVal ?? !IsCollapsed;

        foreach (var child in ChildrenTreeData)
        {
            child.UpdateVisibility(!IsCollapsed);
        }
    }

    private void UpdateVisibility(bool newValue)
    {
        IsVisible = newValue;

        var childVisibility = IsVisible && !IsCollapsed;

        foreach (var child in ChildrenTreeData)
        {
            child.UpdateVisibility(childVisibility);
        }
    }
}