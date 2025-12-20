using MSUScripter.Text;
using ReactiveUI.SourceGenerators;

namespace MSUScripter.ViewModels;

public abstract partial class TranslatedViewModelBase : ViewModelBase
{
    public TranslatedViewModelBase()
    {
        Text = ApplicationText.CurrentLanguageText;;
        ApplicationText.LanguageChanged += (_, text) =>
        {
            Text = text;
        };
    }
    
    [Reactive] public partial ApplicationText Text { get; set; }
}