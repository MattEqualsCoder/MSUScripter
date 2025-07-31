using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using AvaloniaControls.Services;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.Services.ControlServices;
using MSUScripter.Tools;
using MSUScripter.ViewModels;
using FileInputControlType = AvaloniaControls.FileInputControlType;

namespace MSUScripter.Views;

public partial class MsuProjectWindow : RestorableWindow
{
    private readonly MsuProjectWindowViewModel? _viewModel;
    private readonly MsuProjectWindowService? _service;
    private readonly Action _performTextFilter;
    protected override string RestoreFilePath => Path.Combine(Directories.BaseFolder, "Windows", "project-window.json");
    protected override int DefaultWidth => 1280;
    protected override int DefaultHeight => 800;
    private ContextMenu? _currentContextMenu;
    private MsuProjectWindowViewModelTreeData? _draggedTreeItem;
    private MsuProjectWindowViewModelTreeData? _hoverValue;
    private MainWindow? _parentWindow;
    
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
        
        _service.SelectedTreeItem(treeData);
    }

    private void TreeLeftIconButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.Tag is not MsuProjectWindowViewModelTreeData treeData)
        {
            return;
        }
        
        _service?.SelectedTreeItem(treeData);
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
        
        if ((sender as Control)?.Tag is not MsuProjectWindowViewModelTreeData)
        {
            return;
        }
        
        _service?.UpdateDrag(null);
    }

    private MsuProjectWindowViewModelTreeData? previousHoverTreeItem;
    
    private void TreeItemInputElement_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (_viewModel?.IsDraggingItem == true) return;
        if ((sender as Control)?.Tag is not MsuProjectWindowViewModelTreeData treeData)
        {
            return;
        }

        if (previousHoverTreeItem != null)
        {
            previousHoverTreeItem.ShowAddButton = false;
            previousHoverTreeItem.ShowMenuButton = false;
        }

        treeData.ShowAddButton = true;

        if (treeData.SongInfo != null)
        {
            treeData.ShowMenuButton = true;
        }
        
        previousHoverTreeItem = treeData;
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
        previousHoverTreeItem = null;
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
        if (e.AddedItems is [MsuProjectWindowViewModelTreeData { ChildTreeData.Count: 0 } treeData])
        {
            _service?.SelectedTreeItem(treeData);
        }
    }

    private void MsuSongPanel_OnNewSongClicked(object? sender, EventArgs e)
    {
        _service?.AddNewSong();
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
        
        _service?.AddNewSong(treeData);
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
        catch
        {
            // Do nothing
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
        catch
        {
            // Do nothing
        }
    }

    private void DuplicateSongMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: MsuProjectWindowViewModelTreeData treeData })
        {
            return;
        }
        
        _service?.AddNewSong(treeData, true);
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
        
    }

    private void OpenAnalyzeProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.MsuProject == null) return;
        _ = OpenDialog(new AudioAnalysisWindow(_viewModel.MsuProject));
    }

    private void SaveMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.SaveProject();
    }

    private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.SaveProject();
    }

    private void GenerateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.MsuProject == null) return;
        _ = OpenDialog(new MsuGenerationWindow(_viewModel.MsuProject));
    }

    private void CopyrightYouTubeVideoMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.MsuProject == null) return;
        _ = OpenDialog(new VideoCreatorWindow(_viewModel.MsuProject));
    }

    private void GenerateMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.MsuProject == null) return;
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
        await MessageWindow.ShowErrorDialog(message, parentWindow: this);
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
        _parentWindow?.Show();
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
        catch (Exception exception)
        {
            Console.WriteLine(exception);
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
}

public enum MsuProjectWindowCloseReason
{
    CloseProject,
    NewProject,
    OpenProject,
    ExitApplication
}