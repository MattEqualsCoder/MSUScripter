using System.Threading.Tasks;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaControls.Extensions;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class AudioControl : UserControl
{
    private readonly AudioControlService? _service;
    private readonly AudioControlViewModel _model;

    public AudioControl()
    {
        if (Design.IsDesignMode)
        {
            DataContext = _model = (AudioControlViewModel)new AudioControlViewModel().DesignerExample();
        }
        else
        {
            _service = this.GetControlService<AudioControlService>();

            if (_service != null)
            {
                _service.OnPlayStarted += async void (_, _) =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(0.1));
                        Dispatcher.UIThread.Post(() =>
                        {
                            this.GetControl<Button>(nameof(PlayPauseButton)).Focus();
                        });
                    }
                    catch
                    {
                        // Do nothing
                    }
                };
            }
            DataContext = _model = _service?.InitializeModel() ?? new AudioControlViewModel();
        }

        CanSetTimeSecondsProperty.Changed.Subscribe(x =>
        {
            if (x.Sender != this) return;
            _model.CanSetTimeSeconds = x.NewValue is { HasValue: true, Value: true };
        });
        
        InitializeComponent();
    }

    public Task<bool> StopAsync()
    {
        return _service?.StopAsync() ?? Task.FromResult(true);
    }
    
    public static readonly StyledProperty<bool> CanSetTimeSecondsProperty = AvaloniaProperty.Register<AudioControl, bool>(
        nameof(CanSetTimeSeconds));

    public bool CanSetTimeSeconds
    {
        get => GetValue(CanSetTimeSecondsProperty);
        set => SetValue(CanSetTimeSecondsProperty, value);
    }
    
    private void PlayPauseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.PlayPause();
    }

    private void PositionSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        _service?.UpdatePosition(e.NewValue);
    }

    private void VolumeSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider || !slider.IsLoaded) return;        
        _service?.UpdateVolume(e.NewValue);
    }

    private void JumpToSecondsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.SetSeconds();
    }

    private void VolumeSlider_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_service == null || sender is not Slider slider) return;
        slider.Value = _service.GetCurrentVolume();
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _service?.ShutdownService();
    }
}