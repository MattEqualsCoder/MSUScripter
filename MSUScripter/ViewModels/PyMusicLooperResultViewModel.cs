using System;
using AvaloniaControls.Models;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class PyMusicLooperResultViewModel(int loopStart, int loopEnd, decimal score) : ViewModelBase
{
    [Reactive] public int LoopStart { get; set; } = loopStart;

    [Reactive] public int LoopEnd { get; set; } = loopEnd;

    [Reactive] public decimal Score { get; set; } = Math.Round(score * 100, 2);

    [Reactive] public string Status { get; set; } = "";

    [Reactive] public string Duration { get; set; } = "";
    
    [Reactive] public bool Generated { get; set; }

    [Reactive] public bool IsSelected { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(CanTestFile))]
    public string TempPath { get; set; } = "";

    public bool CanTestFile => !string.IsNullOrEmpty(TempPath);
    
    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}