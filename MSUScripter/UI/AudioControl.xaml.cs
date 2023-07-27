using System;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using MSUScripter.Services;

namespace MSUScripter.UI;

public partial class AudioControl : UserControl
{
    private AudioService _audioService;
    private System.Timers.Timer _timer;
    private double _prevValue = 0;
    
    public AudioControl(AudioService audioService)
    {
        _audioService = audioService;
        
        InitializeComponent();
        
        audioService.PlayStarted += PlayStarted;
        audioService.PlayPaused += PlayPaused;
        audioService.PlayStopped += PlayStopped;

        _timer = new(1000);
        _timer.Elapsed += TimerOnElapsed;
    }

    private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        var position = _prevValue = _audioService.GetCurrentPosition() ?? 0.0;
        Dispatcher.Invoke(() => PositionSlider.Value = position * 100);
    }

    private void PlayStopped(object? sender, EventArgs e)
    {
        UpdateIcon(false, false);
    }

    private void PlayPaused(object? sender, EventArgs e)
    {
        UpdateIcon(false, true);
    }

    private void PlayStarted(object? sender, EventArgs e)
    {
        UpdateIcon(true, false);
    }

    public void UpdateIcon(bool isPlaying, bool isPaused)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => UpdateIcon(isPlaying, isPaused));
            return;
        }
        
        if (isPlaying)
        {
            IconPlay.Visibility = Visibility.Visible;
            IconPause.Visibility = Visibility.Collapsed;
            IconStop.Visibility = Visibility.Collapsed;
            PlayPauseButton.IsEnabled = true;
            PositionSlider.IsEnabled = true;
            _timer.Start();
        }
        else if (isPaused)
        {
            IconPlay.Visibility = Visibility.Collapsed;
            IconPause.Visibility = Visibility.Visible;
            IconStop.Visibility = Visibility.Collapsed;
            PlayPauseButton.IsEnabled = true;
            PositionSlider.IsEnabled = true;
            _timer.Stop();
            EditPanel.Instance?.UpdateStatusBarText("Pcm Paused");
        }
        else
        {
            IconPlay.Visibility = Visibility.Collapsed;
            IconPause.Visibility = Visibility.Collapsed;
            IconStop.Visibility = Visibility.Visible;
            PlayPauseButton.IsEnabled = false;
            PositionSlider.IsEnabled = false;
            _timer.Stop();
        }
    }

    private void PlayPauseButton_OnClick(object sender, RoutedEventArgs e)
    {
        _audioService.PlayPause();
    }

    private void PositionSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Math.Abs(PositionSlider.Value / 100.0 - _prevValue) > 0.1)
        {
            _audioService.SetPosition(PositionSlider.Value / 100.0);
        }
    }
}