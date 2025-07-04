using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.Services.ControlServices;
using MSUScripter.Tools;
using MSUScripter.ViewModels;

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
    
    public MsuProjectWindow()
    {
        InitializeComponent();
        DataContext = new MsuProjectWindowViewModel().DesignerExample();
        var performSearch = () => _service?.FilterTree();
        _performTextFilter = performSearch.Debounce();
    }
    
    public MsuProjectWindow(MsuProject project)
    {
        _service = this.GetControlService<MsuProjectWindowService>();
        InitializeComponent();
        DataContext = _viewModel = _service!.InitViewModel(project);
        var performSearch = () => _service?.FilterTree();
        _performTextFilter = performSearch.Debounce(200);
    }
    
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
    
    private void TreeItemInputElement_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (_viewModel?.IsDraggingItem == true) return;
        if ((sender as Control)?.Tag is not MsuProjectWindowViewModelTreeData treeData)
        {
            return;
        }

        treeData.ShowAddButton = true;

        if (treeData.SongInfo != null)
        {
            treeData.ShowMenuButton = true;
        }
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
    }

    private async void OpenAnalyzeProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.MsuProject == null) return;
        var window = new AudioAnalysisWindow(_viewModel.MsuProject);
        await window.ShowDialog(this);
    }

    private void SaveMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.SaveProject();
    }

    private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.SaveProject();
    }

    private async void  GenerateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.MsuProject == null) return;
        var window = new MsuGenerationWindow(_viewModel.MsuProject);
        await window.ShowDialog(this);
    }
}