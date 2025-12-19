using System;
using System.Threading.Tasks;
using System.Timers;
using AvaloniaControls.Services;
using Material.Icons;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

// ReSharper disable once ClassNeverInstantiated.Global
public class AudioControlService(IAudioPlayerService audioService, SettingsService settingsService) : ControlService
{
    private readonly AudioControlViewModel _model = new();
    private readonly Timer _timer = new(250);
    private Settings Settings => settingsService.Settings;

    public AudioControlViewModel InitializeModel()
    {
        _model.CanPlayMusic = audioService.CanPlayMusic;
        _model.CanChangePosition = audioService.CanSetMusicPosition;
        _model.CanChangeVolume = audioService.CanChangeVolume;
        _model.Volume = Settings.Volume * 100;
        _timer.Elapsed += TimerOnElapsed;
        
        audioService.PlayStarted += PlayStarted;
        audioService.PlayPaused += PlayPaused;
        audioService.PlayStopped += PlayStopped;

        if (audioService.IsPlaying)
        {
            StartTimer();
            _model.Icon = MaterialIconKind.Pause;
            _model.CanPlayPause = true;
        }
        else if (audioService.IsStopped)
        {
            _model.Icon = MaterialIconKind.Stop;
        }
        else if (audioService.IsPaused)
        {
            _model.Icon = MaterialIconKind.Play;
            _model.CanPlayPause = true;
        }

        return _model;
    }

    public EventHandler? OnPlayStarted;

    public Task<bool> StopAsync()
    {
        return audioService.StopSongAsync();
    }
    
    public void PlayPause()
    {
        audioService.PlayPause();
    }

    public void UpdatePosition(double position)
    {
        if (Math.Abs(position - _model.PreviousPosition) > 0.01)
        {
            _model.PreviousPosition = position;
            audioService.SetPosition(position / 100);
        }
    }
    
    public void SetSeconds()
    {
        audioService.JumpToTime(_model.JumpToSeconds ?? 0);
        if (audioService.IsPaused)
        {
            audioService.Play();
        }
    }

    public double GetCurrentVolume()
    {
        return Settings.Volume * 100;
    }

    public void UpdateVolume(double volume)
    {
        Settings.Volume = volume / 100;
        audioService.SetVolume(volume / 100);
        settingsService.SaveSettings();
    }

    public void ShutdownService()
    {
        _timer.Elapsed -= TimerOnElapsed;
        _timer.Stop();
    }

    private void PlayStopped(object? sender, EventArgs e)
    {
        _model.Icon = MaterialIconKind.Stop;
        _model.CanPlayPause = false;
        StopTimer();
    }

    private void PlayPaused(object? sender, EventArgs e)
    {
        _model.Icon = MaterialIconKind.Play;
        _model.CanPlayPause = true;
        StopTimer();
    }

    private void PlayStarted(object? sender, EventArgs e)
    {
        OnPlayStarted?.Invoke(this, EventArgs.Empty);
        _model.Icon = MaterialIconKind.Pause;
        _model.CanPlayPause = true;
        StartTimer();
    }
    
    private void StartTimer()
    {
        if (audioService.CanSetMusicPosition)
        {
            _timer.Start();
        }
    }

    private void StopTimer()
    {
        if (audioService.CanSetMusicPosition)
        {
            _timer.Stop();
        }
    }

    private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        _model.Position = _model.PreviousPosition = (audioService.GetCurrentPosition() ?? 0.0) * 100;
        var currentTime = TimeSpan.FromSeconds(audioService.GetCurrentPositionSeconds()).ToString(@"mm\:ss");
        var totalTime = TimeSpan.FromSeconds(audioService.GetLengthSeconds()).ToString(@"mm\:ss");
        _model.Timestamp = $"{currentTime}/{totalTime}";
    }
}