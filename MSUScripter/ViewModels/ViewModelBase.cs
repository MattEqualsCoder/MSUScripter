using AvaloniaControls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MSUScripter.ViewModels;

public abstract class ViewModelBase : ReactiveObject
{
    protected ViewModelBase()
    {
        this.LinkProperties();
        
        PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != nameof(HasBeenModified))
            {
                HasBeenModified = true;
            }
        };
    }

    public abstract ViewModelBase DesignerExample();
    
    [Reactive] public bool HasBeenModified { get; set; }
    
    
}
