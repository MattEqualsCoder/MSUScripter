using System.Collections.Generic;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class PackageMsuWindowViewModel : ViewModelBase
{
    [Reactive] public string ButtonText { get; set; } = "Cancel";

    [Reactive] public MsuProjectViewModel Project { get; set; } = new();

    [Reactive] public string Response { get; set; } = "";
    
    [Reactive] public bool IsRunning { get; set; }

    public List<string> ValidPcmPaths = [];
    
    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}