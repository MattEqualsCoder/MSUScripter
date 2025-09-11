using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using AvaloniaControls.Services;
using MSUScripter.Services.ControlServices;
using MSUScripter.Tools;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class MsuSongOuterPanel : UserControl
{
    private MsuSongOuterPanelViewModel? _viewModel;
    private MsuSongPanelService? _service;
    
    public MsuSongOuterPanel()
    {
        InitializeComponent();
        if (Design.IsDesignMode)
        {
            DataContext = _viewModel = (MsuSongOuterPanelViewModel)new MsuSongOuterPanelViewModel().DesignerExample();
        }
        else
        {
            _service = this.GetControlService<MsuSongPanelService>();
        }
    }

    public event EventHandler? NewSongClicked;

    public event EventHandler? IsCompleteUpdated;

    public event EventHandler? InputFileUpdated;
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _viewModel = DataContext as MsuSongOuterPanelViewModel ?? new MsuSongOuterPanelViewModel();
        this.GetControl<MsuSongBasicPanel>(nameof(MsuSongBasicPanel)).Service = _service;
        this.GetControl<MsuSongAdvancedPanel>(nameof(MsuSongAdvancedPanel)).Service = _service;
    }

    private void ToggleButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }

    private void MsuSongBasicPanel_OnAdvancedModeToggled(object? sender, EventArgs e)
    {
        if (_viewModel?.SongInfo is null) return;
        _viewModel.SongInfo.DisplayAdvancedMode = true;
        _viewModel.BasicPanelViewModel.SaveChanges();
        _viewModel.BasicPanelViewModel.IsEnabled = false;
        _viewModel.AdvancedPanelViewModel.UpdateViewModel(_viewModel.Project!, _viewModel.TrackInfo!, _viewModel.SongInfo!, _viewModel.TreeData!);
    }

    private void MsuSongAdvancedPanel_OnAdvancedModeToggled(object? sender, EventArgs e)
    {
        if (_viewModel?.SongInfo is null) return;
        _viewModel.SongInfo.DisplayAdvancedMode = false;
        _viewModel.AdvancedPanelViewModel.SaveChanges();
        _viewModel.AdvancedPanelViewModel.IsEnabled = false;
        _viewModel.BasicPanelViewModel.UpdateViewModel(_viewModel.Project!, _viewModel.TrackInfo!, _viewModel.SongInfo!, _viewModel.TreeData!);
    }

    private void AddSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        NewSongClicked?.Invoke(this, EventArgs.Empty);
    }

    private void IconCheckbox_OnOnChecked(object? sender, OnIconCheckboxCheckedEventArgs e)
    {
        IsCompleteUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void MsuSongAdvancedPanel_OnInputFileChanged(object? sender, EventArgs e)
    {
        InputFileUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void MsuSongBasicPanel_OnInputFileUpdated(object? sender, EventArgs e)
    {
        InputFileUpdated?.Invoke(this, EventArgs.Empty);
    }

    private async void PlaySongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel?.Project == null || _viewModel?.SongInfo == null || _service == null)
            {
                return;
            }
            
            _viewModel.SaveChanges();
            var message = await _service.PlaySong(_viewModel.Project, _viewModel.SongInfo, false);
            if (!string.IsNullOrEmpty(message))
            {
                _ = MessageWindow.ShowErrorDialog(message, "Error",
                    TopLevel.GetTopLevel(this) as Window);
            }
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error playing song");
            await MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this.GetTopLevelWindow());
        }
    }

    private async void TestLoopButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel?.Project == null || _viewModel?.SongInfo == null || _service == null)
            {
                return;
            }
            _viewModel.SaveChanges();
            var message = await _service.PlaySong(_viewModel.Project, _viewModel.SongInfo, true);
            if (!string.IsNullOrEmpty(message))
            {
                _ = MessageWindow.ShowErrorDialog(message, "Error",
                    TopLevel.GetTopLevel(this) as Window);
            }
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error playing song");
            await MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this.GetTopLevelWindow());
        }
    }

    private async void GeneratePcmSplitButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel?.Project == null || _viewModel?.SongInfo == null || _service == null)
            {
                return;
            }
            
            _viewModel.SaveChanges();
            var response = await _service.GeneratePcm(_viewModel.Project, _viewModel.SongInfo, false, false);
            if (!response.Successful)
            {
                _ = MessageWindow.ShowErrorDialog(response.Message ?? "Could not generate PCM file", "Error",
                    TopLevel.GetTopLevel(this) as Window);
            }
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error generating PCM file");
            await MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this.GetTopLevelWindow());
        }
    }

    private async void GenerateBlankPcmMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel?.Project == null || _viewModel?.SongInfo == null || _service == null)
            {
                return;
            }
            
            _viewModel.SaveChanges();
            var response = await _service.GeneratePcm(_viewModel.Project, _viewModel.SongInfo, false, true);
            if (!response.Successful)
            {
                _ = MessageWindow.ShowErrorDialog(response.Message ?? "Could not generate PCM file", "Error",
                    TopLevel.GetTopLevel(this) as Window);
            }
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error generating PCM file");
            await MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this.GetTopLevelWindow());
        }
    }

    private async void GeneratePrimaryPcmMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel?.Project == null || _viewModel?.SongInfo == null || _service == null)
            {
                return;
            }
            
            _viewModel.SaveChanges();
            var response = await _service.GeneratePcm(_viewModel.Project, _viewModel.SongInfo, true, false);
            if (!response.Successful)
            {
                _ = MessageWindow.ShowErrorDialog(response.Message ?? "Could not generate PCM file", "Error",
                    TopLevel.GetTopLevel(this) as Window);
            }
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error generating PCM file");
            await MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this.GetTopLevelWindow());
        }
    }

    private async void TestAudioLevelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel?.Project == null || _viewModel?.SongInfo == null || _service == null)
            {
                return;
            }

            _viewModel.SaveChanges();

            _viewModel.AverageAudioLevel = "Running audio analysis";
            _viewModel.PeakAudioLevel = "";
            _viewModel.DisplaySecondAudioLine = false;

            var output = await _service.AnalyzeAudio(_viewModel.Project, _viewModel.SongInfo);

            if (output is { AvgDecibels: not null, MaxDecibels: not null })
            {
                _viewModel.AverageAudioLevel = $"Average: {Math.Round(output.AvgDecibels.Value, 2)}db";
                _viewModel.PeakAudioLevel = $"Peak: {Math.Round(output.MaxDecibels.Value, 2)}db";
                _viewModel.DisplaySecondAudioLine = true;
            }
            else if (output is null)
            {
                _viewModel.AverageAudioLevel = "Could not generate audio file";
                _viewModel.PeakAudioLevel = "";
                _viewModel.DisplaySecondAudioLine = false;
            }
            else
            {
                _viewModel.AverageAudioLevel = "Error analyzing audio";
                _viewModel.PeakAudioLevel = "";
                _viewModel.DisplaySecondAudioLine = false;
            }
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error testing audio levels");
            await MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this.GetTopLevelWindow());
        }
    }
}