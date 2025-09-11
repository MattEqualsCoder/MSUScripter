using System.Collections.Generic;

namespace MSUScripter.Models;


public class GetSampleRateRequest
{
    public string Mode => "samples";
    public required string File { get; set; }
}

public class GetSampleRateResponse : PythonCompanionModeResponse
{
    public double Duration { get; set; }
    public int SampleRate { get; set; }
}

public class RunPyMusicLooperRequest
{
    public string Mode => "py_music_looper";
    public required string File { get; set; }
    public double? MinDurationMultiplier { get; set; } = 0.25f;
    public double? MinLoopDuration { get; set; }
    public double? MaxLoopDuration { get; set; }
    public double? ApproxLoopStart { get; set; }
    public double? ApproxLoopEnd { get; set; }

}

public class RunPyMusicLooperResponse : PythonCompanionModeResponse
{
    public List<PyMusicLooperPair> Pairs { get; set; } = [];
}

public class PyMusicLooperPair
{
    public int LoopStart { get; set; }
    public int LoopEnd { get; set; }
    public double LoudnessDifference { get; set; }
    public double NoteDistance { get; set; }
    public double Score { get; set; }
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
    public string Result { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}