using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaControls.Models;
using MSUScripter.Configs;
using MSUScripter.Models;
using ReactiveUI.SourceGenerators;

#pragma warning disable CS0067 // Event is never used

namespace MSUScripter.ViewModels;

[SkipLastModified]
public partial class PyMusicLooperPanelViewModel : TranslatedViewModelBase
{
    [Reactive] public partial double MinDurationMultiplier { get; set; }
    [Reactive] public partial int? MinLoopDuration { get; set; }
    [Reactive] public partial int? MaxLoopDuration { get; set; }
    [Reactive] public partial int? ApproximateStart { get; set; }
    [Reactive] public partial int? ApproximateEnd { get; set; }
    [Reactive] public partial List<PyMusicLooperResultViewModel> PyMusicLooperResults { get; set; }
    [Reactive] public partial PyMusicLooperResultViewModel? SelectedResult { get; set; }
    [Reactive] public partial MsuProject MsuProject { get; set; }
    [Reactive] public partial bool DisplayGitHubLink { get; set; }
    [Reactive] public partial bool DisplayOldVersionWarning { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(FilteredResults)), ReactiveLinkedEvent(nameof(FilteredResultsUpdated))]
    public partial int? FilterStart { get; set; }

    [Reactive, ReactiveLinkedProperties(nameof(FilteredResults)), ReactiveLinkedEvent(nameof(FilteredResultsUpdated))]
    public partial int? FilterEnd { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(CanClickOnPrev), nameof(CanClickOnNext))]
    public partial int LastPage { get; set; }

    [Reactive, ReactiveLinkedProperties(nameof(CanClickOnPrev), nameof(CanClickOnNext), nameof(CurrentPageResults))]
    public partial int Page { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(DisplayResultsTable), nameof(DisplayMessage))] 
    public partial string? Message { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(CanClickOnPrev), nameof(CanClickOnNext))]
    public partial bool GeneratingPcms { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(CurrentPageResults))]
    public partial List<PyMusicLooperResultViewModel> FilteredResults { get; set; }

    public string? FilePath { get; set; } = string.Empty;
    
    public double? Normalization { get; set; }
    
    public bool HasTestedPyMusicLooper { get; set; }
    public bool IsRunning { get; set; }
    public bool DisplayResultsTable => string.IsNullOrEmpty(Message);
    public bool DisplayMessage => !DisplayResultsTable;
    public bool CanClickOnPrev => Page > 0 && !GeneratingPcms;
    public bool CanClickOnNext => Page < LastPage && !GeneratingPcms;
    public int NumPerPage => 8;
    [Reactive] public partial bool CanRun { get; set; }
    [Reactive] public partial bool DisplayAutoRun { get; set; }
    [Reactive] public partial bool AutoRun { get; set; }

    public event EventHandler? FilteredResultsUpdated;
    
    public List<PyMusicLooperResultViewModel> CurrentPageResults =>
        FilteredResults.Skip(Page * NumPerPage).Take(NumPerPage).ToList();

    public PyMusicLooperPanelViewModel()
    {
        MsuProject = new MsuProject();
        MinDurationMultiplier = 0.25;
        PyMusicLooperResults = [];
        FilteredResults = [];
    }
    
    public override ViewModelBase DesignerExample()
    {
        FilteredResults =
        [
            new PyMusicLooperResultViewModel(10000, 500000, new decimal(0.95))
            {
                Duration = "1:00"
            },
            new PyMusicLooperResultViewModel(12000, 502000, new decimal(0.94))
            {
                Duration = "1:00"
            },
            new PyMusicLooperResultViewModel(12000, 502000, new decimal(0.94))
            {
                Duration = "1:00"
            },
            new PyMusicLooperResultViewModel(12000, 502000, new decimal(0.94))
            {
                Duration = "1:00"
            },
            new PyMusicLooperResultViewModel(12000, 502000, new decimal(0.94))
            {
                Duration = "1:00"
            },
            new PyMusicLooperResultViewModel(12000, 502000, new decimal(0.94))
            {
                Duration = "1:00"
            },
            new PyMusicLooperResultViewModel(12000, 502000, new decimal(0.94))
            {
                Duration = "1:00"
            },
            new PyMusicLooperResultViewModel(12000, 502000, new decimal(0.94))
            {
                Duration = "1:00"
            }
        ];
        return this;
    }
}