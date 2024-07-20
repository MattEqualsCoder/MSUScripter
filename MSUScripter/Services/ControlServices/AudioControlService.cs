using System;
using System.Threading.Tasks;
using System.Timers;
using AvaloniaControls.ControlServices;
using Material.Icons;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

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
        
        return _model;
    }

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
        audioService.SetPosition(position / 100);
    }
    
    public void SetSeconds()
    {
        audioService.JumpToTime(_model.JumpToSeconds ?? 0);
    }

    public void UpdateVolume(double volume)
    {
        Settings.Volume = volume / 100;
        audioService.SetVolume(volume / 100);
        settingsService.SaveSettings();
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
        _model.Position = (audioService.GetCurrentPosition() ?? 0.0) * 100;
        var currentTime = TimeSpan.FromSeconds(audioService.GetCurrentPositionSeconds()).ToString(@"mm\:ss");
        var totalTime = TimeSpan.FromSeconds(audioService.GetLengthSeconds()).ToString(@"mm\:ss");
        _model.Timestamp = $"{currentTime}/{totalTime}";
    }
}