using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
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

    private void CreateAltSwapperComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var value = (string)CreateAltSwapperComboBox.SelectedValue == "Yes";
        _parent?.ToggleAltSwpper(value);
    }

    private void CreateSplitSmz3ScriptComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var value = (string)CreateSplitSmz3ScriptComboBox.SelectedValue == "Yes";
        _parent?.ToggleSmz3SplitScript(value);
    }

    private void MetroidMsuPathButton_OnClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new CommonSaveFileDialog()
        {
            EnsurePathExists = true,
            Title = "Select MSU File",
            DefaultExtension = ".msu",
            AlwaysAppendDefaultExtension = true,
            OverwritePrompt = false,
            Filters = { new CommonFileDialogFilter("MSU Files", "*.msu") }
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            var mainMsuPath = new FileInfo(_project!.MsuPath);
            var mainMsuFolder = mainMsuPath.DirectoryName;
            
            var newMsuPath = new FileInfo(dialog.FileName);
            var newMsuFolder = newMsuPath.DirectoryName;

            if (mainMsuFolder != newMsuFolder)
            {
                MessageBox.Show("The Metroid MSU must be located in the same folder as the SMZ3 MSU", "Error");
                return;
            }
            
            MetroidMsuPathTextBox.Text = dialog.FileName;
        }
    }

    private void ZeldaMsuPathButton_OnClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new CommonSaveFileDialog()
        {
            EnsurePathExists = true,
            Title = "Select MSU File",
            DefaultExtension = ".msu",
            AlwaysAppendDefaultExtension = true,
            OverwritePrompt = false,
            Filters = { new CommonFileDialogFilter("MSU Files", "*.msu") }
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            var mainMsuPath = new FileInfo(_project!.MsuPath);
            var mainMsuFolder = mainMsuPath.DirectoryName;
            
            var newMsuPath = new FileInfo(dialog.FileName);
            var newMsuFolder = newMsuPath.DirectoryName;

            if (mainMsuFolder != newMsuFolder)
            {
                MessageBox.Show("The Zelda MSU must be located in the same folder as the SMZ3 MSU", "Error");
                return;
            }
            
            ZeldaMsuPathTextBox.Text = dialog.FileName;
        }
    }
}