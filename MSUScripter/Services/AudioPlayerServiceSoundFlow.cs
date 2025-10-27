using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaControls.Services;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;

namespace MSUScripter.Services;

public class AudioPlayerServiceSoundFlow(ILogger<AudioPlayerServiceSoundFlow> logger, Settings settings)
    : IAudioPlayerService
{
    private SoundPlayer? _soundPlayer;
    
    // ReSharper disable once NotAccessedField.Local
    private MiniAudioEngine _audioEngine = new(44100, Capability.Playback);

    public string CurrentPlayingFile { get; set; } = "";
    
    public bool IsPlaying => _soundPlayer?.State == PlaybackState.Playing;

    public bool IsPaused => _soundPlayer?.State == PlaybackState.Paused;

    public bool IsStopped => !IsPlaying && !IsPaused;

    public void Pause()
    {
        if (_soundPlayer == null) return;
        _soundPlayer?.Pause();
        PlayPaused?.Invoke(this, EventArgs.Empty);
    }

    public void PlayPause()
    {
        if (_soundPlayer == null) return;
        if (_soundPlayer.State == PlaybackState.Playing)
        {
            Pause();
        }
        else if (_soundPlayer.State == PlaybackState.Paused)
        {
            Play();
        }
    }

    public void Play()
    {
        if (_soundPlayer == null) return;
        _soundPlayer?.Play();
        PlayStarted?.Invoke(this, EventArgs.Empty);
    }

    public double? GetCurrentPosition()
    {
        if (_soundPlayer == null) return null;
        return _soundPlayer.Time / _soundPlayer.Duration;
    }

    public double GetLengthSeconds()
    {
        if (_soundPlayer == null) return 0;
        return _soundPlayer.Duration;
    }
    
    public double GetCurrentPositionSeconds()
    {
        if (_soundPlayer == null) return 0;
        return _soundPlayer.Time;
    }

    public void SetPosition(double value)
    {
        if (_soundPlayer == null) return;
        var percent = (float)Math.Clamp(value, 0.0, 1.0);
        _soundPlayer.Seek(percent * _soundPlayer.Duration);
    }

    public void JumpToTime(double seconds)
    {
        SetPosition(seconds / GetLengthSeconds());
    }

    public void SetVolume(double volume)
    {
        if (_soundPlayer == null) return;
        volume = Math.Clamp(volume, 0.0, 1.0) * 1.5f;
        _soundPlayer.Volume = (float)volume;
        _soundPlayer.Pan = 0.5f;
    }
    
    public async Task<bool> StopSongAsync(string? newSongPath = null, bool waitForFile = false)
    {
        if (_soundPlayer == null)
        {
            return true;
        }
        
        if (_soundPlayer.State == PlaybackState.Playing)
        {
            _soundPlayer.Stop();
        }

        // Wait until the previous song has stopped playing
        if (_soundPlayer.State == PlaybackState.Playing)
        {
            for(var i = 0; i < 30; i++) 
            {
                logger.LogInformation($"{_soundPlayer.State}");
                await Task.Delay(200);
                if (_soundPlayer.State != PlaybackState.Playing)
                {
                    break;
                }
            }
        }
        
        try
        {
            if (_soundPlayer != null)
            {
                Mixer.Master.RemoveComponent(_soundPlayer);
                _soundPlayer = null;
            }

            logger.LogInformation("Song stopped playing successfully");
            
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error stopping music");

            return false;
        }
    }
    
    public async Task<bool> PlaySongAsync(string path, bool fromEnd, bool isLoopingSong)
    {
        var canPlay = await StopSongAsync(path);

        if (!canPlay) return false;
        
        CurrentPlayingFile = path;
        
        _ = ITaskService.Run(() =>
        {
            logger.LogInformation("Playing song {Path}", path);
            
            var initBytes = new byte[8];
            using (var reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                if (reader.Read(initBytes, 0, 8) < 8)
                {
                    logger.LogInformation("Invalid file");
                    return;
                }
            }
            
            logger.LogInformation("Audio file read");

            var loopSamples = BitConverter.ToInt32(initBytes, 4) * 1;
            var totalBytes = new FileInfo(path).Length - 8;
            var totalSamples = totalBytes / 4;
            var startPosition = 0;
            if (fromEnd)
            {
                var endSamples = totalSamples - 44100 * settings.LoopDuration;
                startPosition = (int)endSamples;
                if (startPosition < 0)
                {
                    startPosition = 0;
                }
            }

            // Fix bad loops to be at the beginning
            if (loopSamples > totalSamples)
            {
                loopSamples = 0;
            }

            var bytes = File.ReadAllBytes(path).Skip(8);
            _soundPlayer = new SoundPlayer(new RawDataProvider(bytes.ToArray(), SampleFormat.S16));

            if (isLoopingSong)
            {
                _soundPlayer.IsLooping = true;
                _soundPlayer.SetLoopPoints(loopSamples * 2);
            }
            else
            {
                _soundPlayer.PlaybackEnded += SoundPlayerOnPlaybackEnded;
            }
            
            // Add the player to the master mixer. This connects the player's output to the audio engine's output.
            Mixer.Master.AddComponent(_soundPlayer);

            // Start playback.
            _soundPlayer.Play();
            _soundPlayer.Seek(startPosition * 2);
            _soundPlayer.Volume = (float)settings.Volume;
            _soundPlayer.Pan = 0.5f;
            
            PlayStarted?.Invoke(this, EventArgs.Empty);
            
        });

        return true;
    }

    private void SoundPlayerOnPlaybackEnded(object? sender, EventArgs e)
    {
        ITaskService.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            _soundPlayer?.Seek(0);
            _soundPlayer?.Play();
        });
    }


    public EventHandler? PlayStarted { get; set; }
    public EventHandler? PlayPaused { get; set; }
    public EventHandler? PlayStopped { get; set; }

    public bool CanPlayMusic { get; set; } = true;
    public bool CanSetMusicPosition { get; set; } = true;
    public bool CanChangeVolume { get; set; } = true;
    public bool CanPauseMusic { get; set; } = true;
}
