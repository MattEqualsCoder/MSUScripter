using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaControls;
using AvaloniaControls.Extensions;
using MSUScripter.Events;
using MSUScripter.Services.ControlServices;
using MSUScripter.Text;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class PyMusicLooperPanel : UserControl
{
    private readonly PyMusicLooperPanelService? _service;
    private readonly PyMusicLooperPanelViewModel _model = new();

    public PyMusicLooperPanel()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            DataContext = new PyMusicLooperPanelViewModel().DesignerExample();
        }
        else
        {
            _service = this.GetControlService<PyMusicLooperPanelService>();
            DataContext = _model = _service?.InitializeModel() ?? new PyMusicLooperPanelViewModel();

            if (_service != null)
            {
                _service.OnUpdated += (sender, args) => OnUpdated?.Invoke(sender, args);
                _service.RunningUpdated += (sender, b) => RunningUpdated?.Invoke(sender, b); 
            }
        }
    }
    
    public event EventHandler<PyMusicLooperPanelUpdatedArgs>? OnUpdated;

    public event EventHandler<bool> RunningUpdated;

    public void UpdateDetails(PyMusicLooperDetails details)
    {
        _service?.UpdateDetails(details);
    }
    
    public void UpdateFilterStart(int? filterStart)
    {
        _service?.UpdateFilterStart(filterStart);
    }

    public void Stop()
    {
        _service?.StopPyMusicLooper();
    }

    public PyMusicLooperResultViewModel? SelectedResult => _model.SelectedResult;
    
    private void NextPageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = _service?.ChangePage(1);
    }

    private void PrevPageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = _service?.ChangePage(-1);
    }

    private void PlaySongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.Tag is not PyMusicLooperResultViewModel result) return;
        _service?.PlayResult(result);
    }

    private void SelectedRadioButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton button) return;
        if (button.Tag is not PyMusicLooperResultViewModel result) return;
        _service?.SelectResult(result);
    }

    private void RunPyMusicLooperButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.RunPyMusicLooper();
    }
    
    private void GitHubLink_OnClick(object? sender, RoutedEventArgs e)
    {
        CrossPlatformTools.OpenUrl("https://github.com/arkrow/PyMusicLooper");
    }

    private void StopPyMusicLooperButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.StopPyMusicLooper();
    }

    private void AutoRunIconCheckbox_OnOnChecked(object? sender, OnIconCheckboxCheckedEventArgs e)
    {
        _service?.SaveAutoRun(e.Value);
    }
}