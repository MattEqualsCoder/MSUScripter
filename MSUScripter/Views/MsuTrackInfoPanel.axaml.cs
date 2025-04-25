using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;
using ReactiveUI;

namespace MSUScripter.Views;

public partial class MsuTrackInfoPanel : UserControl
{
    public void SetTrackInfo(MsuProjectViewModel project, MsuTrackInfoViewModel trackInfo)
    {
        trackInfo.Project = project;
        DataContext = trackInfo;
    }
    
    private readonly MsuTrackInfoPanelService? _service;
    
    private MsuTrackInfoViewModel? TrackData => DataContext as MsuTrackInfoViewModel;
    
    public MsuTrackInfoPanel()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            DataContext = new MsuSongMsuPcmInfoViewModel().DesignerExample();
        }
        else
        {
            _service = this.GetControlService<MsuTrackInfoPanelService>();
            DataContextChanged += (_, _) =>
            {
                if (DataContext is MsuTrackInfoViewModel trackInfoViewModel)
                {
                    _service?.InitializeModel(trackInfoViewModel);    
                }
            };
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        if (Design.IsDesignMode)
        {
            return;
        }

        if (DataContext is MsuTrackInfoViewModel trackInfoViewModel && !string.IsNullOrEmpty(trackInfoViewModel.Description))
        {
            trackInfoViewModel.RaisePropertyChanged(nameof(trackInfoViewModel.Description));
        }
    }

    private void AddSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.AddSong();
    }

    private void AddSongWindowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (TrackData == null) return;
        var window = new AddSongWindow(TrackData.Project, TrackData.TrackNumber, null);
        window.ShowDialog(TopLevel.GetTopLevel(this) as Window ?? App.MainWindow!);
    }

    public void UpdateScrollValue(Vector vector)
    {
        if (_service == null)
        {
            return;
        }
        
        if (TrackData == null || TrackData.Songs.Count == 0)
        {
            _service.SetScrolledSong(null, -1);
            return;
        }
        var parentControl = this.FindControl<ItemsControl>("SongItemsControl")!;
        var songContentPresenters = parentControl.GetLogicalChildren().Select(x => x as ContentPresenter).Select(x => x!.Bounds).ToList();

        var index = -1;
        for (var i = 0; i < TrackData.Songs.Count - 1; i++)
        {
            if (songContentPresenters[i].Y <= vector.Y && songContentPresenters[i + 1].Y > vector.Y)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            index = TrackData.Songs.Count - 1;
        }
        
        var song = TrackData.Songs.ToList()[index];
        _service.SetScrolledSong(song, index);
    }

    private void ScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer) return;
        UpdateScrollValue(scrollViewer.Offset);
    }

    private async void PlayButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_service == null) return;
            var errorMessage = await _service.PlayCurrent();
            if (!string.IsNullOrEmpty(errorMessage))
            {
                await MessageWindow.ShowErrorDialog(errorMessage, "Error", TopLevel.GetTopLevel(this) as Window);
            }
        }
        catch
        {
            // Do nothing
        }
    }

    private async void LoopButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_service == null) return;
            var errorMessage = await _service.LoopCurrent();
            if (!string.IsNullOrEmpty(errorMessage))
            {
                await MessageWindow.ShowErrorDialog(errorMessage, "Error", TopLevel.GetTopLevel(this) as Window);
            }
        }
        catch
        {
            // Do nothing
        }
    }

    private async void PauseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_service == null) return;
            await _service.PauseCurrent();
        }
        catch
        {
            // Do nothing
        }
    }
}