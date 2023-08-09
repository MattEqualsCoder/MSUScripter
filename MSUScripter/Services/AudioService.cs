using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using NAudio.Wave;

namespace MSUScripter.Services;

public class AudioService
{
    private WaveOutEvent? _waveOutEvent;
    private LoopStream? _loopStream;
    private readonly ILogger<AudioService> _logger;
    private readonly Settings _settings;

    public AudioService(ILogger<AudioService> logger, Settings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public string CurrentPlayingFile { get; private set; } = "";

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

    public void SetPosition(double value)
    {
        if (_waveOutEvent == null || _loopStream == null) return;
        value = Math.Clamp(value, 0.0, 1.0);
        _loopStream.Position = (long)(_loopStream.Length * value + 8.0);
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
            
            _logger.LogInformation("Song stopped playing {Value}", canPlay ? "successfully" : "unsuccessfully");
            return canPlay;
        }

        _logger.LogInformation("Song stopped playing successfully");
        return true;
    }
    
    public async Task<bool> PlaySongAsync(string path, bool fromEnd)
    {
        var canPlay = await StopSongAsync(path);

        if (!canPlay) return false;
        
        CurrentPlayingFile = path;
        
        _ = Task.Run(() =>
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
                var endSamples = totalSamples - 44100 * 3;
                startPosition = (long)(endSamples / totalSamples * totalBytes) + 8;
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
            }
            
        });

        return true;
    }
    
    public EventHandler? PlayStarted { get; set; }
    public EventHandler? PlayPaused { get; set; }
    public EventHandler? PlayStopped { get; set; }
}
