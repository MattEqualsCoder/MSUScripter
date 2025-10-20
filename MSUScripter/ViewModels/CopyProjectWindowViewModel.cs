using System;
using System.Collections.Generic;
using System.IO;
using MSUScripter.Configs;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class CopyProjectWindowViewModel : TranslatedViewModelBase
{
    [Reactive] public MsuProject? OriginalProject { get; set; }

    [Reactive] public MsuProject? NewProject { get; set; }
    
    [Reactive] public MsuProject? SavedProject { get; set; }
    
    [Reactive] public List<CopyProjectViewModel> Paths { get; set; } = [];

    [Reactive] public bool IsValid { get; set; }
    
    [Reactive] public string ButtonText { get; set; } = "Update Project";
    
    [Reactive] public string Title { get; set; } = "Update Project";

    public string TopText =>
        IsCopy
            ? "Update the paths below as desired for the new project."
            : "One or more input files are missing. Update them below or continue opening the project.";
    
    public bool IsCopy { get; set; }
    
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
            BaseFileName = GetFileNameFromAnyPath(PreviousPath);
            Extension = Path.GetExtension(PreviousPath);
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

    public override ViewModelBase DesignerExample()
    {
        return this;
    }
    
    static string GetFileNameFromAnyPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        // Normalize to system-independent form
        path = path.Replace('\\', '/');

        // Extract after last '/'
        int lastSlash = path.LastIndexOf('/');
        if (lastSlash >= 0 && lastSlash < path.Length - 1)
            return path.Substring(lastSlash + 1);

        // Fall back to entire string if no slash
        return path;
    }
}