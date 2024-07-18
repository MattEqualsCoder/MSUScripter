using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaControls.Models;
using MSUScripter.Configs;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class EditProjectPanelViewModel : ViewModelBase
{
    [Reactive,
     ReactiveLinkedProperties(nameof(DisplayBasicInfoPanel), nameof(DisplayTrackOverviewPanel),
         nameof(DisplayTrackInfoPanel), nameof(SelectedTrack), nameof(CanClickPrev), nameof(CanClickNext))]
    public int PageNumber { get; set; } = 0;
    
    [Reactive] public string StatusBarText { get; set; } = "";
    
    public MsuProject? MsuProject { get; set; }

    [Reactive,
     ReactiveLinkedProperties(nameof(IsMsuPcmProject), nameof(WriteYamlFile), nameof(WriteTrackList),
         nameof(CreateAltSwapper), nameof(CreateSplitSmz3Script))]
    public MsuProjectViewModel? MsuProjectViewModel { get; set; }
    
    public List<MsuTrackInfoViewModel> Tracks { get; set; } = [];
    public MsuTrackInfoViewModel? SelectedTrack => PageNumber < 2 ? null : Tracks[PageNumber - 2];

    public MsuBasicInfoViewModel MsuBasicInfoViewModel => MsuProjectViewModel?.BasicInfo ?? new MsuBasicInfoViewModel();
    public bool CanClickPrev => PageNumber > 0;
    public bool CanClickNext => PageNumber < Tracks.Count + 1;
    public bool IsMsuPcmProject => MsuProjectViewModel?.BasicInfo.IsMsuPcmProject == true;
    public bool WriteYamlFile => MsuProjectViewModel?.BasicInfo.WriteYamlFile == true;
    public bool WriteTrackList => MsuProjectViewModel?.BasicInfo.WriteYamlFile == true;
    public bool CreateAltSwapper => MsuProjectViewModel?.BasicInfo.WriteYamlFile == true;
    public bool CreateSplitSmz3Script => MsuProjectViewModel?.BasicInfo.WriteYamlFile == true;
    public bool DisplayBasicInfoPanel => PageNumber == 0;
    public bool DisplayTrackOverviewPanel => PageNumber == 1;
    public bool DisplayTrackInfoPanel => PageNumber >= 2;
    public DateTime LastAutoSave = DateTime.MaxValue;
    public object? NullValue => null;
    
    [Reactive] public bool DisplayAltSwapperExportButton { get; set; }
    
    [Reactive] public List<ComboBoxAndSearchItem> TrackSearchItems { get; set; } = [];

    public override ViewModelBase DesignerExample()
    {
        StatusBarText = "Project Loaded";
        MsuProjectViewModel = new MsuProjectViewModel()
        {
            BasicInfo = new MsuBasicInfoViewModel()
            {
                IsMsuPcmProject = true
            }
        };
        return this;
    }
}