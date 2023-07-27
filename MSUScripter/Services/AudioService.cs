using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace MSUScripter.Services;

public class AudioService
{
    private WaveOutEvent? _waveOutEvent;

    public static AudioService Instance { get; private set; } = null!;

    public AudioService()
    {
        Instance = this;
    }

    public string CurrentPlayingFile { get; private set; } = "";
    
    public void StopSong()
    {
        if (_waveOutEvent?.PlaybackState == PlaybackState.Playing)
        {
            _waveOutEvent?.Stop();
        }
    }
    
    public bool PlaySong(string path, bool fromEnd)
    {
        StopSong();
        
        // If we're replaying the same song, wait until the song is accessible
        if (CurrentPlayingFile == path)
        {
            for(var i = 0; i < 30; i++)
            {
                try
                {
                    using var reader = new BinaryReader(new FileStream(path, FileMode.Open));
                    break;
                }
                catch
                {
                    Thread.Sleep(200);
                }
            }
                
            try
            {
                using var reader = new BinaryReader(new FileStream(path, FileMode.Open));
            }
            catch
            {
                return false;
            }
        }

        // Wait until the previous song has stopped playing
        if (_waveOutEvent != null)
        {
            var canPlay = false;
            for(var i = 0; i < 30; i++) 
            {
                Thread.Sleep(200);
                if (_waveOutEvent == null)
                {
                    canPlay = true;
                    break;
                }
            }

            if (!canPlay)
            {
                return false;
            }
        }

        CurrentPlayingFile = path;
        

        Task.Run(() =>
        {
            var initBytes = new byte[8];
            using (var reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                reader.Read(initBytes, 0, 8);
            }

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

            using (var fs = File.OpenRead(path))
            using (var rs = new RawSourceWaveStream(fs, new WaveFormat(44100, 2)))
            using (var loop = new LoopStream(rs))
            using (var waveOutEvent = new WaveOutEvent())
            {
                loop.EnableLooping = enableLoop;
                _waveOutEvent = waveOutEvent;
                waveOutEvent.Init(loop);
                loop.Position = startPosition;
                loop.LoopPosition = loopBytes;
                waveOutEvent.Play();
                Thread.Sleep(200);
                while (waveOutEvent.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(200);
                }
                _waveOutEvent = null;
            }
        });

        return true;
    }
}