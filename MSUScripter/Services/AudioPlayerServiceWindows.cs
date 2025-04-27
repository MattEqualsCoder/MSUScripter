using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaControls.Services;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using NAudio.Wave;

namespace MSUScripter.Services;

public class AudioPlayerServiceWindows : IAudioPlayerService
{
    private WaveOutEvent? _waveOutEvent;
    private LoopStream? _loopStream;
    private readonly ILogger<AudioPlayerServiceWindows> _logger;
    private readonly Settings _settings;

    public AudioPlayerServiceWindows(ILogger<AudioPlayerServiceWindows> logger, Settings settings)
    {
        _logger = logger;
        _settings = settings;
        IAudioPlayerService.CanPlaySongs = true;
    }

    public string CurrentPlayingFile { get; set; } = "";
    
    public bool IsPlaying => _waveOutEvent?.PlaybackState == PlaybackState.Playing;

    public bool IsPaused => _waveOutEvent?.PlaybackState == PlaybackState.Paused;

    public bool IsStopped => !IsPlaying && !IsPaused;

    public void Pause()
    {
        if (_waveOutEvent == null) return;
        _waveOutEvent?.Pause();
        PlayPaused?.Invoke(this, EventArgs.Empty);
    }

    public void PlayPause()
    {
        if (_waveOutEvent == null) return;
        if (_waveOutEvent.PlaybackState == PlaybackState.Playing)
        {
            Pause();
        }
        else if (_waveOutEvent.PlaybackState == PlaybackState.Paused)
        {
            Play();
        }
    }

    public void Play()
    {
        if (_waveOutEvent == null) return;
        _waveOutEvent?.Play();
        PlayStarted?.Invoke(this, EventArgs.Empty);
    }

    public double? GetCurrentPosition()
    {
        if (_waveOutEvent == null || _loopStream == null) return null;
        var value = (1.0 * _loopStream.Position) / (1.0 * _loopStream.Length);
        return value;
    }

    public double GetLengthSeconds()
    {
        if (_loopStream == null) return 0;
        return (_loopStream.Length - 8) / 4.0 / 44100.0;
    }
    
    public double GetCurrentPositionSeconds()
    {
        if (_loopStream == null) return 0;
        return (_loopStream.Position - 8) / 4.0 / 44100.0;
    }

    public void SetPosition(double value)
    {
        if (_waveOutEvent == null || _loopStream == null) return;
        value = Math.Clamp(value, 0.0, 1.0);
        _loopStream.Position = (long)(_loopStream.Length * value + 8.0);
    }

    public void JumpToTime(double seconds)
    {
        SetPosition(seconds / GetLengthSeconds());
    }

    public void SetVolume(double volume)
    {
        if (_waveOutEvent == null || _loopStream == null) return;
        volume = Math.Clamp(volume, 0.0, 1.0);
        _waveOutEvent.Volume = (float)volume;
    }
    
    public async Task<bool> StopSongAsync(string? newSongPath = null, bool waitForFile = false)
    {
        if (_waveOutEvent?.PlaybackState == PlaybackState.Playing || _waveOutEvent?.PlaybackState == PlaybackState.Paused)
        {
            _waveOutEvent?.Stop();
        }

        if (waitForFile && !string.IsNullOrEmpty(CurrentPlayingFile))
        {
            newSongPath = CurrentPlayingFile;
        }
        
        // If we're replaying the same song, wait until the song is accessible
        if (CurrentPlayingFile == newSongPath)
        {
            for(var i = 0; i < 30; i++)
            {
                try
                {
                    using var reader = new BinaryReader(new FileStream(newSongPath, FileMode.Open));
                    break;
                }
                catch
                {
                    await Task.Delay(200);
                }
            }
                
            try
            {
                using var reader = new BinaryReader(new FileStream(newSongPath, FileMode.Open));
            }
            catch
            {
                _logger.LogInformation("Song not accessible");
                return false;
            }
        }

        // Wait until the previous song has stopped playing
        if (_waveOutEvent != null)
        {
            var canPlay = false;
            for(var i = 0; i < 30; i++) 
            {
                await Task.Delay(200);
                if (_waveOutEvent == null)
                {
                    canPlay = true;
                    break;
                }
            }

            try
            {
                if (_waveOutEvent != null)
                {
                    _waveOutEvent?.Dispose();
                    _waveOutEvent = null;
                }

                if (_loopStream != null)
                {
                    await _loopStream.DisposeAsync();
                    _loopStream = null;
                }

                canPlay = true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error stopping music");
            }
            
            _logger.LogInformation("Song stopped playing {Value}", canPlay ? "successfully" : "unsuccessfully");
            return canPlay;
        }

        _logger.LogInformation("Song stopped playing successfully");
        return true;
    }
    
