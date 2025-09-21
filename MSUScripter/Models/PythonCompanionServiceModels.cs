using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace MSUScripter.Models;

public class GetSampleRateRequest
{
    public string Mode => "samples";
    public required string File { get; init; }
}

public class GetSampleRateResponse : PythonCompanionModeResponse
{
    public double Duration { get; set; }
    public int SampleRate { get; init; } = 44100;
    public int Channels { get; init; }
    public int BitsPerSample { get; init; }
    public bool IsBlankSuccess { get; init; }
}

public class RunPyMusicLooperRequest
{
    public string Mode => "py_music_looper";
    public required string File { get; init; }
    public double? MinDurationMultiplier { get; init; } = 0.25f;
    public double? MinLoopDuration { get; init; }
    public double? MaxLoopDuration { get; init; }
    public double? ApproxLoopStart { get; init; }
    public double? ApproxLoopEnd { get; init; }

}

public class RunPyMusicLooperResponse : PythonCompanionModeResponse
{
    public List<PyMusicLooperPair> Pairs { get; init; } = [];
}

public class PythonCompanionModeResponse
{
    public bool Successful { get; set; }
    public string Error { get; set; } = string.Empty;
}

public class CreateVideoRequest
{
    public string Mode => "create_video";
    public required string OutputVideo { get; set; }
    public string? ProgressFile { get; set; }
    public required List<string> Files { get; set; }
}

public class CreateVideoResponse : PythonCompanionModeResponse;

public class RunPyResult
{
    public bool Success { get; set; }
    public string Result { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public bool IsBlankSuccess => Success && string.IsNullOrEmpty(Result) && string.IsNullOrEmpty(Error);
}
public class PyMusicLooperPair
{
    public int LoopStart { get; set; }
    public int LoopEnd { get; set; }
    public decimal LoudnessDifference { get; set; }
    public decimal NoteDistance { get; set; }
    public decimal Score { get; set; }
}

public class PyMusicLooperCacheKey
{
    private string File { get; }
    private DateTime AudioFileModifiedDate { get; }
    private long AudioFileLength { get; }
    private string MinDurationMultiplier { get; }
    private string MinLoopDuration { get; }
    private string MaxLoopDuration { get; }
    private string ApproxLoopStart { get; }
    private string ApproxLoopEnd { get; }

    public PyMusicLooperCacheKey(RunPyMusicLooperRequest request)
    {
        File = request.File;
        
        var fileInfo = new FileInfo(request.File);
        AudioFileModifiedDate = fileInfo.LastWriteTimeUtc;
        AudioFileLength = fileInfo.Length;

        MinDurationMultiplier = Math.Round(request.MinDurationMultiplier ?? -1, 4).ToString(CultureInfo.InvariantCulture);
        MinLoopDuration = Math.Round(request.MinLoopDuration ?? -1, 4).ToString(CultureInfo.InvariantCulture);
        MaxLoopDuration = Math.Round(request.MaxLoopDuration ?? -1, 4).ToString(CultureInfo.InvariantCulture);
        ApproxLoopStart = Math.Round(request.ApproxLoopStart ?? -1, 4).ToString(CultureInfo.InvariantCulture);
        ApproxLoopEnd = Math.Round(request.ApproxLoopEnd ?? -1, 4).ToString(CultureInfo.InvariantCulture);
    }

    public override string ToString()
    {
        var inputBytes = Encoding.UTF8.GetBytes(File);
        var hashBytes = System.Security.Cryptography.MD5.HashData(inputBytes);
        var fileHash = Convert.ToHexString(hashBytes);
            
        var start =
            $"{File}|{AudioFileModifiedDate}|{AudioFileLength}|{MinDurationMultiplier}|{MinLoopDuration}|{MaxLoopDuration}|{ApproxLoopStart}|{ApproxLoopEnd}";
        inputBytes = Encoding.UTF8.GetBytes(start);
        hashBytes = System.Security.Cryptography.MD5.HashData(inputBytes);
        var keyHash = Convert.ToHexString(hashBytes);

        return $"{fileHash}_{keyHash}";
    }
}
