using MSUScripter.Text;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public abstract class TranslatedViewModelBase : ViewModelBase
{
    public TranslatedViewModelBase()
    {
        ApplicationText.LanguageChanged += (_, text) =>
        {
            Text = text;
        };
    }
    
    [Reactive] public ApplicationText Text { get; set; } = ApplicationText.CurrentLanguageText;
}