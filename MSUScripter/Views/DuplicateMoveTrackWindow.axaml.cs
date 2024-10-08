using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaControls.Extensions;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class DuplicateMoveTrackWindow : Window
{
    private CopyMoveTrackWindowService? _service;
    
    public DuplicateMoveTrackWindow()
    {
        InitializeComponent();
        DataContext = new CopyMoveTrackWindowViewModel().DesignerExample();
    }
    
    public DuplicateMoveTrackWindow(MsuProjectViewModel msuProjectViewModel, MsuTrackInfoViewModel trackViewModel,
        MsuSongInfoViewModel msuSongInfoViewModel, CopyMoveType type)
    {
        InitializeComponent();
        _service = this.GetControlService<CopyMoveTrackWindowService>();
        DataContext = _service?.InitializeModel(msuProjectViewModel, trackViewModel, msuSongInfoViewModel, type);
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.RunCopyMove();
        Close();
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TrackComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _service?.UpdateTrackLocations();
    }
}