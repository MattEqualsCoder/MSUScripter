namespace MSUScripter.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
    public SettingsPanelViewModel SettingsPanelViewModel { get; } = new();

    public override ViewModelBase DesignerExample()
    {
        return this;
    }
}