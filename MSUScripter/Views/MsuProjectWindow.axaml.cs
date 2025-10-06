using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using AvaloniaControls.Models;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.Services.ControlServices;
using MSUScripter.Tools;
using MSUScripter.ViewModels;
using FileInputControlType = AvaloniaControls.FileInputControlType;
using Timer = System.Timers.Timer;

namespace MSUScripter.Views;

public partial class MsuProjectWindow : RestorableWindow
{
    private readonly MsuProjectWindowViewModel? _viewModel;
    private readonly MsuProjectWindowService? _service;
    private readonly Action _performTextFilter;
    private readonly MainWindow? _parentWindow;
    private readonly Timer _backupTimer = new(TimeSpan.FromSeconds(60));
    private ContextMenu? _currentContextMenu;
    private MsuProjectWindowViewModelTreeData? _draggedTreeItem;
    private MsuProjectWindowViewModelTreeData? _hoverValue;
    private MsuProjectWindowViewModelTreeData? _previousHoverTreeItem;
    private bool _forceClose;
    
    // ReSharper disable once UnusedMember.Global
    public MsuProjectWindow()
    {
        InitializeComponent();
        DataContext = new MsuProjectWindowViewModel().DesignerExample();
        var performSearch = () => _service?.FilterTree();
        _performTextFilter = performSearch.Debounce();
    }
    
    public MsuProjectWindow(MsuProject project, MainWindow parentWindow)
    {
        _service = this.GetControlService<MsuProjectWindowService>();
        InitializeComponent();
        DataContext = _viewModel = _service!.InitViewModel(project);
        var performSearch = () => _service?.FilterTree();
        _performTextFilter = performSearch.Debounce(200);
        _parentWindow = parentWindow;
        _backupTimer.Elapsed += BackupTimerOnElapsed;
        _backupTimer.Start();
        AddHandler(DragDrop.DropEvent, DropFile);
    }
    
    protected override string RestoreFilePath => Path.Combine(Directories.BaseFolder, "Windows", "project-window.json");
    protected override int DefaultWidth => 1280;
    protected override int DefaultHeight => 800;

    private void DropFile(object? sender, DragEventArgs e)
    {
        var file = e.Data.GetFiles()?.FirstOrDefault();
        if (file == null || _viewModel?.CurrentTreeItem?.TrackInfo == null || _service == null)
        {
            return;
        }

        if (_viewModel.CurrentTreeItem.SongInfo == null)
        {
            AddNewSong(_viewModel.CurrentTreeItem, false, file.Path.LocalPath);
            return;
        }
        
        _service?.DragDropFile(file.Path.LocalPath);
    }
    
    public MsuProjectWindowCloseReason CloseReason { get; private set; }
    
    public string OpenProjectPath { get; private set; } = string.Empty;
    
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        
        if (_viewModel?.IsDraggingItem != true)
        {
            return;
        }

        var listBox = this.GetControl<ListBox>(nameof(TreeListBox));
        var point = e.GetPosition(listBox);
        var hit = listBox.InputHitTest(point);
        
