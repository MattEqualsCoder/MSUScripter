using Avalonia.Controls;
using Avalonia.Interactivity;
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

    private void CheckDependenciesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window parent)
        {
            return;
        }
        var newWindow = new InstallDependenciesWindow();
        newWindow.ShowDialog(parent);
    }
}