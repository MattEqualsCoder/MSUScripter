using System;
using AvaloniaControls.Models;
using ReactiveUI.SourceGenerators;

namespace MSUScripter.ViewModels;

public partial class PyMusicLooperResultViewModel: ViewModelBase
{
    public int LoopStart { get; set; }

    public int LoopEnd { get; set; }

    public decimal Score { get; set; }

    [Reactive] public partial string Status { get; set; }

    [Reactive] public partial string Duration { get; set; }
    
    [Reactive] public partial bool Generated { get; set; }

    [Reactive] public partial bool IsSelected { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(CanTestFile))]
    public partial string TempPath { get; set; }

    public bool CanTestFile => !string.IsNullOrEmpty(TempPath);

    public PyMusicLooperResultViewModel(int loopStart, int loopEnd, decimal score)
    {
        LoopStart = loopStart;
        LoopEnd = loopEnd;
        Score = Math.Round(score * 100, 2);
        Status = string.Empty;
        Duration = string.Empty;
        TempPath = string.Empty;
    }
    
    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}