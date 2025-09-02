using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public class SongPanelPromptWindowViewModel : TranslatedViewModelBase
{
    [Reactive] public bool Basic { get; set; } = true;
    [Reactive] public bool Advanced { get; set; }
    [Reactive] public bool DontAskAgain { get; set; } = true;
    
    public override ViewModelBase DesignerExample()
    {
        return new SongPanelPromptWindowViewModel();
    }
}