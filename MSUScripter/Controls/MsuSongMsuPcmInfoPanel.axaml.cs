using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.Services;
using MSUScripter.ViewModels;
using Newtonsoft.Json;

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
        Dispatcher.UIThread.InvokeAsync(AddSubTrack);
    }

    private async Task AddSubTrack()
    {
        if (!SettingsService.Instance.Settings.HideSubTracksSubChannelsWarning && MsuPcmData.HasSubChannels)
        {
            var result = await new MessageWindow("PCM files can't be generated with both a sub track and a sub channel at the same level. Before generating the PCM, you'll need to make sure it has one or the other.", MessageWindowType.DoNotShowAgain, "Warning")
                .ShowDialog();

            if (result == MessageWindowResult.DontShow)
            {
                SettingsService.Instance.Settings.HideSubTracksSubChannelsWarning = true;
                SettingsService.Instance.SaveSettings();
            }
        }
        
        MsuPcmData.AddSubTrack();
    }

    private void AddSubChannelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(AddSubChannel);
    }
    
    private async Task AddSubChannel()
    {
        if (!SettingsService.Instance.Settings.HideSubTracksSubChannelsWarning && MsuPcmData.HasSubTracks)
        {
            var result = await new MessageWindow("PCM files can't be generated with both a sub track and a sub channel at the same level. Before generating the PCM, you'll need to make sure it has one or the other.", MessageWindowType.DoNotShowAgain, "Warning")
                .ShowDialog();

            if (result == MessageWindowResult.DontShow)
            {
                SettingsService.Instance.Settings.HideSubTracksSubChannelsWarning = true;
                SettingsService.Instance.SaveSettings();
            }
        }
        
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

    private void MsuSongMsuPcmInfoPanel_OnPcmOptionSelected(object? sender, PcmEventArgs e)
    {
        PcmOptionSelected?.Invoke(sender, e);
    }

    private void GetTrimStartButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PcmOptionSelected?.Invoke(this, new PcmEventArgs(MsuPcmData.Song, PcmEventType.StartingSamples, MsuPcmData));
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

    private void Copy_OnClick(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(CopyMsuPcmDetails);
    }

    private void PasteMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

            if (clipboard == null)
            {
                return;
            }
        
            var yamlText = await clipboard.GetTextAsync();

            if (string.IsNullOrEmpty(yamlText))
            {
                return;
            }

            if (!YamlService.Instance.FromYaml<MsuSongMsuPcmInfo>(yamlText, out var yamlMsuPcmDetails, out _, false) || yamlMsuPcmDetails == null)
            {
                _ = await new MessageWindow("Invalid msupcm++ track details", MessageWindowType.Error, "Error")
                    .ShowDialog();
                return;
            }

            var originalProject = MsuPcmData.Project;
            var originalSong = MsuPcmData.Song;
            var originalIsAlt = MsuPcmData.IsAlt;
            var originalParent = MsuPcmData.ParentMsuPcmInfo;
            
            if (!ConverterService.Instance.ConvertViewModel(yamlMsuPcmDetails, MsuPcmData))
            {
                _ = await new MessageWindow("Invalid msupcm++ track details", MessageWindowType.Error, "Error")
                    .ShowDialog();
            }
            
            MsuPcmData.ApplyCascadingSettings(originalProject, originalSong, originalIsAlt, originalParent, true);
            MsuPcmData.LastModifiedDate = DateTime.Now;
        });
    }

    private void ContextMenu_OnOpening(object? sender, CancelEventArgs e)
    {
        if (sender is not ContextMenu contextMenu)
        {
            return;
        }
        
        var pasteMenuItem = contextMenu.Items.FirstOrDefault(x => x is MenuItem { Name: "PasteMenuItem" }) as MenuItem;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

        if (pasteMenuItem == null || clipboard == null)
        {
            return;
        }

        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            pasteMenuItem.IsEnabled = !string.IsNullOrWhiteSpace(await clipboard.GetTextAsync());
        });
    }

    private async Task<bool> CopyMsuPcmDetails()
    {
        MsuSongMsuPcmInfo output = new();
        if (!ConverterService.Instance.ConvertViewModel(MsuPcmData, output))
        {
            return false;
        }
        output.ClearLastModifiedDate();

        try
        {
            var yamlText = YamlService.Instance.ToYaml(output, false);
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard == null)
            {
                return false;
            }
            await clipboard.SetTextAsync(yamlText);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void Insert_OnClick(object? sender, RoutedEventArgs e)
    {
        if (MsuPcmData.ParentMsuPcmInfo == null)
        {
            return;
        }

        if (MsuPcmData.IsSubChannel)
        {
            var index = MsuPcmData.ParentMsuPcmInfo.SubChannels.IndexOf(MsuPcmData);
            MsuPcmData.ParentMsuPcmInfo.AddSubChannel(index);
        }
        else if (MsuPcmData.IsSubTrack)
        {
            var index = MsuPcmData.ParentMsuPcmInfo.SubTracks.IndexOf(MsuPcmData);
            MsuPcmData.ParentMsuPcmInfo.AddSubTrack(index);
        }
    }
}