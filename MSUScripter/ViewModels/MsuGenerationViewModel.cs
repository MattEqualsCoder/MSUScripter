using System;
using System.Collections.Generic;
using MSUScripter.Configs;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class MsuGenerationViewModel : TranslatedViewModelBase
{
    [Reactive] public MsuProject MsuProject { get; set; } = new();
    [Reactive] public List<MsuGenerationRowViewModel> Rows { get; set; } = [];
    [Reactive] public int TotalSongs { get; set; }
    [Reactive] public int SongsCompleted { get; set; }
    [Reactive] public string ButtonText { get; set; } = "Cancel";
    [Reactive] public bool IsFinished { get; set; }
    [Reactive] public int NumErrors { get; set; }
    [Reactive] public List<string> GenerationErrors { get; set; } = [];
    public double GenerationSeconds { get; set; }
    public string? ZipPath { get; set; }
    
    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}

public enum MsuGenerationRowType
{
    Msu,
    Song,
    Yaml,
    MsuPcmJson,
    TrackList,
    SwapperScript,
    Smz3Zelda,
    Smz3Metroid,
    Smz3Script,
    Smz3ZeldaYaml,
    Smz3MetroidYaml,
    Compress
}

public class MsuGenerationRowViewModel : ViewModelBase
{
    [Reactive] public string Title { get; set; }
    
    [Reactive] public MsuSongInfo? SongInfo { get; set; }

    [Reactive] public string Path { get; set; }
    [Reactive] public string PathDisplay { get; set; } = "";
    
    [Reactive] public bool HasLoaded { get; set; }
    
    [Reactive] public bool HasWarning { get; set; }

    [Reactive] public string Message { get; set; }

    [Reactive] public bool CanParallelize { get; set; } = true;

    public MsuGenerationRowType Type { get; }
    
    public MsuGenerationRowViewModel(MsuSongInfo songInfo)
    {
        Type = MsuGenerationRowType.Song;
        SongInfo = songInfo;
        Title = $"Track #{songInfo.TrackNumber} - {System.IO.Path.GetFileName(songInfo.OutputPath)}";
        Path = songInfo.OutputPath ?? "";
        Message = "Waiting";
        SetPathDisplay();
    }

    public MsuGenerationRowViewModel(MsuGenerationRowType type, MsuProject project, string? path = null)
    {
        Type = type;
        Title = type switch
        {
            MsuGenerationRowType.Msu => "MSU File",
            MsuGenerationRowType.Yaml => "MSU Randomizer YAML",
            MsuGenerationRowType.MsuPcmJson => "MsuPcm++ Tracks JSON File",
            MsuGenerationRowType.TrackList => "Track List",
            MsuGenerationRowType.SwapperScript => "Alt Track Swapper Script",
            MsuGenerationRowType.Smz3Zelda => "A Link to the Past MSU",
            MsuGenerationRowType.Smz3ZeldaYaml => "A Link to the Past MSU YAML",
            MsuGenerationRowType.Smz3Metroid => "Super Metroid MSU",
            MsuGenerationRowType.Smz3MetroidYaml => "Super Metroid MSU YAML",
            MsuGenerationRowType.Smz3Script => "SMZ3 to Split ALttP & SM MSUs Script",
            MsuGenerationRowType.Compress => "Compressing to ZIP File",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        Path = type switch
        {
            MsuGenerationRowType.Msu => project.MsuPath,
            MsuGenerationRowType.Yaml => project.GetYamlPath(),
            MsuGenerationRowType.MsuPcmJson => project.GetTracksJsonPath(),
            MsuGenerationRowType.TrackList => project.GetTracksTextPath(),
            MsuGenerationRowType.SwapperScript => project.GetAltSwapperPath(),
            MsuGenerationRowType.Smz3Zelda => project.GetZeldaMsuPath(),
            MsuGenerationRowType.Smz3ZeldaYaml => project.GetZeldaMsuYamlPath(),
            MsuGenerationRowType.Smz3Metroid => project.GetMetroidMsuPath(),
            MsuGenerationRowType.Smz3MetroidYaml => project.GetMetroidMsuYamlPath(),
            MsuGenerationRowType.Smz3Script => project.GetSmz3SwapperPath(),
            MsuGenerationRowType.Compress => path ?? "",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        if (type is MsuGenerationRowType.Compress or MsuGenerationRowType.Yaml or MsuGenerationRowType.Smz3MetroidYaml or MsuGenerationRowType.Smz3ZeldaYaml)
        {
            CanParallelize = false;
        }

        Message = "Waiting";
        
        SetPathDisplay();
    }

    private void SetPathDisplay()
    {
        if (Path.Length > 50)
        {
            PathDisplay = "..." + Path[^47..];
        }
        else
        {
            PathDisplay = Path;
        }
    }
    
    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}