using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class MsuSongMsuPcmPanelViewModel : ViewModelBase
{
    [Reactive] public bool AdvancedMode { get; set; }
    
    public bool IsTopLevel { get; set; }
    public bool IsSubChannel { get; set; }
    public string HeaderText =>
        IsTopLevel ? "MsuPcm++ Details" : IsSubChannel ? "Sub Channel Details" : "Sub Track Details";
    
    
    
    public override ViewModelBase DesignerExample()
    {
        throw new System.NotImplementedException();
    }
}