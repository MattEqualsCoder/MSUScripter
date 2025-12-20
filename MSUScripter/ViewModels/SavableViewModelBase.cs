namespace MSUScripter.ViewModels;

public abstract partial class SavableViewModelBase : TranslatedViewModelBase
{
    public abstract void SaveChanges();
}
