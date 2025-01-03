﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Models;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class TrackOverviewPanel : UserControl
{
    private TrackOverviewPanelService? _service;

    public static readonly StyledProperty<EditProjectPanelViewModel> ProjectProperty = AvaloniaProperty.Register<MsuSongInfoPanel, EditProjectPanelViewModel>(
        nameof(Project));

    public EditProjectPanelViewModel Project
    {
        get => GetValue(ProjectProperty);
        set => SetValue(ProjectProperty, value);
    }
    
    public TrackOverviewPanel()
    {
        if (Design.IsDesignMode)
        {
            DataContext = new TrackOverviewPanelViewModel().DesignerExample();
            return;
        }

        ProjectProperty.Changed.Subscribe(x =>
        {
            if (x.Sender != this || (EditProjectPanelViewModel?)x.NewValue.Value == null)
            {
                return;
            }
            _service = this.GetControlService<TrackOverviewPanelService>();
            DataContext = _service?.InitializeModel(x.NewValue.Value);    
        });

        IsVisibleProperty.Changed.Subscribe(x =>
        {
            if (x.Sender != this || x.NewValue.Value != true) return;
            _service?.RefreshTracks();
        });

        InitializeComponent();
        
        AddHandler(DragDrop.DropEvent, DropFile);
    }

    private async void DropFile(object? sender, DragEventArgs e)
    {
        try
        {
            if (_service == null) return;

            var obj = e.Source;
            while (obj is not DataGridRow)
            {
                if (obj is not Control control) return;
                obj = control.Parent;
            }

            if (obj is not DataGridRow {  DataContext: TrackOverviewPanelViewModel.TrackOverviewRow row }) return;

            var file = e.Data.GetFiles()?.FirstOrDefault();

            if (file == null || string.IsNullOrEmpty(file.Path.LocalPath)) return;

            await OpenAddSongWindow(row, file.Path.LocalPath);
        }
        catch
        {
            await MessageWindow.ShowErrorDialog("Unable to open song window from dropped file");
        }
    }
    
    public event EventHandler<TrackEventArgs>? OnSelectedTrack;

    private void AudioDataGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not TextBlock && e.Source is not Border { Name: "CellBorder"})
        {
            return;
        }

        var selectedItems = this.Find<DataGrid>(nameof(TrackDataGrid))!.SelectedItems;
        if (selectedItems.Count <= 0)
        {
            return;
        }

        if (selectedItems[0] is not TrackOverviewPanelViewModel.TrackOverviewRow row)
        {
            return;
        }
        
        OnSelectedTrack?.Invoke(this, new TrackEventArgs(row.TrackNumber));
    }

    private async void OpenAddSongWindowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { Tag: TrackOverviewPanelViewModel.TrackOverviewRow row }) return;
            await OpenAddSongWindow(row, null);
        }
        catch
        {
            await MessageWindow.ShowErrorDialog("Unable to open add song window");
        }
    }

    private async Task OpenAddSongWindow(TrackOverviewPanelViewModel.TrackOverviewRow row, string? filePath)
    {
        if (_service == null) return;
        var window = new AddSongWindow(_service.GetProject(), row.TrackNumber, filePath, true);
        var newSongInfo = await window.ShowDialog<MsuSongInfoViewModel?>(TopLevel.GetTopLevel(this) as Window ?? App.MainWindow);
        if (newSongInfo == null) return;
        _service.AddSong(row, newSongInfo);
    }

    public void Refresh()
    {
        _service?.RefreshTracks();
    }

    private void ToggleCopyrightSafeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TrackOverviewPanelViewModel.TrackOverviewRow row } || row.SongInfo == null) return;

        if (row.SongInfo.IsCopyrightSafe == null)
        {
            row.SongInfo.IsCopyrightSafe = true;
        }
        else if (row.SongInfo.IsCopyrightSafe == true)
        {
            row.SongInfo.IsCopyrightSafe = false;
        }
        else
        {
            row.SongInfo.IsCopyrightSafe = null;
        }
    }

    private void ToggleCheckCopyrightButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TrackOverviewPanelViewModel.TrackOverviewRow row } || row.SongInfo == null) return;
        
        row.SongInfo.CheckCopyright = !row.SongInfo.CheckCopyright;
    }
    
    private void ToggleCompleteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TrackOverviewPanelViewModel.TrackOverviewRow row } || row.SongInfo == null) return;
        
        row.SongInfo.IsComplete = !row.SongInfo.IsComplete;
        _service?.UpdateCompletedTrackDetails();
    }
}