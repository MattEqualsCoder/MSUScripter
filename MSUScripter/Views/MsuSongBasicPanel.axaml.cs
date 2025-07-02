using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaControls;
using MSUScripter.Events;
using MSUScripter.Services.ControlServices;
using MSUScripter.Text;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class MsuSongBasicPanel : UserControl
{
    private MsuSongBasicPanelViewModel? _viewModel;
    
    public MsuSongPanelService? Service { get; set; }
    
    public MsuSongBasicPanel()
    {
        InitializeComponent();
        if (Design.IsDesignMode)
        {
            DataContext = _viewModel = (MsuSongBasicPanelViewModel)new MsuSongBasicPanelViewModel().DesignerExample();
        }
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _viewModel = DataContext as MsuSongBasicPanelViewModel ?? new MsuSongBasicPanelViewModel();
        _viewModel.ViewModelUpdated += (sender, args) =>
        {
            PyMusicLooperPanel.UpdateDetails(new PyMusicLooperDetails
            {
                FilePath = _viewModel.InputFilePath ?? "",
                FilterStart = _viewModel.TrimStart,
                Project = _viewModel.Project!,
                AllowRunByDefault = false
            });
        };
        PyMusicLooperPanel.UpdateDetails(new PyMusicLooperDetails
        {
            FilePath = _viewModel.InputFilePath ?? "",
            FilterStart = _viewModel.TrimStart,
            Project = _viewModel.Project!,
            AllowRunByDefault = false
        });
    }
    
    public event EventHandler? AdvancedModeToggled;
    public event EventHandler? InputFileUpdated;
    
    private void TrimStartNumericUpDown_OnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        PyMusicLooperPanel.UpdateFilterStart((int)(e.NewValue ?? 0));
    }

    private void AutoPyMusicLooperCheckbox_OnOnChecked(object? sender, OnIconCheckboxCheckedEventArgs e)
    {
    }

    private void PyMusicLooperPanel_OnOnUpdated(object? sender, PyMusicLooperPanelUpdatedArgs e)
    {
        if (_viewModel == null || e.Result?.IsSelected != true)
        {
            return;
        }
        
        _viewModel.LoopPoint = e.Result?.LoopStart;
        _viewModel.TrimEnd = e.Result?.LoopEnd;
    }

    private void AdvancedModeCheckbox_OnOnChecked(object? sender, OnIconCheckboxCheckedEventArgs e)
    {
        if (_viewModel == null) return;
        _viewModel.IsAdvancedMode = false;
        AdvancedModeToggled?.Invoke(this, EventArgs.Empty);
    }

    private void InputFileControl_OnOnUpdated(object? sender, FileControlUpdatedEventArgs e)
    {
        if (_viewModel?.InputFilePath == null || _viewModel.Project == null) return;
        
        _viewModel.SaveChanges();
        
        if (!string.IsNullOrEmpty(_viewModel.InputFilePath) && string.IsNullOrEmpty(_viewModel.SongName) && string.IsNullOrEmpty(_viewModel.Album) && string.IsNullOrEmpty(_viewModel.ArtistName) && string.IsNullOrEmpty(_viewModel.Url))
        {
            var metadata = Service?.GetAudioMetadata(_viewModel.InputFilePath);
            _viewModel.SongName = metadata?.SongName;
            _viewModel.ArtistName = metadata?.Artist;
            _viewModel.Album = metadata?.Album;
            _viewModel.Url = metadata?.Url;
        }
        
        PyMusicLooperPanel.UpdateDetails(new PyMusicLooperDetails
        {
            FilePath = _viewModel.InputFilePath ?? "",
            FilterStart = _viewModel.TrimStart,
            Project = _viewModel.Project,
            AllowRunByDefault = true
        });
        
        InputFileUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void PyMusicLooperPanel_OnRunningUpdated(object? sender, bool e)
    {
        if (_viewModel == null) return;
        _viewModel.PyMusicLooperRunning = !e;
    }
}