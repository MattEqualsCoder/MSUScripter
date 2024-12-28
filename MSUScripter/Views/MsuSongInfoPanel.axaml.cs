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
using MSUScripter.Tools;
using MSUScripter.ViewModels;
using FileInputControlType = AvaloniaControls.FileInputControlType;

namespace MSUScripter.Views;

public partial class MsuSongInfoPanel : UserControl
{
    public static readonly StyledProperty<MsuSongInfoViewModel> SongProperty = AvaloniaProperty.Register<MsuSongInfoPanel, MsuSongInfoViewModel>(
        nameof(Song));

    private MsuSongInfoPanelService? _service;

    public MsuSongInfoViewModel Song
    {
        get => GetValue(SongProperty);
        set => SetValue(SongProperty, value);
    }
    
    public MsuSongInfoPanel()
    {
        InitializeComponent();
        
        if (Design.IsDesignMode)
        {
            DataContext = new MsuSongMsuPcmInfoViewModel().DesignerExample();
        }
        else
        {
            _service = this.GetControlService<MsuSongInfoPanelService>();
        }
        
        SongProperty.Changed.Subscribe(x =>
        {
            if (x.Sender != this || (MsuSongInfoViewModel?)x.NewValue.Value == null)
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
        var response = await MessageWindow.ShowYesNoDialog("Are you sure you want to delete this song?", "Delete song?",
            TopLevel.GetTopLevel(this) as Window);
        if (response)
        {
            _service?.DeleteSong();
        }
    }

    private async void PlaySongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service == null) return;
        var errorMessage = await _service.PlaySong(false);
        if (!string.IsNullOrEmpty(errorMessage))
        {
            await MessageWindow.ShowErrorDialog(errorMessage, "Error", TopLevel.GetTopLevel(this) as Window);
        }
    }

    private async void TestLoopButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service == null) return;
        var errorMessage = await _service.PlaySong(true);
        if (!string.IsNullOrEmpty(errorMessage))
        {
            await MessageWindow.ShowErrorDialog(errorMessage, "Error", TopLevel.GetTopLevel(this) as Window);
        }
    }

    private async void ImportSongMetadataButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var directory = _service?.GetOpenMusicFilePath() ?? await this.GetDocumentsFolderPath();
        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        var file = await CrossPlatformTools.OpenFileDialogAsync(TopLevel.GetTopLevel(this) as Window ?? App.MainWindow!,
            FileInputControlType.OpenFile, filter: "All Files:*.*", path: directory, title: "Select Audio File");
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        if (!string.IsNullOrEmpty(file?.Path.LocalPath))
        {
            _service?.ImportAudioMetadata(file.Path.LocalPath);
        }
    }

    private async void StopMusicButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service == null) return;
        await _service.PauseSong();
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
        
        if (contextMenu.Items.FirstOrDefault(x => x is MenuItem { Name: "PasteMenuItem" }) is MenuItem pasteMenuItem)
        {
            pasteMenuItem.IsEnabled = !string.IsNullOrEmpty((await this.GetClipboardAsync())?.Trim());    
        }
        
        contextMenu.PlacementTarget = button;
        contextMenu.Open();
        e.Handled = true;
    }

    private void DuplicateSongMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var window = new DuplicateMoveTrackWindow(Song.Project,
            Song.Project.Tracks.First(x => x.TrackNumber == Song.TrackNumber), Song, CopyMoveType.Copy);
        window.ShowDialog(App.MainWindow!);
    }

    private void MoveSongMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var window = new DuplicateMoveTrackWindow(Song.Project,
            Song.Project.Tracks.First(x => x.TrackNumber == Song.TrackNumber), Song, CopyMoveType.Move);
        window.ShowDialog(App.MainWindow!);
    }
    
    private void SwapSongMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var window = new DuplicateMoveTrackWindow(Song.Project,
            Song.Project.Tracks.First(x => x.TrackNumber == Song.TrackNumber), Song, CopyMoveType.Swap);
        window.ShowDialog(App.MainWindow!);
    }

    private async void CopySongToClipboardMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var yamlText = _service?.GetCopyDetailsString();
        if (string.IsNullOrEmpty(yamlText)) return;
        await this.SetClipboardAsync(yamlText);
    }

    private async void PasteSongFromClipboardMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var yamlText = await this.GetClipboardAsync();
        if (string.IsNullOrEmpty(yamlText)) return;
        var error = _service?.CopyDetailsFromString(yamlText);
        if (!string.IsNullOrEmpty(error))
        {
            await MessageWindow.ShowErrorDialog(error, "Error", TopLevel.GetTopLevel(this) as Window);
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
    
    private async void CreateEmptyPcmFileButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await GeneratePcm(false, true);
    }
    
    private async Task GeneratePcm(bool asPrimary, bool asEmpty)
    {
        if (_service == null) return;
        
        var response = await _service.GeneratePcmFile(asPrimary, asEmpty);
        if (!response.Successful)
        {
            await MessageWindow.ShowErrorDialog(response.Message ?? "Unknown error generating the PCM file via msupcm++", "Error", TopLevel.GetTopLevel(this) as Window);
        }
        else if (response is { Successful: false, GeneratedPcmFile: true })
        {
            var window = new MessageWindow(new MessageWindowRequest
            {
                Message = response.Message ?? "Unknown error generating the PCM file via msupcm++",
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

    private void TestAudioLevelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.AnalyzeAudio();
    }

    private async void ContextMenu_OnOpening(object? sender, CancelEventArgs e)
    {
        if (sender is not ContextMenu contextMenu)
        {
            return;
        }

        if (contextMenu.Items.FirstOrDefault(x => x is MenuItem { Name: "PasteMenuItem" }) is MenuItem pasteMenuItem)
        {
            pasteMenuItem.IsEnabled = !string.IsNullOrEmpty((await this.GetClipboardAsync())?.Trim());    
        }
    }

    private void IsCopyrightSafeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Song.IsCopyrightSafe == null)
        {
            Song.IsCopyrightSafe = true;
        }
        else if (Song.IsCopyrightSafe == true)
        {
            Song.IsCopyrightSafe = false;
        }
        else
        {
            Song.IsCopyrightSafe = null;
        }
    }

    private void CheckCopyrightButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Song.CheckCopyright = !Song.CheckCopyright;
    }
    
    private void IsCompleteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Song.IsComplete = !Song.IsComplete;
    }
}