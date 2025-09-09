using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Models;
using MSUScripter.Configs;
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

    private async void DitherEnumComboBox_OnValueChanged(object sender, EnumValueChangedEventArgs args)
    {
        var newValue = (DitherType)args.EnumValue;

        if (newValue is DitherType.DefaultOn or DitherType.DefaultOff && MsuBasicInfoViewModel?.HasSeenDitherWarning == false)
        {
            await MessageWindow.ShowInfoDialog(
                "While the MSU Scripter application can work with different dither values per track in an MSU, this is not " +
                "compatible with MsuPcm++ tracks json files with multiple tracks in it. Because of this, if you export " +
                "the MsuPcm++ tracks json file, it will use the default value for all songs and may sound different " +
                "for other people using it to regenerate the MSU.",
                "Dither Setting Warning", TopLevel.GetTopLevel(this) as Window ?? App.MainWindow);
            MsuBasicInfoViewModel.HasSeenDitherWarning = true;
        }
    }
}