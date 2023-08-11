using System;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Material.Icons.Avalonia;
using MSUScripter.Configs;
using MSUScripter.Services;

namespace MSUScripter.Controls;

public partial class AudioControl : UserControl
{
    private readonly AudioService? _audioService;
    private readonly SettingsService? _settingsService;
    private readonly Timer _timer;
    private readonly Settings? _settings;
    private double _prevValue;

    public AudioControl() : this(null, null, null)
    {
        
    }
    
    public AudioControl(AudioService? audioService, SettingsService? settingsService, Settings? settings)
    {
        _audioService = audioService;
        _settingsService = settingsService;
        _settings = settings;

        InitializeComponent();
        
        _timer = new Timer(250);
        _timer.Elapsed += TimerOnElapsed;

        if (audioService == null) return;
        
        audioService.PlayStarted += PlayStarted;
        audioService.PlayPaused += PlayPaused;
        audioService.PlayStopped += PlayStopped;
    }
    
    private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_audioService == null) return;
        var position = _prevValue = _audioService.GetCurrentPosition() ?? 0.0;
        Dispatcher.UIThread.Invoke(() => this.Find<Slider>(nameof(PositionSlider))!.Value = position * 100);
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
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(() => UpdateIcon(isPlaying, isPaused));
            return;
        }
        
        if (isPlaying)
        {
            this.Find<MaterialIcon>(nameof(IconPlay))!.IsVisible = true;
            this.Find<MaterialIcon>(nameof(IconPause))!.IsVisible = false;
            this.Find<MaterialIcon>(nameof(IconStop))!.IsVisible = false;
            this.Find<Button>(nameof(PlayPauseButton))!.IsEnabled = true;
            this.Find<Slider>(nameof(PositionSlider))!.IsEnabled = true;
            _timer.Start();
        }
        else if (isPaused)
        {
            this.Find<MaterialIcon>(nameof(IconPlay))!.IsVisible = false;
            this.Find<MaterialIcon>(nameof(IconPause))!.IsVisible = true;
            this.Find<MaterialIcon>(nameof(IconStop))!.IsVisible = false;
            this.Find<Button>(nameof(PlayPauseButton))!.IsEnabled = true;
            this.Find<Slider>(nameof(PositionSlider))!.IsEnabled = true;
            _timer.Stop();
        }
        else
        {
            this.Find<MaterialIcon>(nameof(IconPlay))!.IsVisible = false;
            this.Find<MaterialIcon>(nameof(IconPause))!.IsVisible = false;
            this.Find<MaterialIcon>(nameof(IconStop))!.IsVisible = true;
            this.Find<Button>(nameof(PlayPauseButton))!.IsEnabled = false;
            this.Find<Slider>(nameof(PositionSlider))!.IsEnabled = false;
            _timer.Stop();
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        this.Find<Slider>(nameof(VolumeSlider))!.Value = (_settings?.Volume ?? 0) * 100;
    }

    private void PlayPauseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_audioService == null) return;
        _audioService.PlayPause();
    }

    private void PositionSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_audioService == null) return;
        var positionSlider = this.Find<Slider>(nameof(PositionSlider))!;
        if (Math.Abs(positionSlider.Value / 100.0 - _prevValue) > 0.1)
        {
            _audioService.SetPosition(positionSlider.Value / 100.0);
        }
    }

    private void VolumeSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_audioService == null || _settingsService == null || _settings == null) return;
        var volumeSlider = this.Find<Slider>(nameof(VolumeSlider))!;
        if (Math.Abs(volumeSlider.Value / 100.0 - _settings?.Volume ?? 0) > 0.1)
        {
            _settings!.Volume = volumeSlider.Value / 100;
            _audioService.SetVolume(_settings.Volume);
            try
            {
                _settingsService.SaveSettings();
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}