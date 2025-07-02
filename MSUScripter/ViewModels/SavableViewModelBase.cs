namespace MSUScripter.ViewModels;

public abstract class SavableViewModelBase : TranslatedViewModelBase
{
    public abstract void SaveChanges();
}
