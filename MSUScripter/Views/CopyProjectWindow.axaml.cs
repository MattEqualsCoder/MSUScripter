using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Configs;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class CopyProjectWindow : ScalableWindow
{
    private readonly CopyProjectWindowViewModel _model = new();
    private readonly CopyProjectWindowService? _service;
    
    public CopyProjectWindow()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            DataContext = new CopyProjectWindowViewModel().DesignerExample();
        }
        else
        {
            _service = this.GetControlService<CopyProjectWindowService>();
            DataContext = _model = _service?.InitializeModel() ?? _model;
        }
    }

    public async Task<MsuProject?> ShowDialog(Window parentWindow, MsuProject project, bool isCopy)
    {
        _service?.SetProject(project, isCopy);
        await ShowDialog(parentWindow);
        return _model.SavedProject;
    }

    private async void UpdatePathButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { Tag: CopyProjectViewModel viewModel })
            {
                return;
            }
        
            var file = await CrossPlatformTools.OpenFileDialogAsync(
                parentWindow: this,
                type: viewModel.IsSongFile ? FileInputControlType.OpenFile : FileInputControlType.SaveFile,
                filter: $"{viewModel.Extension} File:*{viewModel.Extension}",
                path: viewModel.PreviousPath,
                title: $"Select Replacement File for {viewModel.BaseFileName}");
        
            if (string.IsNullOrEmpty(file?.Path.LocalPath))
            {
                return;
            }

            _service?.UpdatePath(viewModel, file);
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error updating path");
            await MessageWindow.ShowErrorDialog(_model.Text.GenericError, _model.Text.GenericErrorTitle, this);
        }
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _model.NewProject = null;
        Close();
    }

    private void ImportProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.ImportProject();
        Close();
    }

}