        if (hit is Visual visual)
        {
            var control = visual.FindAncestorOfType<Border>();
            if (control is { Tag: MsuProjectWindowViewModelTreeData treeData } && treeData != _hoverValue)
            {
                _hoverValue = treeData;
                _service?.UpdateHover(treeData);
            }
        }
        else
        {
            _service?.UpdateHover(null);
        }
    }

    private void MenuButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        StyledElement? currentControl = control;
        while (currentControl is not null)
        {
            if (currentControl is Control { ContextMenu: { } contextMenu })
            {
                contextMenu.Open();
            }
            currentControl = currentControl.Parent;
        }
    }
    
    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as Control)?.Tag is not MsuProjectWindowViewModelTreeData treeData || _service == null || treeData.ChildTreeData.Count == 0)
        {
            return;
        }
        
        var songOuterPanel = this.Get<MsuSongOuterPanel>(nameof(MsuSongPanel));
        var songBasicPanel = songOuterPanel.Get<MsuSongBasicPanel>(nameof(songOuterPanel.MsuSongBasicPanel));
        var pyMusicLooperPanel = songBasicPanel.Get<PyMusicLooperPanel>(nameof(songBasicPanel.PyMusicLooperPanel));
        pyMusicLooperPanel.Stop();
        
        _service.SelectedTreeItem(treeData, false);
    }

    private void TreeLeftIconButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.Tag is not MsuProjectWindowViewModelTreeData treeData)
        {
            return;
        }

        var songOuterPanel = this.Get<MsuSongOuterPanel>(nameof(MsuSongPanel));
        var songBasicPanel = songOuterPanel.Get<MsuSongBasicPanel>(nameof(songOuterPanel.MsuSongBasicPanel));
        var pyMusicLooperPanel = songBasicPanel.Get<PyMusicLooperPanel>(nameof(songBasicPanel.PyMusicLooperPanel));
        pyMusicLooperPanel.Stop();
        
        _service?.SelectedTreeItem(treeData, true);
    }

    private void TreeItemInputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { Tag: MsuProjectWindowViewModelTreeData treeData } control)
        {
            return;
        }
        
        var point = e.GetCurrentPoint(control);
        if (point.Properties.IsLeftButtonPressed)
        {
            var summaryBorder = this.GetControl<Border>(nameof(SummaryBorder));
            MsuProjectWindowViewModelTreeData.HighlightColor = summaryBorder.Background ?? Brushes.LightSlateGray;
        
            _service?.UpdateDrag(treeData);
            _draggedTreeItem = treeData;
            control.Cursor = new Cursor(StandardCursorType.DragMove);
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            var contextMenu = control.ContextMenu;
            if (contextMenu == null)
            {
                return;
            }

            _currentContextMenu?.Close();
            _currentContextMenu = contextMenu;
            contextMenu.PlacementTarget = control;
            contextMenu.Open();
            e.Handled = true;
        }
    }

    private void TreeItemInputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_draggedTreeItem == null)
        {
            return;
        }
        
        if (sender is not Control { Tag: MsuProjectWindowViewModelTreeData } control)
        {
            return;
        }
        
        _service?.UpdateDrag(null);
        control.Cursor = Cursor.Default;
    }

    private void TreeItemInputElement_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (_viewModel?.IsDraggingItem == true) return;
        if ((sender as Control)?.Tag is not MsuProjectWindowViewModelTreeData treeData)
        {
            return;
        }

        if (_previousHoverTreeItem != null)
        {
            _previousHoverTreeItem.ShowAddButton = false;
            _previousHoverTreeItem.ShowMenuButton = false;
        }

        if (treeData.TrackInfo != null || treeData.SongInfo != null)
        {
            treeData.ShowAddButton = true;
        }

        if (treeData.SongInfo != null)
        {
            treeData.ShowMenuButton = true;
        }
        
        _previousHoverTreeItem = treeData;
    }

    private void TreeItemInputElement_OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (_viewModel?.IsDraggingItem == true) return;
        if ((sender as Control)?.Tag is not MsuProjectWindowViewModelTreeData treeData)
        {
            return;
        }

        treeData.ShowAddButton = false;
        treeData.ShowMenuButton = false;
        _previousHoverTreeItem = null;
    }

    private void DisplayIsCompleteMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.ToggleCompletedIcons();
    }

    private void DisplayHasAudioMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.ToggleHasAudioIcons();
    }

    private void DisplayCopyrightTestMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.ToggleCheckCopyrightIcons();
    }

    private void DisplayCopyrightStatusMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.ToggleCopyrightStatusIcons();
    }

    private void TreeListBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems is [MsuProjectWindowViewModelTreeData treeData])
        {
            var songOuterPanel = this.Get<MsuSongOuterPanel>(nameof(MsuSongPanel));
            var songBasicPanel = songOuterPanel.Get<MsuSongBasicPanel>(nameof(songOuterPanel.MsuSongBasicPanel));
            var pyMusicLooperPanel = songBasicPanel.Get<PyMusicLooperPanel>(nameof(songBasicPanel.PyMusicLooperPanel));
            pyMusicLooperPanel.Stop();
            
            _service?.SelectedTreeItem(treeData, false);
        }
    }

    private void MsuSongPanel_OnNewSongClicked(object? sender, EventArgs e)
    {
        AddNewSong();
    }
    
    private void MsuSongPanel_OnInputFileUpdated(object? sender, EventArgs e)
    {
        _service?.InputFileUpdated();
    }

    private void InsertSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: MsuProjectWindowViewModelTreeData treeData })
        {
            return;
        }
        
        AddNewSong(treeData);
    }

    private void TreeContextMenuBase_OnOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu { Tag: MsuProjectWindowViewModelTreeData treeData } || _viewModel == null)
        {
            return;
        }

        _viewModel.SelectedTreeItem = treeData;
        treeData.CanDelete = treeData.SongInfo != null;
    }

    private void DeleteSongMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: MsuProjectWindowViewModelTreeData treeData } || _viewModel == null)
        {
            return;
        }

        _service?.RemoveSong(treeData);
    }

    private async void CopySongMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not MenuItem { Tag: MsuProjectWindowViewModelTreeData treeData } || _viewModel == null)
            {
                return;
            }

            var songYaml = _service?.GetSongCopyDetails(treeData);
            await this.SetClipboardAsync(songYaml);
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error copying song");
            await MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this);
        }
    }
    
    private async void PasteMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not MenuItem { Tag: MsuProjectWindowViewModelTreeData treeData } || _viewModel == null)
            {
                return;
            }

            var songYaml = await this.GetClipboardAsync();
            if (string.IsNullOrEmpty(songYaml))
            {
                return;
            }

            _service?.PasteSongDetails(treeData, songYaml);
            _viewModel.MsuSongViewModel.UpdateViewModel(_viewModel.MsuProject!, treeData.TrackInfo!, treeData.SongInfo, treeData);
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error pasting song");
            await MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this);
        }
    }

    private void DuplicateSongMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: MsuProjectWindowViewModelTreeData treeData })
        {
            return;
        }
        
        AddNewSong(treeData, true);
    }

    private void Window_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.Handled && _currentContextMenu != null)
        {
            _currentContextMenu.Close();
            _currentContextMenu = null;
        }
    }

    private void TreeItemMenuBase_OnClosed(object? sender, RoutedEventArgs e)
    {
        if (Equals(sender, _currentContextMenu) && _currentContextMenu != null)
        {
            _currentContextMenu = null;
        }
    }

    private void ToggleCompleteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: MsuProjectWindowViewModelTreeData treeData })
        {
            return;
        }

        _service?.UpdateCompletedFlag(treeData);
        
        e.Handled = true;
    }

    private void ToggleCheckCopyrightButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: MsuProjectWindowViewModelTreeData treeData })
        {
            return;
        }

        _service?.UpdateCheckCopyright(treeData);
        
        e.Handled = true;
    }

    private void ToggleCopyrightSafeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: MsuProjectWindowViewModelTreeData treeData })
        {
            return;
        }

        _service?.UpdateCopyrightSafe(treeData);
        
        e.Handled = true;
    }

    private void MsuSongPanel_OnIsCompleteUpdated(object? sender, EventArgs e)
    {
        _service?.UpdateCompletedSummary();
    }

    private void FilterTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        _performTextFilter();
    }

    private void ToggleFilterTracksMissingSongsMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.ToggleFilterTracksMissingSongs();
    }
    
    private void ToggleFilterIncompleteMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.ToggleFilterIncomplete();
    }

    private void ToggleFilterMissingAudioMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.ToggleFilterMissingAudio();
    }

    private void ToggleCopyrightUntestedMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.ToggleFilterCopyrightUntested();
    }
    
    private void WindowControl_OnLoaded(object? sender, RoutedEventArgs e)
    {
        var summaryBorder = this.GetControl<Border>(nameof(SummaryBorder));
        MsuProjectWindowViewModelTreeData.HighlightColor = summaryBorder.Background ?? Brushes.LightSlateGray;
        MsuSongAdvancedPanelViewModelModelTreeData.HighlightColor = summaryBorder.Background ?? Brushes.LightSlateGray;
        _parentWindow?.Hide();

        foreach (var recentProject in _viewModel?.RecentProjects ?? [])
        {
            var menuItem = new MenuItem { Header = recentProject.ProjectName, Tag = recentProject.ProjectPath };
            menuItem.SetValue(ToolTip.TipProperty, recentProject.ProjectPath);
            menuItem.Click += (_, _) =>
            {
                CloseReason = MsuProjectWindowCloseReason.OpenProject;
                OpenProjectPath = recentProject.ProjectPath;
                Close();
            };
            BrowseMenu.Items.Add(menuItem);
        }
        
        try
        {
            var menuItem = this.Find<Button>(nameof(SaveButton))!;
            HotKeyManager.SetHotKey(menuItem, new KeyGesture(Key.S, KeyModifiers.Control));
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error setting up Ctrl-S save hot key");
        }
    }

    private void OpenAnalyzeProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.MsuProject == null) return;
        _ = OpenDialog(new AudioAnalysisWindow(_viewModel.MsuProject));
    }

    public void SaveMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.SaveProject();
    }

    private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.SaveProject();
    }

    private void GenerateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.MsuProject == null || _viewModel.IsBusy) return;
        _service?.SaveProject();
        _ = OpenDialog(new MsuGenerationWindow(_viewModel.MsuProject));
    }

    private async void CopyrightYouTubeVideoMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_service == null || _viewModel == null)
            {
                return;
            }

            if (!_service.CanCreateVideos())
            {
                await MessageWindow.ShowErrorDialog(
                    "Python companion app is not installed. Please install the app and reverify in the settings window.",
                    "Missing Companion App", this.GetTopLevelWindow());
                return;
            }
            
            var songsForVideo = _viewModel.GetSongsForVideo();

            if (songsForVideo.Count == 0)
            {
                _ = MessageWindow.ShowErrorDialog("No songs are currently selected to be added to the copyright test video", "No Songs Selected", this);
                return;
            }
        
            IStorageFolder? previousFolder;
            if (!string.IsNullOrEmpty(_viewModel!.PreviousVideoPath))
            {
                previousFolder = await StorageProvider.TryGetFolderFromPathAsync(_viewModel.PreviousVideoPath);    
            }
            else
            {
                previousFolder = await StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
            }

            var file = await CrossPlatformTools.OpenFileDialogAsync(this, FileInputControlType.SaveFile, "MP4 Video File:*.mp4",
                previousFolder?.Path.LocalPath, "Select mp4 file");

            if (string.IsNullOrEmpty(file?.Path.LocalPath))
            {
                return;
            }
        
            var videoPath = file.Path.LocalPath;
            if (!videoPath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                videoPath += ".mp4";
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var messageWindow = new MessageWindow(new MessageWindowRequest()
            {
                Message = "Creating Video",
                Title = "MSU Scripter",
                Buttons = MessageWindowButtons.Close,
                ProgressBar = MessageWindowProgressBarType.Normal,
                PrimaryButtonText = "Cancel"
            });

            _ = messageWindow.ShowDialog(this);
            _ = _service.CreateVideo(songsForVideo, videoPath, messageWindow, cancellationTokenSource.Token);
            
            messageWindow.Closing += (_, _) =>
            {
                cancellationTokenSource.Cancel();
            };
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error generating video");
            await MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this);
        }
    }
    

    private void GenerateMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.MsuProject == null) return;
        _service?.SaveProject();
        _ = OpenDialog(new MsuGenerationWindow(_viewModel.MsuProject));
    }

    private async Task OpenDialog(Window window)
    {
        if (_viewModel == null) return;
        _viewModel.IsBusy = true;
        await window.ShowDialog(this);
        _viewModel.IsBusy = false;
    }
    
    private async Task ShowError(string message)
    {
        if (_viewModel == null) return;
        _viewModel.IsBusy = true;
        await MessageWindow.ShowErrorDialog(message, "Error", parentWindow: this);
        _viewModel.IsBusy = false;
    }

    private void CreateYamlMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null || _service == null) return;
        if (!_service.CreateYamlFile(out var error))
        {
            _ = ShowError(error ?? "Failed to create YAML file(s).");
        }
    }

    private void CreateTrackListMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null || _service == null) return;
        _service.CreateTrackList();
    }

    private void CreateScriptsMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null || _service == null) return;
        if (!_service.CreateScriptFiles())
        {
            _ = ShowError("Failed to create script file(s).");
        }
    }

    private void OpenFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_viewModel?.MsuProject?.MsuPath)) return;
        CrossPlatformTools.OpenDirectory(_viewModel.MsuProject.MsuPath, true);
    }
    
    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        _service?.SaveCurrentPanel();
        if (_viewModel?.MsuProject != null && _viewModel.LastModifiedDate > _viewModel.MsuProject.LastSaveTime && !_forceClose)
        {
            e.Cancel = true;
            _ = ShowUnsavedChangesWindow();
            return;
        }
        _service?.OnClose();
        _backupTimer.Stop();
        _parentWindow?.RefreshRecentProjects();
        _parentWindow?.Show();
    }

    private async Task ShowUnsavedChangesWindow()
    {
        var messageWindow = new MessageWindow(new MessageWindowRequest()
        {
            Buttons = MessageWindowButtons.YesNoCancel,
            PrimaryButtonText = "Save and Close",
            SecondaryButtonText = "Don't Save and Close",
            TertiaryButtonText = "Cancel",
            Message = "You currently have unsaved changes. If you don't save, you may lose pending changes.",
            Title = "Unsaved Changes",
        });

        await messageWindow.ShowDialog(this);

        var result = messageWindow.DialogResult?.PressedButton ?? ButtonType.Tertiary;

        if (result == ButtonType.Tertiary)
        {
            return;
        }
        
        if (result == ButtonType.Primary)
        {
            _service?.SaveProject();
        }

        CloseReason = MsuProjectWindowCloseReason.CloseProject;
        _forceClose = true;
        Close();
    }
    
    private async Task<string?> OpenMsuProjectFilePicker(bool isSave)
    {
        var folder = await this.GetDocumentsFolderPath();
        var path = await CrossPlatformTools.OpenFileDialogAsync(this, isSave ? FileInputControlType.SaveFile : FileInputControlType.OpenFile,
            "MSU Scripter Project File:*.msup", folder);
        return path?.Path.LocalPath;
    }

    private async void BrowseMsuProjectMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var path = await OpenMsuProjectFilePicker(false);
            if (!string.IsNullOrEmpty(path))
            {
                CloseReason = MsuProjectWindowCloseReason.OpenProject;
                OpenProjectPath = path;
                Close();
            }
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error selecting msu to open");
            await MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this);
        }
    }

    private void ExitApplicationMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        CloseReason = MsuProjectWindowCloseReason.ExitApplication;
        Close();
    }

    private void CloseProjectMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        CloseReason = MsuProjectWindowCloseReason.CloseProject;
        Close();
    }

    private void NewProjectMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        CloseReason = MsuProjectWindowCloseReason.NewProject;
        Close();
    }
    
    private void BackupTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_viewModel?.MsuProject == null || _service == null)
        {
            return;
        }

        if (_service.SaveCurrentPanel())
        {
            _service.SaveProject(true);
        }
    }

    private void OpenSettingsMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Closed += (_, _) =>
        {
            _service?.LoadSettings();
        };
        settingsWindow.ShowDialog(this);
    }

    private void AddNewSong(MsuProjectWindowViewModelTreeData? treeData = null, bool duplicate = false,
        string? initialFile = null)
    {
        var songPanel = _viewModel?.DefaultSongPanel ?? DefaultSongPanel.Prompt;
        if (songPanel == DefaultSongPanel.Prompt)
        {
            _ = Dispatcher.UIThread.Invoke(async () =>
            {
                var promptWindow = new SongPanelPromptWindow();
                var response = await promptWindow.ShowDialog<SongPanelPromptWindowViewModel>(this);
                if (response.Advanced || response.Basic)
                {
                    _service?.AddNewSong(treeData, duplicate, response.Advanced, response.DontAskAgain, initialFile);
                }
            });
        }
        else
        {
            _service?.AddNewSong(treeData, duplicate, songPanel ==  DefaultSongPanel.Advanced, false, initialFile);
        }
    }

    private void ReapplyFiltersMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.FilterTree();
    }

    private void GenerateTracksJsonMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null || _service == null) return;
        var errorMessage = _service.CreateTracksJsonFile();
        if (!string.IsNullOrEmpty(errorMessage))
        {
            _ = ShowError(errorMessage);
        }
    }
}

public enum MsuProjectWindowCloseReason
{
    CloseProject,
    NewProject,
    OpenProject,
    ExitApplication
}