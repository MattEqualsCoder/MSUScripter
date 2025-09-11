using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class MsuGenerationWindow : RestorableWindow
{
    private readonly MsuGenerationWindowService? _service;
    private readonly MsuGenerationViewModel? _viewModel;
    
    public MsuGenerationWindow()
    {
        InitializeComponent();
        DataContext = new MsuGenerationViewModel().DesignerExample();
    }
    
    public MsuGenerationWindow(MsuProject project)
    {
        InitializeComponent();
        _service = this.GetControlService<MsuGenerationWindowService>();
        DataContext = _viewModel = _service?.InitializeModel(project);

        if (_service != null)
        {
            _service.PcmGenerationComplete += (_, args) =>
            {
                var model = args.Data;
                Dispatcher.UIThread.Invoke(() =>
                {
                    Title = $"MSU Export - MSU Scripter (Completed in {model.GenerationSeconds} seconds)";
                    if (model.GenerationErrors.Count > 0)
                    {
                        var errorText = string.Join("\r\n", model.GenerationErrors);
                        MessageWindow.ShowErrorDialog($"MSU generation completed with errors:\r\n{errorText}", "MSU Scripter", this);
                    }
                    else
                    {
                        MessageWindow.ShowInfoDialog("MSU Generation Completed Successfully", "MSU Scripter", this);
                    }
                });
            };
        }
    }
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _service?.Cancel();
        base.OnClosing(e);
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel == null)
            {
                return;
            }
            
            var result = await MessageWindow.ShowYesNoDialog("Do you want to compress the MSU into a zip file?",
                "Compress MSU?", this);

            if (result && _viewModel != null)
            {
                var storageItem = await CrossPlatformTools.OpenFileDialogAsync(this,
                    FileInputControlType.SaveFile, "Zip File:*.zip",
                    Path.GetDirectoryName(_viewModel.MsuProject.MsuPath), "Select Desired MSU Zip File");
                var path = storageItem?.Path.LocalPath;
                if (!string.IsNullOrEmpty(path))
                {
                    _service?.SetZipPath(path);
                }
            }
        
            _service?.RunGeneration();
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error running Msu generation");
            await MessageWindow.ShowErrorDialog(_viewModel!.Text.GenericError, _viewModel.Text.GenericErrorTitle, this);
        }
    }

    protected override string RestoreFilePath => Path.Combine(Directories.BaseFolder, "Windows", "msu-pcm-generation-window.json");
    protected override int DefaultWidth => 1024;
    protected override int DefaultHeight => 768;
}