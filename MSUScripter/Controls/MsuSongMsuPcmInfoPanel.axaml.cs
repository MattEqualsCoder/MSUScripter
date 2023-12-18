using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MSUScripter.Models;
using MSUScripter.Services;
using MSUScripter.Tools;
using MSUScripter.ViewModels;

namespace MSUScripter.Controls;

public partial class MsuSongMsuPcmInfoPanel : UserControl
{
    public static readonly StyledProperty<MsuSongMsuPcmInfoViewModel> MsuPcmDataProperty = AvaloniaProperty.Register<MsuSongMsuPcmInfoPanel, MsuSongMsuPcmInfoViewModel>(
        "MsuPcmData");

    public MsuSongMsuPcmInfoViewModel MsuPcmData
    {
        get => GetValue(MsuPcmDataProperty);
        set => SetValue(MsuPcmDataProperty, value);
    }
    
    public MsuSongMsuPcmInfoPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    public event EventHandler? OnDelete;
    
    public event EventHandler<PcmEventArgs>? PcmOptionSelected;

    public event EventHandler<BasicEventArgs>? FileUpdated;

    private async void RemoveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var result = await new MessageWindow("Are you sure you want to delete these msupcm++ details?", MessageWindowType.YesNo, "Delete details?")
            .ShowDialog();

        if (result != MessageWindowResult.Yes) return;
        
        OnDelete?.Invoke(this, new RoutedEventArgs(e.RoutedEvent, this));
    }

    private void AddSubTrackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        MsuPcmData.AddSubTrack();
    }

    private void AddSubChannelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        MsuPcmData.AddSubChannel();
    }

    private void MsuSongMsuPcmInfoPanelSubChannel_OnOnDelete(object? sender, EventArgs e)
    {
        if (sender is not MsuSongMsuPcmInfoPanel panel) return;
        MsuPcmData.RemoveSubChannel(panel.MsuPcmData);
    }
    
    private void MsuSongMsuPcmInfoPanelSubTrack_OnOnDelete(object? sender, EventArgs e)
    {
        if (sender is not MsuSongMsuPcmInfoPanel panel) return;
        MsuPcmData.RemoveSubTrack(panel.MsuPcmData);
    }

    private void PlaySongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PcmOptionSelected?.Invoke(this, new PcmEventArgs(MsuPcmData.Song));
    }

    private void TestLoopButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PcmOptionSelected?.Invoke(this, new PcmEventArgs(MsuPcmData.Song, PcmEventType.PlayLoop));
    }

    private void GenerateAsMainPcmFileButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PcmOptionSelected?.Invoke(this, new PcmEventArgs(MsuPcmData.Song, PcmEventType.GenerateAsPrimary));
    }

    private void GeneratePcmFileButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PcmOptionSelected?.Invoke(this, new PcmEventArgs(MsuPcmData.Song, PcmEventType.Generate));
    }

    private void FileControl_OnOnUpdated(object? sender, BasicEventArgs e)
    {
        FileUpdated?.Invoke(this, new BasicEventArgs(e.Data));
    }

    private void MsuSongMsuPcmInfoPanel_OnFileUpdated(object? sender, BasicEventArgs e)
    {
        FileUpdated?.Invoke(sender, e);
    }

    private void LoopWindowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PcmOptionSelected?.Invoke(this, new PcmEventArgs(MsuPcmData.Song, PcmEventType.LoopWindow, MsuPcmData));
    }

    private void StopButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PcmOptionSelected?.Invoke(this, new PcmEventArgs(MsuPcmData.Song, PcmEventType.StopMusic));
    }

    private void CreateEmptyPcmFileButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PcmOptionSelected?.Invoke(this, new PcmEventArgs(MsuPcmData.Song, PcmEventType.GenerateEmpty));
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (IAudioPlayerService.CanPlaySongs != true)
        {
            this.Find<Button>(nameof(PlaySongButton))!.IsVisible = false;
            this.Find<Button>(nameof(TestLoopButton))!.IsVisible = false;
            this.Find<Button>(nameof(StopButton))!.IsVisible = false;
        }
    }
}