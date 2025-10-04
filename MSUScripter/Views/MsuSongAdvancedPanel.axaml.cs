using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Services.ControlServices;
using MSUScripter.Tools;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class MsuSongAdvancedPanel : UserControl
{
    private MsuSongAdvancedPanelViewModel? _viewModel;
    
    public MsuSongPanelService? Service { get; set; }
    
    public MsuSongAdvancedPanel()
    {
        InitializeComponent();
        if (Design.IsDesignMode)
        {
            DataContext = _viewModel = (MsuSongAdvancedPanelViewModel)new MsuSongAdvancedPanelViewModel().DesignerExample();
        }
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _viewModel = DataContext as MsuSongAdvancedPanelViewModel ?? new MsuSongAdvancedPanelViewModel();
        Service?.CheckFileErrors(_viewModel);
        _viewModel.ViewModelUpdated += (_, _) =>
        {
            Service?.CheckFileErrors(_viewModel);
        };
        _viewModel.FileDragDropped += (_, _) =>
        {
            try
            {
                if (!string.IsNullOrEmpty(_viewModel.Input) && string.IsNullOrEmpty(_viewModel.SongName) && string.IsNullOrEmpty(_viewModel.Album) && string.IsNullOrEmpty(_viewModel.ArtistName) && string.IsNullOrEmpty(_viewModel.Url))
                {
                    var metadata = Service?.GetAudioMetadata(_viewModel.Input);
                    _viewModel.SongName = metadata?.SongName;
                    _viewModel.ArtistName = metadata?.Artist;
                    _viewModel.Album = metadata?.Album;
                    _viewModel.Url = metadata?.Url;
                }
        
                _viewModel.UpdateTreeItemName();
                Service?.CheckFileErrors(_viewModel);
            }
            catch (Exception ex)
            {
                Service?.LogError(ex, "Error handling new file");
                MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this.GetTopLevelWindow());
            }
            
        };
    }
    
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_viewModel is not { IsDraggingItem: true })
        {
            return;
        }

        var listBox = this.GetControl<ListBox>(nameof(TreeListBox));
        var point = e.GetPosition(listBox);
        var hit = listBox.InputHitTest(point);
        
        if (hit is Visual visual)
        {
            var control = visual.FindAncestorOfType<Border>();
            if (control is { Tag: MsuSongAdvancedPanelViewModelModelTreeData { ParentTreeData: not null } treeData })
            {
                _viewModel.UpdateHover(treeData);
            }
        }
        else
        {
           _viewModel.UpdateHover(null);
        }
    }

    public event EventHandler? AdvancedModeToggled;
    public event EventHandler? InputFileUpdated;

    private void TreeMenuItemLeftIcon_OnClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.Tag is not MsuSongAdvancedPanelViewModelModelTreeData treeData)
        {
            return;
        }
        
        treeData.ToggleCollapsed();
    }

    private void AdvancedModeIconCheckbox_OnOnChecked(object? sender, OnIconCheckboxCheckedEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }
        _viewModel.IsAdvancedMode = true;
        AdvancedModeToggled?.Invoke(this, EventArgs.Empty);
    }

    private void TreeListBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems is not [MsuSongAdvancedPanelViewModelModelTreeData treeData])
        {
            return;
        }

        _viewModel?.SetSelectedTreeData(treeData);
    }

    private void InputFileControl_OnOnUpdated(object? sender, FileControlUpdatedEventArgs e)
    {
        try
        {
            if (_viewModel == null)
            {
                return;
            }
        
            _viewModel.SaveChanges();
        
            if (!string.IsNullOrEmpty(_viewModel.Input) && string.IsNullOrEmpty(_viewModel.SongName) && string.IsNullOrEmpty(_viewModel.Album) && string.IsNullOrEmpty(_viewModel.ArtistName) && string.IsNullOrEmpty(_viewModel.Url))
            {
                var metadata = Service?.GetAudioMetadata(_viewModel.Input);
                _viewModel.SongName = metadata?.SongName;
                _viewModel.ArtistName = metadata?.Artist;
                _viewModel.Album = metadata?.Album;
                _viewModel.Url = metadata?.Url;
            }
        
            _viewModel.UpdateTreeItemName();
            InputFileUpdated?.Invoke(this, EventArgs.Empty);
            Service?.CheckFileErrors(_viewModel);
        }
        catch (Exception ex)
        {
            Service?.LogError(ex, "Error handling new file");
            MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this.GetTopLevelWindow());
        }
    }

    private void TreeInputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            if (sender is not Control { Tag: MsuSongAdvancedPanelViewModelModelTreeData treeData } control || _viewModel is null)
            {
                return;
            }
        
            var point = e.GetCurrentPoint(control);
            if (point.Properties.IsLeftButtonPressed)
            {
                _viewModel?.UpdateDrag(treeData);
            }
            else if (point.Properties.IsRightButtonPressed)
            {
                var contextMenu = control.ContextMenu;
                if (contextMenu == null)
                {
                    return;
                }

                _viewModel.CurrentContextMenu?.Close();
                _viewModel.CurrentContextMenu = contextMenu;
                contextMenu.PlacementTarget = control;
                contextMenu.Open();
                e.Handled = true;
            }
        }
        catch (Exception ex)
        {
            Service?.LogError(ex, "Error handling click");
            MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this.GetTopLevelWindow());
        }
    }

    private void TreeInputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        try
        {
            if (_viewModel?.IsDraggingItem != true || (sender as Control)?.Tag is not MsuSongAdvancedPanelViewModelModelTreeData)
            {
                return;
            }

            if (_viewModel.UpdateDrag(null))
            {
                Service?.CheckFileErrors(_viewModel);
            }
        }
        catch (Exception ex)
        {
            Service?.LogError(ex, "Error updating drag value");
            MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this.GetTopLevelWindow());
        }
    }

    private void TreeInputElement_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (_viewModel?.IsDraggingItem == true) return;
        if ((sender as Control)?.Tag is not MsuSongAdvancedPanelViewModelModelTreeData treeData)
        {
            return;
        }

        if (treeData.ParentTreeData != null)
        {
            treeData.ShowAddButton = true;
        }
        
        treeData.ShowMenuButton = treeData.MsuPcmInfo != null;
    }

    private void TreeInputElement_OnPointerExited(object? sender, PointerEventArgs e)
    {
        if ((sender as Control)?.Tag is not MsuSongAdvancedPanelViewModelModelTreeData treeData)
        {
            return;
        }

        treeData.ShowAddButton = false;
        treeData.ShowMenuButton = false;
    }

    private void AddMsuPcmInfoButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel == null || (sender as Control)?.Tag is not MsuSongAdvancedPanelViewModelModelTreeData treeData)
            {
                return;
            }

            _viewModel.AddMsuPcmInfo(treeData);
            _viewModel.LastModifiedDate = DateTime.Now;
            Service?.CheckFileErrors(_viewModel);
        }
        catch (Exception ex)
        {
            Service?.LogError(ex, "Error adding new MsuPcm++ info");
            MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this.GetTopLevelWindow());
        }
    }

    private void TreeMenuButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        var parent = control.Parent;
        while (parent is not null)
        {
            if (parent is Border { ContextMenu: { } contextMenu })
            {
                contextMenu.Open();
            }
            parent = parent.Parent;
        }
    }

    private async void CopyMsuPcmInfoMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel == null)
            {
                return;
            }
            
            if ((sender as Control)?.Tag is not MsuSongAdvancedPanelViewModelModelTreeData treeData || treeData.MsuPcmInfo == null)
            {
                return;
            }
        
            _viewModel?.SaveChanges();
            var yaml = Service?.GetMsuPcmInfoCopyText(treeData.MsuPcmInfo);
            await this.SetClipboardAsync(yaml);
        }
        catch (Exception ex)
        {
            Service?.LogError(ex, "Error copying MsuPcm++ info to clipboard");
            await MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this.GetTopLevelWindow());
        }
    }

    private async void PasteMsuPcmInfoMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel == null)
            {
                return;
            }
            
            if ((sender as Control)?.Tag is not MsuSongAdvancedPanelViewModelModelTreeData treeData || treeData.MsuPcmInfo == null)
            {
                return;
            }
            
            _viewModel.SaveChanges();

            var clipboardText = await this.GetClipboardAsync();
            if (string.IsNullOrEmpty(clipboardText)) return;
            
            var msuPcmInfoFromClipboard = Service?.GetMsuPcmInfoFromText(clipboardText);
            if (msuPcmInfoFromClipboard == null)
            {
                await MessageWindow.ShowErrorDialog(
                    "Invalid MsuPcm++ YAML. Please check your formatting before trying again.", "Invalid Format",
                    TopLevel.GetTopLevel(this) as Window ?? App.MainWindow);
                return;
            }
            
            if (treeData.MsuPcmInfo != null && treeData.MsuPcmInfo.HasData())
            {
                var typeName = treeData.ParentTreeData == null ? "song's"
                    : treeData.IsSubChannel ? "sub channel's" : "sub track's";
                var response = await MessageWindow.ShowYesNoDialog(
                    $"Do you want to override this {typeName} data? This action cannot be undone.",
                    "Override Data?", TopLevel.GetTopLevel(this) as Window ?? App.MainWindow);
                if (!response)
                {
                    return;
                }
            }
                    
            _viewModel.ReplaceMsuPcmInfo(treeData, msuPcmInfoFromClipboard);
            Service?.CheckFileErrors(_viewModel);
        }
        catch (Exception ex)
        {
            Service?.LogError(ex, "Error pasting MsuPcm++ info from clipboard");
            await MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this.GetTopLevelWindow());
        }
    }
    
    private void DuplicateMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.Tag is not MsuSongAdvancedPanelViewModelModelTreeData treeData || treeData.MsuPcmInfo == null || Service == null || _viewModel == null)
        {
            return;
        }

        var msuPcmInfo = Service.DuplicateMsuPcmInfo(treeData.MsuPcmInfo);
        if (msuPcmInfo == null)
        {
            return;
        }
        
        var newTreeData = _viewModel.AddMsuPcmInfo(treeData);
        _viewModel.ReplaceMsuPcmInfo(newTreeData, msuPcmInfo);
        Service?.CheckFileErrors(_viewModel);
    }

    private void TreeItemMenuBase_OnOpened(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.Tag is not MsuSongAdvancedPanelViewModelModelTreeData treeData || treeData.MsuPcmInfo == null || _viewModel == null)
        {
            return;
        }
        
        _viewModel.SelectedTreeItem = treeData;
        treeData.EnableMenuItems = treeData.MsuPcmInfo != null;
    }

    private async void DeleteMsuPcmInfoMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel == null)
            {
                return;
            }
            
            if ((sender as Control)?.Tag is not MsuSongAdvancedPanelViewModelModelTreeData treeData ||
                treeData.MsuPcmInfo == null)
            {
                return;
            }

            if (treeData.MsuPcmInfo != null && treeData.MsuPcmInfo.HasData())
            {
                var typeName = treeData.ParentTreeData == null ? "song's"
                    : treeData.IsSubChannel ? "sub channel's" : "sub track's";
                var response = await MessageWindow.ShowYesNoDialog(
                    $"Do you want to delete this {typeName} data? This action cannot be undone.",
                    "Override Data?", TopLevel.GetTopLevel(this) as Window ?? App.MainWindow);
                if (!response)
                {
                    return;
                }
            }

            _viewModel.RemoveMsuPcmInfo(treeData);
            _viewModel.LastModifiedDate = DateTime.Now;
            Service?.CheckFileErrors(_viewModel);
        }
        catch (Exception ex)
        {
            Service?.LogError(ex, "Error deleting MsuPcm++ info");
            await MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this.GetTopLevelWindow());
        }
    }

    private async void OpenPyMusicLooperWindowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel == null || Service == null)
            {
                return;
            }

            if (!Service.CanRunPyMusicLooper())
            {
                await MessageWindow.ShowErrorDialog(
                    "Python companion app is not installed. Please install the app and reverify in the settings window.",
                    "Missing Companion App", this.GetTopLevelWindow());
                return;
            }
        
            var window = new PyMusicLooperWindow();
            window.UpdateDetails(new PyMusicLooperDetails
            {
                Project = _viewModel.Project,
                FilePath = _viewModel.Input,
                Normalization = _viewModel.Normalization,
                FilterStart = _viewModel.TrimStart,
                ForceRun = true
            
            });
            var loopResult = await window.ShowPyMusicLooperWindowDialog(TopLevel.GetTopLevel(this) as Window);
            if (loopResult != null)
            {
                _viewModel.Loop = loopResult.LoopStart;
                _viewModel.TrimEnd = loopResult.LoopEnd;
            }
        }
        catch (Exception ex)
        {
            Service?.LogError(ex, "Error opening PyMusicLooper window");
            await MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this.GetTopLevelWindow());
        }
    }

    private void DetectStartingSamplesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel == null || Service == null) return;
            var samples = Service.DetectStartingSamples(_viewModel.Input ?? "");
            if (samples >= 0)
            {
                _viewModel.TrimStart = samples;
            }
            else
            {
                _ = MessageWindow.ShowErrorDialog("Failed to capture starting samples", "Error", this.GetTopLevelWindow());
            }
        }
        catch
        {
            // Do nothing
        }
    }
    
    private void DetectEndingSamplesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel == null || Service == null) return;
            var samples = Service.DetectEndingSamples(_viewModel.Input ?? "");
            if (samples >= 0)
            {
                _viewModel.TrimEnd = samples;
            }
            else
            {
                _ = MessageWindow.ShowErrorDialog("Failed to capture ending samples", "Error", this.GetTopLevelWindow());
            }
        }
        catch
        {
            // Do nothing
        }
    }
}