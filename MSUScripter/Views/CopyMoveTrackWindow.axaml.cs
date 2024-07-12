using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaControls.Extensions;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class CopyMoveTrackWindow : Window
{
    private CopyMoveTrackWindowService? _service;
    
    public CopyMoveTrackWindow()
    {
        InitializeComponent();
        DataContext = new CopyMoveTrackWindowViewModel().DesignerExample();
    }
    
    public CopyMoveTrackWindow(MsuProjectViewModel msuProjectViewModel, MsuTrackInfoViewModel trackViewModel,
        MsuSongInfoViewModel msuSongInfoViewModel, bool isMove)
    {
        InitializeComponent();
        _service = this.GetControlService<CopyMoveTrackWindowService>();
        DataContext = _service?.InitializeModel(msuProjectViewModel, trackViewModel, msuSongInfoViewModel, isMove);
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
}