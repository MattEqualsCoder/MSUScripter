using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Platform.Storage;
using MSUScripter.Configs;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class CopyProjectWindowViewModel : ViewModelBase
{
    [Reactive] public MsuProject? OriginalProject { get; set; }

    [Reactive] public MsuProjectViewModel? ProjectViewModel { get; set; }

    [Reactive] public MsuProject? NewProject { get; set; }
    
    [Reactive] public List<CopyProjectViewModel> Paths { get; set; } = new();

    [Reactive] public bool IsValid { get; set; }
    
    [Reactive] public string ButtonText { get; set; } = "Update Project";
    
    public override ViewModelBase DesignerExample()
    {
        Paths =
        [
            new CopyProjectViewModel(@"C:\Test\TestMsuProject.msup"),
            new CopyProjectViewModel(@"C:\Test\TestMsu.msu")
            {
                Message = "Bad file"
            },
            new CopyProjectViewModel(@"C:\Test\TestSong.mp3")
            {
                IsValid = true
            }
        ];
        return this;
    }
}

public class CopyProjectViewModel : ViewModelBase
{
    public CopyProjectViewModel(string? path)
    {
        PreviousPath = path ?? "";
        NewPath = path ?? "";
        if (!string.IsNullOrEmpty(PreviousPath))
        {
            var file = new FileInfo(PreviousPath);
            BaseFileName = file.Name;
            Extension = file.Extension;
        }
    }

    [Reactive] public string PreviousPath { get; set; }

    [Reactive] public string NewPath { get; set; }

    [Reactive] public string Extension { get; set; } = "";

    [Reactive] public string BaseFileName { get; set; } = "";

    [Reactive] public bool IsValid { get; set; }

    [Reactive] public string Message { get; set; } = "";

    public bool IsSongFile => !Extension.Equals(".msup", StringComparison.OrdinalIgnoreCase) &&
                              !Extension.Equals(".msu", StringComparison.OrdinalIgnoreCase);
    public List<FilePickerFileType>? FileTypePatterns =>
        string.IsNullOrEmpty(Extension)
            ? null
            : new List<FilePickerFileType>
            {
                new($"{Extension} File") { Patterns = new List<string> { $"*{Extension}" } }
            };

    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}