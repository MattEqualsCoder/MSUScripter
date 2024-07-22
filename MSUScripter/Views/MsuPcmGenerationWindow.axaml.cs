using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Models;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class MsuPcmGenerationWindow : RestorableWindow
{
    private readonly MsuPcmGenerationWindowService? _service;
    
    public MsuPcmGenerationWindow()
    {
        InitializeComponent();
        DataContext = new MsuPcmGenerationViewModel().DesignerExample();
    }
    
    public MsuPcmGenerationWindow(MsuProjectViewModel project, bool exportYaml)
    {
        InitializeComponent();
        _service = this.GetControlService<MsuPcmGenerationWindowService>();
        DataContext = _service?.InitializeModel(project, exportYaml);

        if (_service != null)
        {
            _service.PcmGenerationComplete += (sender, args) =>
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

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _service?.RunGeneration();
    }

    protected override string RestoreFilePath => Path.Combine(Directories.BaseFolder, "Windows", "msu-pcm-generation-window.json");
    protected override int DefaultWidth => 1024;
    protected override int DefaultHeight => 768;
}