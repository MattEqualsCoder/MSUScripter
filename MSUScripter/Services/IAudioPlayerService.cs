using System;
using System.Threading.Tasks;

namespace MSUScripter.Services;

public interface IAudioPlayerService
{
    public void Pause();

    public void PlayPause();

    public void Play();

    public double? GetCurrentPosition();

    public double GetLengthSeconds();

    public double GetCurrentPositionSeconds();

    public void SetPosition(double value);
    
    public void JumpToTime(double seconds);

    public void SetVolume(double volume);
    
    public bool IsPlaying { get; }

    public bool IsPaused { get; }

    public bool IsStopped { get; }

    public Task<bool> PlaySongAsync(string path, bool fromEnd, bool isLoopingSong);

    public Task<bool> StopSongAsync(string? newSongPath = null, bool waitForFile = false);
    
    public EventHandler? PlayStarted { get; set; }
    public EventHandler? PlayPaused { get; set; }
    public EventHandler? PlayStopped { get; set; }
    
    public bool CanPlayMusic { get; protected set; }
    public bool CanSetMusicPosition { get; protected set; }
    public bool CanChangeVolume { get; protected set; }
    public bool CanPauseMusic { get; protected set; }
}