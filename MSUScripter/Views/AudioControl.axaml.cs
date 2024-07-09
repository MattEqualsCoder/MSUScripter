using System.Timers;
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

    public AudioControl()
    {
        if (Design.IsDesignMode)
        {
            DataContext = new AudioControlViewModel().DesignerExample();
        }
        else
        {
            _service = this.GetControlService<AudioControlService>();
            DataContext = _service?.InitializeModel();
        }
        
        InitializeComponent();
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
        _service?.UpdateVolume(e.NewValue);
    }
}