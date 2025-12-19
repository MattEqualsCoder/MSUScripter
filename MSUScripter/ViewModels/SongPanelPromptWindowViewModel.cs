using ReactiveUI.SourceGenerators;

namespace MSUScripter.ViewModels;

public partial class SongPanelPromptWindowViewModel : TranslatedViewModelBase
{
    [Reactive] public partial bool Basic { get; set; }
    [Reactive] public partial bool Advanced { get; set; }
    [Reactive] public partial bool DontAskAgain { get; set; }

    public SongPanelPromptWindowViewModel()
    {
        Basic = true;
        DontAskAgain = true;
    }
    
    public override ViewModelBase DesignerExample()
    {
        return new SongPanelPromptWindowViewModel();
    }
}