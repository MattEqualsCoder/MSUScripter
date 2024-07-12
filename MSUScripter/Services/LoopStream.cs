using System;
using NAudio.Wave;

namespace MSUScripter.Services;

/// <summary>
/// Stream for looping playback
/// </summary>
public class LoopStream (WaveStream sourceStream) : WaveStream
{
    /// <summary>
    /// Use this to turn looping on or off
    /// </summary>
    public bool EnableLooping { get; set; } = true;

    /// <summary>
    /// Return source stream's wave format
    /// </summary>
    public override WaveFormat WaveFormat => sourceStream.WaveFormat;

    /// <summary>
    /// LoopStream simply returns
    /// </summary>
    public override long Length => sourceStream.Length;

    /// <summary>
    /// LoopStream simply passes on positioning to source stream
    /// </summary>
    public override long Position
    {
        get => sourceStream.Position;
        set => sourceStream.Position = value;
    }

    public long LoopPosition { get; set; } = 100000;

    public override int Read(byte[] buffer, int offset, int count)
    {
        var totalBytesRead = 0;

        while (totalBytesRead < count)
        {
            try
            {
                var bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                {
                    
                    if (sourceStream.Position == 0 || !EnableLooping)
                    {
                        // something wrong with the source stream
                        break;
                    }
                    
                    // loop
                    sourceStream.Position = LoopPosition;
                }
                totalBytesRead += bytesRead;
            }
            catch (Exception)
            {
                break;
            }
        }
        return totalBytesRead;
    }
}