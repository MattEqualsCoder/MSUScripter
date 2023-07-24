using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using MSURandomizerLibrary.Services;
using MSUScripter.Configs;
using MSUScripter.Services;

namespace MSUScripter.UI;

public partial class NewPanel : UserControl
{
    private IMsuTypeService _msuTypeService;
    private ProjectService _projectService;

    public NewPanel(IMsuTypeService msuTypeService, ProjectService projectService)
    {
        _msuTypeService = msuTypeService;
        _projectService = projectService;
        InitializeComponent();
        PopulateMsuTypeComboBox();
    }

    public EventHandler? OnProjectSelected;
    
    public MsuProject? Project { get; set; }

    public void PopulateMsuTypeComboBox()
    {
        MsuTypeComboBox.ItemsSource = _msuTypeService.MsuTypes.OrderBy(x => x.DisplayName).Select(x => x.DisplayName);
    }
    
    private void MsuPcmJsonPathButton_OnClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new CommonSaveFileDialog()
        {
            EnsurePathExists = true,
            Title = "Select JSON File",
            DefaultExtension = ".json",
            OverwritePrompt = false,
            AlwaysAppendDefaultExtension = true,
            Filters = { new CommonFileDialogFilter("JSON Files", "*.json") }
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            MsuPcmJsonPathTextBox.Text = dialog.FileName;
        }
    }

    private void MsuPathButton_OnClick(object sender, RoutedEventArgs e)
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
            MsuPathTextBox.Text = dialog.FileName;
        }
    }

    private void NewButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(MsuPathTextBox.Text))
        {
            MessageBox.Show("Please enter a MSU Path");
            return;
        }

        var msuType = MsuTypeComboBox.SelectedItem as string;
        if (string.IsNullOrEmpty(msuType))
        {
            MessageBox.Show("Please enter a valid MSU Type");
            return;
        }
        
        using var dialog = new CommonSaveFileDialog()
        {
            EnsurePathExists = true,
            Title = "Select Project File",
            DefaultExtension = ".msup",
            AlwaysAppendDefaultExtension = true,
            Filters = { new CommonFileDialogFilter("MSU Project Files", "*.msup") }
        };
        
        if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
        
        var projectPath = dialog.FileName;
        
        Project = _projectService.NewMsuProject(projectPath, msuType, MsuPathTextBox.Text,
            MsuPcmJsonPathTextBox.Text, MsuPcmWorkingDirectoryTextBox.Text);
        OnProjectSelected?.Invoke(this, EventArgs.Empty);
    }

    private void MsuPcmWorkingDirectoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new CommonOpenFileDialog()
        {
            EnsurePathExists = true,
            Title = "Select MsuPcm++ Working Directory",
            IsFolderPicker = true
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            MsuPcmWorkingDirectoryTextBox.Text = dialog.FileName;
        }
    }

    private void OpenButton_OnClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new CommonOpenFileDialog()
        {
            EnsurePathExists = true,
            Title = "Select Project File",
            DefaultExtension = ".msup",
            Filters = { new CommonFileDialogFilter("MSU Project Files", "*.msup") }
        };
        
        if (dialog.ShowDialog() != CommonFileDialogResult.Ok || string.IsNullOrEmpty(dialog.FileName) || !File.Exists(dialog.FileName)) return;

        Project = _projectService.LoadMsuProject(dialog.FileName);
        OnProjectSelected?.Invoke(this, EventArgs.Empty);
    }
}