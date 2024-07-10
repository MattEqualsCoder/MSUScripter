using System.Collections.Generic;
using System.Linq;
using AvaloniaControls.Models;
using MSUScripter.Configs;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class PyMusicLooperPanelViewModel : ViewModelBase
{
    [Reactive] public double MinDurationMultiplier { get; set; } = 0.25;
    [Reactive] public int? MinLoopDuration { get; set; }
    [Reactive] public int? MaxLoopDuration { get; set; }
    [Reactive] public int? ApproximateStart { get; set; }
    [Reactive] public int? ApproximateEnd { get; set; }
    [Reactive] public List<PyMusicLooperResultViewModel> PyMusicLooperResults { get; set; } = [];
    [Reactive] public PyMusicLooperResultViewModel? SelectedResult { get; set; }
    [Reactive] public MsuSongInfoViewModel MsuSongInfoViewModel { get; set; } = new();
    [Reactive] public MsuProjectViewModel MsuProjectViewModel { get; set; } = new();
    [Reactive] public MsuProject MsuProject { get; set; } = new();
    [Reactive] public MsuSongMsuPcmInfoViewModel MsuSongMsuPcmInfoViewModel { get; set; } = new();
    [Reactive] public bool DisplayGitHubLink { get; set; }
    [Reactive] public bool DisplayOldVersionWarning { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(FilteredResults))]
    public int? FilterStart { get; set; }

    [Reactive, ReactiveLinkedProperties(nameof(FilteredResults))]
    public int? FilterEnd { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(CanClickOnPrev), nameof(CanClickOnNext))]
    public int LastPage { get; set; }

    [Reactive, ReactiveLinkedProperties(nameof(CanClickOnPrev), nameof(CanClickOnNext), nameof(CurrentPageResults))]
    public int Page { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(DisplayResultsTable), nameof(DisplayMessage))] 
    public string? Message { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(CanClickOnPrev), nameof(CanClickOnNext))]
    public bool GeneratingPcms { get; set; }
    
    [Reactive, ReactiveLinkedProperties(nameof(CurrentPageResults))]
    public List<PyMusicLooperResultViewModel> FilteredResults { get; set; } = [];
    
    public bool HasTestedPyMusicLooper { get; set; }
    public bool IsRunning { get; set; }
    public bool DisplayResultsTable => string.IsNullOrEmpty(Message);
    public bool DisplayMessage => !DisplayResultsTable;
    public bool CanClickOnPrev => Page > 0 && !GeneratingPcms;
    public bool CanClickOnNext => Page < LastPage && !GeneratingPcms;
    public int NumPerPage => 8;
    
    public List<PyMusicLooperResultViewModel> CurrentPageResults =>
        FilteredResults.Skip(Page * NumPerPage).Take(NumPerPage).ToList();
    
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
            }
        ];
        return this;
    }
}