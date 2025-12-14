using System;
using System.IO;
using System.Linq;
using MSUScripter.Configs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MSUScripter.Services;

public class PcmModifierService
{
    public void UpdatePcmFile(string tempFile, string outFile, MsuSongInfo song)
    {
        var volumeMultiplier = song.MsuPcmInfo.IsPostGenerateVolumeDecibels
            ? MathF.Pow(10, song.MsuPcmInfo.PostGenerateVolumeModifier!.Value / 20f)
            : song.MsuPcmInfo.PostGenerateVolumeModifier!.Value / 100f;

        var waveFormat = new WaveFormat(
            rate: 44100,
            bits: 16,
            channels: 2
        );

        using var inputStream = File.OpenRead(tempFile);

        // Get the bytes for looping
        var headerBytes = new byte[8];
        inputStream.ReadExactly(headerBytes, 0, 8);
        inputStream.Position = 0;
        
        // Load the source, convert to samples, apply modifiers, and convert back to PCM
        using var rawSource = new RawSourceWaveStream(inputStream, waveFormat);
        var sampleProvider = rawSource.ToSampleProvider();
        var volumeProvider = new VolumeSampleProvider(sampleProvider)
        {
            Volume = volumeMultiplier
        };
        var pcm16Provider = new SampleToWaveProvider16(volumeProvider);

        using var outputStream = File.Create(outFile);
        var buffer = new byte[4096];
        int bytesRead;
        var isFirstRead = true;
        
        // Write the modified stream to file, using the previous first 8 bytes
        while ((bytesRead = pcm16Provider.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (isFirstRead)
            {
                isFirstRead = false;
                outputStream.Write(headerBytes, 0, 8);
                outputStream.Write(buffer.Skip(8).ToArray(), 0, bytesRead - 8);
            }
            else
            {
                outputStream.Write(buffer, 0, bytesRead);
            }
        }
    }
}