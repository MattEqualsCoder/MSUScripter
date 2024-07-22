using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaControls.Extensions;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class MsuTrackInfoPanel : UserControl
{
    public void SetTrackInfo(MsuProjectViewModel project, MsuTrackInfoViewModel trackInfo)
    {
        trackInfo.Project = project;
        DataContext = trackInfo;
    }
    
    private readonly MsuTrackInfoPanelService? _service;
    
    private MsuTrackInfoViewModel? TrackData => DataContext as MsuTrackInfoViewModel;
    
    public MsuTrackInfoPanel()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            DataContext = new MsuSongMsuPcmInfoViewModel().DesignerExample();
        }
        else
        {
            _service = this.GetControlService<MsuTrackInfoPanelService>();
        }

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MsuTrackInfoViewModel trackInfoViewModel)
            {
                _service?.InitializeModel(trackInfoViewModel);    
            }
        };
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void AddSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.AddSong();
    }

    private void AddSongWindowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (TrackData == null) return;
        var window = new AddSongWindow(TrackData.Project, TrackData.TrackNumber, null);
        window.ShowDialog(TopLevel.GetTopLevel(this) as Window ?? App.MainWindow!);
    }

}