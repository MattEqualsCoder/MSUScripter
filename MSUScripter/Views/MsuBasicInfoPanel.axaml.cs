using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaControls;
using AvaloniaControls.Controls;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class MsuBasicInfoPanel : UserControl
{
    private MsuBasicInfoViewModel? MsuBasicInfoViewModel => DataContext as MsuBasicInfoViewModel;
    
    public MsuBasicInfoPanel()
    {
        InitializeComponent();
        
        if (Design.IsDesignMode)
        {
            DataContext = (MsuBasicInfoViewModel)new MsuBasicInfoViewModel().DesignerExample();
        }
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void MsuFilePathControl_OnOnUpdated(object? sender, FileControlUpdatedEventArgs e)
    {
        if (e.Path != MsuBasicInfoViewModel?.Project.MsuPath) return;
        await MessageWindow.ShowErrorDialog(
            "The Super Metroid and A Link to the Past MSU paths must be different from the main MSU path and each other.",
            "Invalid MSU Path", TopLevel.GetTopLevel(this) as Window ?? App.MainWindow!);
    }
}