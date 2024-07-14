using System;
using Avalonia;
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
    
    public static readonly StyledProperty<MsuTrackInfoViewModel> TrackDataProperty = AvaloniaProperty.Register<MsuTrackInfoPanel, MsuTrackInfoViewModel>(
        "MsuPcmData");

    public MsuTrackInfoViewModel TrackData
    {
        get => GetValue(TrackDataProperty);
        set => SetValue(TrackDataProperty, value);
    }
    
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
        
        TrackDataProperty.Changed.Subscribe(x =>
        {
            if (x.Sender != this || (MsuTrackInfoViewModel?)x.NewValue.Value == null)
            {
                return;
            }

            DataContext = x.NewValue.Value;
            _service?.InitializeModel(x.NewValue.Value);
        });
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
        var window = new AddSongWindow(TrackData.Project, TrackData.TrackNumber);
        window.ShowDialog(TopLevel.GetTopLevel(this) as Window ?? App.MainWindow!);
    }

}