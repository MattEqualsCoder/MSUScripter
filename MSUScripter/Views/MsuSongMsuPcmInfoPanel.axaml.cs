using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using AvaloniaControls.Models;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.Services;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;
using Tmds.DBus.Protocol;

namespace MSUScripter.Views;

public partial class MsuSongMsuPcmInfoPanel : UserControl
{
    private readonly MsuSongMsuPcmInfoPanelService? _service;
    
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

        if (Design.IsDesignMode)
        {
            DataContext = new MsuSongMsuPcmInfoViewModel().DesignerExample();
        }
        else
        {
            _service = this.GetControlService<MsuSongMsuPcmInfoPanelService>();
        }
        
        MsuPcmDataProperty.Changed.Subscribe(x =>
        {
            if (x.Sender != this || (MsuSongMsuPcmInfoViewModel?)x.NewValue.Value == null)
            {
                return;
            }
            _service?.InitializeModel(x.NewValue.Value);
        });
        
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    public event EventHandler<BasicEventArgs>? FileUpdated;

    private async void RemoveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await MessageWindow.ShowYesNoDialog("Are you sure you want to delete these msupcm++ details?",
                "Delete details"))
        {
            return;
        }

        _service?.Delete();
    }

    private async void AddSubTrackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await ShowSubTracksSubChannelsWarningPopup(true, false);
        _service?.AddSubTrack();
    }

    private async void AddSubChannelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await ShowSubTracksSubChannelsWarningPopup(false, true);
        _service?.AddSubChannel();
    }

    private async Task ShowSubTracksSubChannelsWarningPopup(bool newSubTrack, bool newSubChannel)
    {
        if (_service?.ShouldShowSubTracksSubChannelsWarningPopup(newSubTrack, newSubChannel) == true)
        {
            var window = new MessageWindow(new MessageWindowRequest
            {
                Message = "PCM files can't be generated with both a sub track and a sub channel at the same level. Before generating the PCM, you'll need to make sure it has one or the other.",
                Title = "Warning",
                Icon = MessageWindowIcon.Warning,
                Buttons = MessageWindowButtons.OK,
                CheckBoxText = "Don't show this again"
            });

            await window.ShowDialog(this);
        
            if (window.DialogResult?.CheckedBox == true)
            {
                _service?.HideSubTracksSubChannelsWarning();
            }
        }
    }
    
    private async void PlaySongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service == null)
        {
            return;
        }
        
        var errorMessage = await _service.PlaySong(false);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            await MessageWindow.ShowErrorDialog(errorMessage, "Error", TopLevel.GetTopLevel(this) as Window);
        }
    }

    private async void TestLoopButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.PlaySong(true);
    }

    private async Task GeneratePcm(bool asPrimary, bool asEmpty)
    {
        if (_service == null) return;
        
        var successful = _service.GeneratePcmFile(asPrimary, asEmpty, out var error, out var msuPcmError);
        if (!successful)
        {
            await MessageWindow.ShowErrorDialog(error, "Error", TopLevel.GetTopLevel(this) as Window);
        }
        else if (msuPcmError)
        {
            var window = new MessageWindow(new MessageWindowRequest
            {
                Message = error,
                Buttons = MessageWindowButtons.OK,
                Icon = MessageWindowIcon.Warning,
                CheckBoxText = "Ignore future warnings for this song"
            });

            await window.ShowDialog(TopLevel.GetTopLevel(this) as Control ?? this);

            if (window.DialogResult is { PressedAcceptButton: true, CheckedBox: true })
            {
                _service.IgnoreMsuPcmError();
            }
        }
    }
    private async void GenerateAsMainPcmFileButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await GeneratePcm(true, false);
    }

    private async void GeneratePcmFileButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await GeneratePcm(false, false);
    }

    private void MsuSongMsuPcmInfoPanel_OnFileUpdated(object? sender, BasicEventArgs e)
    {
        FileUpdated?.Invoke(sender, e);
    }

    private async void LoopWindowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service?.HasLoopDetails() == true)
        {
            var result = await MessageWindow.ShowYesNoDialog(
                "Either the trim end or loop point have a value. Are you sure you want to overwrite them?",
                "Override Loop Data?", TopLevel.GetTopLevel(this) as Window);
            if (!result)
                return;
        }
        
        var window = new PyMusicLooperWindow(); 
        window.SetDetails(MsuPcmData.Project, MsuPcmData.Song, MsuPcmData);
        var loopResult = await window.ShowDialog();
        if (loopResult != null)
        {
            _service?.UpdateLoopSettings(loopResult);
        }
    }

    private void StopButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.StopSong();
        //PcmOptionSelected?.Invoke(this, new PcmEventArgs(MsuPcmData.Song, PcmEventType.StopMusic));
    }

    private async void CreateEmptyPcmFileButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await GeneratePcm(false, true);
    }

    private async void GetTrimStartButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var response = _service?.GetStartingSamples();
        if (!string.IsNullOrEmpty(response))
        {
            await MessageWindow.ShowErrorDialog(response, "Error", TopLevel.GetTopLevel(this) as Window);
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

    private async void Copy_OnClick(object? sender, RoutedEventArgs e)
    {
        var yaml = _service?.GetCopyDetailsString();
        
        if (string.IsNullOrEmpty(yaml))
        {
            return;
        }

        await this.SetClipboardAsync(yaml);
    }

    private async void PasteMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var yamlText = await this.GetClipboardAsync();

        if (yamlText == null)
        {
            return;
        }
        
        var error = _service?.CopyDetailsFromString(yamlText);

        if (!string.IsNullOrEmpty(error))
        {
            await MessageWindow.ShowErrorDialog(error, "Error", TopLevel.GetTopLevel(this) as Window);
        }
    }

    private async void ContextMenu_OnOpening(object? sender, CancelEventArgs e)
    {
        if (sender is not ContextMenu contextMenu)
        {
            return;
        }

        if (contextMenu.Items.FirstOrDefault(x => x is MenuItem { Name: "PasteMenuItem" }) is MenuItem pasteMenuItem)
        {
            pasteMenuItem.IsEnabled = !string.IsNullOrWhiteSpace(await this.GetClipboardAsync());    
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
            _service?.AddSubChannel(index, true);
        }
        else if (MsuPcmData.IsSubTrack)
        {
            var index = MsuPcmData.ParentMsuPcmInfo.SubTracks.IndexOf(MsuPcmData);
            _service?.AddSubTrack(index, true);
        }
    }

    private void FileControl_OnOnUpdated(object? sender, FileControlUpdatedEventArgs e)
    {
        FileUpdated?.Invoke(this, new BasicEventArgs(e.Path));
    }
}