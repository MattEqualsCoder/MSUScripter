using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class TrackOverviewPanel : UserControl
{
    public TrackOverviewPanel()
    {
        InitializeComponent();
    }

    public event EventHandler<(MsuTrackInfo Track, MsuSongInfo? Song)>? ClickedSong;
    public event EventHandler<(MsuTrackInfo Track, MsuSongInfo? Song)>? AddSong;
    public event EventHandler<(MsuTrackInfo Track, MsuSongInfo? Song)>? ToggleComplete;
    public event EventHandler<(MsuTrackInfo Track, MsuSongInfo? Song)>? ToggleCopyrightSafe;
    public event EventHandler<(MsuTrackInfo Track, MsuSongInfo? Song)>? ToggleCheckCopyright;
    public event EventHandler? UpdatedSettings;

    private void AddSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TrackOverviewPanelViewModel.TrackOverviewRow row })
        {
            return;
        }
        
        AddSong?.Invoke(this, (row.Track, row.SongInfo));
    }

    private void TrackDataGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not TrackOverviewPanelViewModel model || model.SelectedIndex < 0 || model.SelectedIndex >= model.Rows.Count)
        {
            return;
        }

        var row = model.Rows[model.SelectedIndex];
        ClickedSong?.Invoke(this, (row.Track, row.SongInfo));
    }

    private void ToggleCompleteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TrackOverviewPanelViewModel.TrackOverviewRow row })
        {
            return;
        }
        
        ToggleComplete?.Invoke(this, (row.Track, row.SongInfo));
        row.UpdateIcons();
    }

    private void ToggleCopyrightSafeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TrackOverviewPanelViewModel.TrackOverviewRow row })
        {
            return;
        }
        
        ToggleCopyrightSafe?.Invoke(this, (row.Track, row.SongInfo));
        row.UpdateIcons();
    }

    private void ToggleCheckCopyrightButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TrackOverviewPanelViewModel.TrackOverviewRow row })
        {
            return;
        }
        
        ToggleCheckCopyright?.Invoke(this, (row.Track, row.SongInfo));
        row.UpdateIcons();
    }

    public void UpdateIconsForSong(MsuSongInfo songInfo)
    {
        if (DataContext is not TrackOverviewPanelViewModel model)
        {
            return;
        }

        var row = model.Rows.FirstOrDefault(x => x.SongInfo == songInfo);

        row?.UpdateIcons();
    }

    private void ShowIsCompleteMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not TrackOverviewPanelViewModel model)
        {
            return;
        }

        model.ShowCompleteColumn = !model.ShowCompleteColumn;
        model.Settings.TrackOverviewShowIsCompleteIcon = model.ShowCompleteColumn;
        UpdatedSettings?.Invoke(this, EventArgs.Empty);
    }

    private void ShowCopyrightSafeMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not TrackOverviewPanelViewModel model)
        {
            return;
        }

        model.ShowCopyrightSafeColumn = !model.ShowCopyrightSafeColumn;
        model.Settings.TrackOverviewShowCopyrightSafeIcon = model.ShowCopyrightSafeColumn;
        UpdatedSettings?.Invoke(this, EventArgs.Empty);
    }

    private void ShowCheckCopyrightMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not TrackOverviewPanelViewModel model)
        {
            return;
        }

        model.ShowCheckCopyrightColumn = !model.ShowCheckCopyrightColumn;
        model.Settings.TrackOverviewShowCheckCopyrightIcon = model.ShowCheckCopyrightColumn;
        UpdatedSettings?.Invoke(this, EventArgs.Empty);
    }

    private void ShowAudioFilesMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not TrackOverviewPanelViewModel model)
        {
            return;
        }

        model.ShowHasAudioColumn = !model.ShowHasAudioColumn;
        model.Settings.TrackOverviewShowHasSongIcon = model.ShowHasAudioColumn;
        UpdatedSettings?.Invoke(this, EventArgs.Empty);
    }
}