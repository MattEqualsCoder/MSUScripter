using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MSUScripter.ViewModels;

namespace MSUScripter.UI;

public partial class MsuBasicInfoPanel : UserControl
{
    public MsuBasicInfoPanel()
    { 
        InitializeComponent();
        DataContext = MsuBasicInfo = new MsuBasicInfoViewModel();
    }

    public MsuBasicInfoViewModel MsuBasicInfo { get; set; }


    public void DecimalTextBox_OnPreviewTextInput(object s, TextCompositionEventArgs e) =>
        UIHelpers.DecimalTextBox_OnPreviewTextInput(s, e);

    public void DecimalTextBox_OnPaste(object s, DataObjectPastingEventArgs e) =>
        UIHelpers.DecimalTextBox_OnPaste(s, e);

    private void DecimalTextBox_OnLostFocus(object s, RoutedEventArgs e) =>
        UIHelpers.DecimalTextBox_OnLostFocus(s, e);

}