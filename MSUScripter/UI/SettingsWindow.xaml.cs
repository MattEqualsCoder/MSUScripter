using System.IO;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using MSUScripter.Configs;
using MSUScripter.Services;
using MSUScripter.UI.Tools;
using MSUScripter.ViewModels;

namespace MSUScripter.UI;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = Model = new SettingsViewModel();
        Helpers.ConvertViewModel(SettingsService.Settings, Model);
    }

    public SettingsViewModel Model { get; }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        this.UpdateControlBindings();
        Helpers.ConvertViewModel(Model, SettingsService.Settings);
        DialogResult = true;
        Close();
    }

    private void MsuPcmPathButton_OnClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new CommonOpenFileDialog()
        {
            EnsurePathExists = true,
            Title = "Select MsuPcm++ Executable",
            DefaultExtension = ".exe",
            Filters = { new CommonFileDialogFilter("Executable", "*.exe") }
        };

        if (dialog.ShowDialog() != CommonFileDialogResult.Ok || string.IsNullOrEmpty(dialog.FileName) ||
            !File.Exists(dialog.FileName))
        {
            Activate();
            return;
        }
        MsuPcmPathTextBox.Text = dialog.FileName;
        Activate();
    }
}