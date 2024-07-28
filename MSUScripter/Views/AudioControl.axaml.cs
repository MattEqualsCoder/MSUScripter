﻿using System.Threading.Tasks;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
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
            DataContext = _model = _service?.InitializeModel() ?? new AudioControlViewModel();
        }

        CanPopoutProperty.Changed.Subscribe(x =>
        {
            if (x.Sender != this) return;
            _model.CanPopout = OperatingSystem.IsWindows() && x.NewValue is { HasValue: true, Value: true };
        });
        
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
    
    public static readonly StyledProperty<bool> CanPopoutProperty = AvaloniaProperty.Register<AudioControl, bool>(
        nameof(CanPopout));

    public bool CanPopout
    {
        get => GetValue(CanPopoutProperty);
        set => SetValue(CanPopoutProperty, value);
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

    private void PopoutButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var audioPlayerWindow = new AudioPlayerWindow();
        _model.CanPressPopoutButton = false;
        audioPlayerWindow.Closed += (o, args) =>
        {
            _model.CanPressPopoutButton = true;
        };
        audioPlayerWindow.Show(App.MainWindow);
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