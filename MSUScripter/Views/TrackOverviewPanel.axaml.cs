using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaControls.Extensions;
using MSUScripter.Models;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class TrackOverviewPanel : UserControl
{
    private TrackOverviewPanelService? _service;

    public static readonly StyledProperty<EditProjectPanelViewModel> ProjectProperty = AvaloniaProperty.Register<MsuSongInfoPanel, EditProjectPanelViewModel>(
        nameof(Project));

    public EditProjectPanelViewModel Project
    {
        get => GetValue(ProjectProperty);
        set => SetValue(ProjectProperty, value);
    }
    
    public TrackOverviewPanel()
    {
        if (Design.IsDesignMode)
        {
            DataContext = new TrackOverviewPanelViewModel().DesignerExample();
            return;
        }

        ProjectProperty.Changed.Subscribe(x =>
        {
            if (x.Sender != this || (EditProjectPanelViewModel?)x.NewValue.Value == null)
            {
                return;
            }
            _service = this.GetControlService<TrackOverviewPanelService>();
            DataContext = _service?.InitializeModel(x.NewValue.Value);    
        });
        
        InitializeComponent();
    }
    
    public event EventHandler<TrackEventArgs>? OnSelectedTrack;

    private void AudioDataGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not TextBlock && e.Source is not Border { Name: "CellBorder"})
        {
            return;
        }

        var selectedItems = this.Find<DataGrid>(nameof(TrackDataGrid))!.SelectedItems;
        if (selectedItems.Count <= 0)
        {
            return;
        }

        if (selectedItems[0] is not TrackOverviewPanelViewModel.TrackOverviewRow row)
        {
            return;
        }
        
        OnSelectedTrack?.Invoke(this, new TrackEventArgs(row.TrackNumber));
    }

    private void IsCompleteCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { Tag: TrackOverviewPanelViewModel.TrackOverviewRow row} || row.SongInfo == null )
        {
            return;
        }

        row.SongInfo.IsComplete = !row.SongInfo.IsComplete; 
        _service?.UpdateCompletedTrackDetails();

    }
}