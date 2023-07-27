using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MSUScripter.Configs;
using MSUScripter.Services;
using MSUScripter.UI.Tools;
using MSUScripter.ViewModels;

namespace MSUScripter.UI;

public partial class MsuBasicInfoPanel : UserControl
{
    private EditPanel? _parent;
    private MsuProject? _project;
    
    public MsuBasicInfoPanel(EditPanel? parent, MsuProject? project)
    { 
        InitializeComponent();
        DataContext = MsuBasicInfo = new MsuBasicInfoViewModel();
        _parent = parent;
        _project = project;
        if (_project != null)
        {
            ConverterService.ConvertViewModel(_project.BasicInfo, MsuBasicInfo);
            MsuBasicInfo.LastModifiedDate = _project.BasicInfo.LastModifiedDate;
        }
    }

    public MsuBasicInfoViewModel MsuBasicInfo { get; set; }

    public bool HasChangesSince(DateTime time)
    {
        return MsuBasicInfo.LastModifiedDate > time;
    }

    public void UpdateData()
    {
        this.UpdateControlBindings();
        if (_project != null)
        {
            ConverterService.ConvertViewModel(MsuBasicInfo, _project.BasicInfo);    
        }
    }

    private void DecimalTextBox_OnPreviewTextInput(object s, TextCompositionEventArgs e) =>
        Helpers.DecimalTextBox_OnPreviewTextInput(s, e);

    private void DecimalTextBox_OnPaste(object s, DataObjectPastingEventArgs e) =>
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