    public async Task<bool> PlaySongAsync(string path, bool fromEnd, bool isLoopingSong)
    {
        var canPlay = await StopSongAsync(path);

        if (!canPlay) return false;
        
        CurrentPlayingFile = path;
        
        _ = ITaskService.Run(() =>
        {
            _logger.LogInformation("Playing song {Path}", path);
            
            var initBytes = new byte[8];
            using (var reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                reader.Read(initBytes, 0, 8);
            }
            
            _logger.LogInformation("Audio file read");

            var loopPoint = BitConverter.ToInt32(initBytes, 4) * 1.0;
            var totalBytes = new FileInfo(path).Length - 8.0;
            var totalSamples = totalBytes / 4.0;
            var loopBytes = (long)(loopPoint / totalSamples * totalBytes) + 8;
            var startPosition = 8L;
            if (fromEnd)
            {
                var endSamples = totalSamples - 44100 * _settings.LoopDuration;
                startPosition = (long)(endSamples / totalSamples * totalBytes) + 8;
                if (startPosition < 8)
                {
                    startPosition = 8;
                }
            }

            // Fix bad loops to be at the beginning
            var enableLoop = loopBytes >= 8 && loopBytes < totalBytes + 8;

            try
            {
                using (var fs = File.OpenRead(path))
                using (var rs = new RawSourceWaveStream(fs, new WaveFormat(44100, 2)))
                using (var loop = new LoopStream(rs))
                using (var waveOutEvent = new WaveOutEvent())
                {
                    loop.EnableLooping = enableLoop;
                    _waveOutEvent = waveOutEvent;
                    _waveOutEvent.Volume = (float)_settings.Volume;
                    _loopStream = loop;
                    waveOutEvent.Init(loop);
                    loop.Position = startPosition;
                    loop.LoopPosition = loopBytes;
                    Play();
                    _logger.LogInformation("Playing audio file");
                    PlayStarted?.Invoke(this, EventArgs.Empty);
                    Thread.Sleep(200);
                    while (waveOutEvent.PlaybackState != PlaybackState.Stopped)
                    {
                        Thread.Sleep(200);
                    }
                    _waveOutEvent = null;
                    _loopStream = null;
                    PlayStopped?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failure playing song");
                CurrentPlayingFile = "";
               
                try
                {
                    if (_loopStream != null)
                    {
                        _loopStream.Dispose();
                        _loopStream = null;
                    }
                    
                    if (_waveOutEvent != null)
                    {
                        _waveOutEvent.Dispose();
                        _waveOutEvent = null;
                    }

                    canPlay = true;
                }
                catch (Exception e2)
                {
                    _logger.LogError(e2, "Error stopping music");
                }
            }
            
        });

        return true;
    }

    

    public EventHandler? PlayStarted { get; set; }
    public EventHandler? PlayPaused { get; set; }
    public EventHandler? PlayStopped { get; set; }

    public bool CanPlayMusic { get; set; } = true;

    public bool CanSetMusicPosition { get; set; } = true;

    public bool CanChangeVolume { get; set; } = true;
    public bool CanPauseMusic { get; set; } = true;
}
