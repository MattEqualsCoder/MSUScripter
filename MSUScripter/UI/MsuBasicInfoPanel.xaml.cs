using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MSUScripter.UI.Tools;
using MSUScripter.ViewModels;

namespace MSUScripter.UI;

public partial class MsuBasicInfoPanel : UserControl
{
    private EditPanel? _parent;
    
    public MsuBasicInfoPanel(EditPanel? parent)
    { 
        InitializeComponent();
        DataContext = MsuBasicInfo = new MsuBasicInfoViewModel();
        _parent = parent;
    }

    public MsuBasicInfoViewModel MsuBasicInfo { get; set; }


    public void DecimalTextBox_OnPreviewTextInput(object s, TextCompositionEventArgs e) =>
        Helpers.DecimalTextBox_OnPreviewTextInput(s, e);

    public void DecimalTextBox_OnPaste(object s, DataObjectPastingEventArgs e) =>
        Helpers.DecimalTextBox_OnPaste(s, e);

    private void DecimalTextBox_OnLostFocus(object s, RoutedEventArgs e) =>
        Helpers.DecimalTextBox_OnLostFocus(s, e);

    private void IsMsuPcmProjectComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MsuPcmDetailsGroupBox == null) return;
        var value = (string)IsMsuPcmProjectComboBox.SelectedValue == "Yes";
        MsuPcmDetailsGroupBox.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        _parent?.ToggleMsuPcm(value);
    }
}