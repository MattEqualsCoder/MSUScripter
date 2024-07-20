using System;
using System.Threading.Tasks;

namespace MSUScripter.Services;

public interface IAudioPlayerService
{
    public static bool CanPlaySongs { get; protected set; }
    
    public string CurrentPlayingFile { get; protected set; }

    public void Pause();

    public void PlayPause();

    public void Play();

    public double? GetCurrentPosition();

    public double GetLengthSeconds();

    public double GetCurrentPositionSeconds();

    public void SetPosition(double value);
    
    public void JumpToTime(double seconds);

    public void SetVolume(double volume);
    
    public Task<bool> PlaySongAsync(string path, bool fromEnd);

    public Task<bool> StopSongAsync(string? newSongPath = null, bool waitForFile = false);
    
    public EventHandler? PlayStarted { get; set; }
    public EventHandler? PlayPaused { get; set; }
    public EventHandler? PlayStopped { get; set; }
    
    public bool CanPlayMusic { get; protected set; }
    public bool CanSetMusicPosition { get; protected set; }
    public bool CanChangeVolume { get; protected set; }
}