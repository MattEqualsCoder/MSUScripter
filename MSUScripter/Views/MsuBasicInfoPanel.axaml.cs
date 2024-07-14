using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaControls;
using AvaloniaControls.Controls;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class MsuBasicInfoPanel : UserControl
{
    private readonly MsuBasicInfoViewModel _model;
    
    public MsuBasicInfoPanel() : this((MsuBasicInfoViewModel)new MsuBasicInfoViewModel().DesignerExample())
    {
    }
    
    public MsuBasicInfoPanel(MsuBasicInfoViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _model = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void MsuFilePathControl_OnOnUpdated(object? sender, FileControlUpdatedEventArgs e)
    {
        if (e.Path != _model.Project.MsuPath) return;
        await MessageWindow.ShowErrorDialog(
            "The Super Metroid and A Link to the Past MSU paths must be different from the main MSU path and each other.",
            "Invalid MSU Path", TopLevel.GetTopLevel(this) as Window ?? App.MainWindow!);
    }
}