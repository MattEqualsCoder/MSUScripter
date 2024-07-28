using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using AvaloniaControls.Models;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

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

    private async void GetTrimStartButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var response = _service?.GetStartingSamples();
        if (!string.IsNullOrEmpty(response))
        {
            await MessageWindow.ShowErrorDialog(response, "Error", TopLevel.GetTopLevel(this) as Window);
        }
    }

    private async void GetTrimEndButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var response = _service?.GetEndingSamples();
        if (!string.IsNullOrEmpty(response))
        {
            await MessageWindow.ShowErrorDialog(response, "Error", TopLevel.GetTopLevel(this) as Window);
        }
    }

    private async void MenuButton_OnClick(object? sender, RoutedEventArgs e)
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
        
        await UpdateContextMenu(contextMenu);
        
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

        await UpdateContextMenu(contextMenu);
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
        _service?.ImportAudioMetadata();
    }

    private async Task UpdateContextMenu(ContextMenu contextMenu)
    {
        if (contextMenu.Items.FirstOrDefault(x => x is MenuItem { Name: "PasteMenuItem" }) is MenuItem pasteMenuItem)
        {
            pasteMenuItem.IsEnabled = !string.IsNullOrEmpty((await this.GetClipboardAsync())?.Trim());    
        }
        
        _service?.UpdateContextMenuOptions();
    }

    private void MoveUpMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.MoveUp();
    }

    private void MoveDownMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.MoveDown();
    }
}