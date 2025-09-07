using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using AvaloniaControls.Models;
using MSUScripter.Events;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class PyMusicLooperPanel : UserControl
{
    private readonly PyMusicLooperPanelService? _service;
    private readonly PyMusicLooperPanelViewModel _model = new();
    private MessageWindow? _runningMessageWindow;

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
                _service.RunningUpdated += (sender, currentlyRunning) =>
                {
                    if (currentlyRunning)
                    {
                        _runningMessageWindow = new MessageWindow(new MessageWindowRequest
                        {
                            Message = "Running PyMusicLooper",
                            Title = "MSU Scripter",
                            Buttons = MessageWindowButtons.Close,
                            ProgressBar = MessageWindowProgressBarType.Indeterminate,
                            PrimaryButtonText = "Cancel"
                        });

                        _runningMessageWindow.Closed += (_, _) =>
                        {
                            _service?.StopPyMusicLooper();
                        };

                        Dispatcher.UIThread.Invoke(async () =>
                        {
                            var topLevel = TopLevel.GetTopLevel(this);
                            while (topLevel?.IsVisible != true)
                            {
                                await Task.Delay(TimeSpan.FromSeconds(0.25));
                                topLevel = TopLevel.GetTopLevel(this);
                            }
                            _ = _runningMessageWindow.ShowDialog(topLevel);
                        });
                    }
                    else if (_runningMessageWindow != null)
                    {
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            _runningMessageWindow.Close();
                            _runningMessageWindow = null;
                        });
                    }
                    
                    RunningUpdated?.Invoke(sender, currentlyRunning);
                }; 
            }
        }
    }
    
    public event EventHandler<PyMusicLooperPanelUpdatedArgs>? OnUpdated;

    public event EventHandler<bool>? RunningUpdated;

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