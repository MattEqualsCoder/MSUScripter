using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MSUScripter.Models;
using MSUScripter.Services;
using MSUScripter.Tools;
using MSUScripter.ViewModels;
using Tmds.DBus.Protocol;

namespace MSUScripter.Controls;

public partial class MsuSongInfoPanel : UserControl
{
    public static readonly StyledProperty<MsuSongInfoViewModel> SongProperty = AvaloniaProperty.Register<MsuSongInfoPanel, MsuSongInfoViewModel>(
        "Song");

    public MsuSongInfoViewModel Song
    {
        get => GetValue(SongProperty);
        set => SetValue(SongProperty, value);
    }
    
    public MsuSongInfoPanel()
    {
        InitializeComponent();
    }

    public event EventHandler? OnDelete;
    
    public event EventHandler<PcmEventArgs>? PcmOptionSelected; 
    
    public event EventHandler<SongFileEventArgs>? FileUpdated;
    
    public event EventHandler<SongFileEventArgs>? MetaDataFileSelected;

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void RemoveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var result = await new MessageWindow("Are you sure you want to delete this song?", MessageWindowType.YesNo, "Delete song?")
            .ShowDialog();

        if (result != MessageWindowResult.Yes) return;
        
        OnDelete?.Invoke(this, new RoutedEventArgs(e.RoutedEvent, this));
    }

    private void PlaySongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PcmOptionSelected?.Invoke(this, new PcmEventArgs(Song));
    }

    private void TestLoopButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PcmOptionSelected?.Invoke(this, new PcmEventArgs(Song, PcmEventType.PlayLoop));
    }

    private void MsuSongMsuPcmInfoPanel_OnPcmOptionSelected(object? sender, PcmEventArgs e)
    {
        PcmOptionSelected?.Invoke(sender, e);
    }

    private void MsuSongMsuPcmInfoPanel_OnFileUpdated(object? sender, BasicEventArgs e)
    {
        FileUpdated?.Invoke(sender, new SongFileEventArgs(Song, e.Data ?? "", false));
    }

    private async void ImportSongMetadataButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Audio File",
            FileTypeFilter = new []{ new FilePickerFileType("All Files") { Patterns = new List<string>() {"*.*"}}}
        });

        if (!string.IsNullOrEmpty(files.FirstOrDefault()?.Path.LocalPath))
        {
            MetaDataFileSelected?.Invoke(this, new SongFileEventArgs(Song, files.FirstOrDefault()?.Path.LocalPath ?? "", true));
        }
    }

    private void StopMusicButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PcmOptionSelected?.Invoke(this, new PcmEventArgs(Song, PcmEventType.StopMusic));
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (IAudioPlayerService.CanPlaySongs != true)
        {
            this.Find<Button>(nameof(PlaySongButton))!.IsVisible = false;
            this.Find<Button>(nameof(TestLoopButton))!.IsVisible = false;
            this.Find<Button>(nameof(StopMusicButton))!.IsVisible = false;
        }
    }

    private void MenuButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        var contextMenu = button.ContextMenu;
        if (contextMenu == null)
        {
            return;
        }
        
        contextMenu.PlacementTarget = button;
        contextMenu.Open();
        e.Handled = true;
    }

    private void CopySongMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var window = new SelectTrackWindow();
        window.ShowDialog(App.MainWindow!, Song.Project,
            Song.Project.Tracks.First(x => x.TrackNumber == Song.TrackNumber), Song, false);
    }

    private void MoveSongMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var window = new SelectTrackWindow();
        window.ShowDialog(App.MainWindow!, Song.Project,
            Song.Project.Tracks.First(x => x.TrackNumber == Song.TrackNumber), Song, true);
    }
}