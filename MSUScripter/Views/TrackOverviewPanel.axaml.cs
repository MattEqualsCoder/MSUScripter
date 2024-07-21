using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaControls.Extensions;
using AvaloniaControls.Services;
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

        IsVisibleProperty.Changed.Subscribe(x =>
        {
            if (x.Sender != this || x.NewValue.Value != true) return;
            _service?.RefreshTracks();
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
        if (sender is not CheckBox { Tag: TrackOverviewPanelViewModel.TrackOverviewRow row } || row.SongInfo == null)
        {
            return;
        }

        ITaskService.Run(async () =>
        {
            await Task.Delay(100);
            _service?.UpdateCompletedTrackDetails();
        });
        
    }

    private async void OpenAddSongWindowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TrackOverviewPanelViewModel.TrackOverviewRow row } || _service == null) return;
        var window = new AddSongWindow(_service.GetProject(), row.TrackNumber);
        await window.ShowDialog<MsuSongInfoViewModel?>(TopLevel.GetTopLevel(this) as Window ?? App.MainWindow);
        _service?.RefreshTracks();
    }

    public void Refresh()
    {
        _service?.RefreshTracks();
    }
}