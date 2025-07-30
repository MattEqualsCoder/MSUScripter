using Avalonia.Controls;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class SettingsPanel : UserControl
{
    public SettingsPanel()
    {
        InitializeComponent();
        if (Design.IsDesignMode)
        {
            DataContext = new SettingsPanelViewModel();
        }
    }
}