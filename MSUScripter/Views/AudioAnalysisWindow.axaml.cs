using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Services.ControlServices;
using MSUScripter.Tools;
using MSUScripter.ViewModels;
using System.IO;
using MSUScripter.Configs;

namespace MSUScripter.Views;

public partial class AudioAnalysisWindow : ScalableWindow
{
    private readonly AudioAnalysisWindowService? _service;
    private readonly AudioAnalysisViewModel _model;
    private AudioAnalysisWindow? _compareWindow;

    // ReSharper disable once UnusedMember.Global
    public AudioAnalysisWindow()
    {
        InitializeComponent();
        DataContext = _model = (AudioAnalysisViewModel)new AudioAnalysisViewModel().DesignerExample();
    }
    
    public AudioAnalysisWindow(MsuProject project)
    {
        InitializeComponent();
        _service = this.GetControlService<AudioAnalysisWindowService>();
        DataContext = _model = _service?.InitializeModel(project) ?? new AudioAnalysisViewModel();

        if (_service == null) return;
        
        _service.Completed += (_, _) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Title = $"Audio Analysis - MSU Scripter (Completed in {_model.Duration} seconds)";
            });
        };
    }

    private AudioAnalysisWindow(string msuPath)
    {
        InitializeComponent();
        _service = this.GetControlService<AudioAnalysisWindowService>();
        DataContext = _model = _service?.InitializeModel(msuPath) ?? new AudioAnalysisViewModel();

        if (_service == null) return;

        _service.Completed += (_, _) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Title = $"Audio Analysis - MSU Scripter (Completed in {_model.Duration} seconds)";
            });
        };
    }

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(_model.LoadError))
            {
                await MessageWindow.ShowErrorDialog(_model.LoadError, "Error", this);
                Close();
                return;
            }

            _service?.Run();
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error while running audio analysis");
            await MessageWindow.ShowErrorDialog(_model.Text.GenericError, _model.Text.GenericErrorTitle, this);
        }
    }

    private void RefreshSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service == null) return;
        if (sender is not Button button) return;
        if (button.Tag is not AudioAnalysisSongViewModel song) return;
        _service.RunSong(song);
    }

    private async void CompareButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_service == null) return;

            var documentsPath = await this.GetDocumentsFolderPath();

            if (string.IsNullOrEmpty(documentsPath)) return;

            var file = await CrossPlatformTools.OpenFileDialogAsync(
                parentWindow: this,
                type: FileInputControlType.OpenFile,
                filter: $"MSU File:*.msu",
                path: documentsPath,
                title: $"Select MSU To Compare");

            if (file == null || !File.Exists(file.Path.LocalPath)) return;

            _model.CompareEnabled = false;
            _compareWindow = new AudioAnalysisWindow(file.Path.LocalPath);
            _compareWindow.Closing += CompareWindow_Closing;
            _compareWindow.Show(this);
        }
        catch (Exception ex)
        {
            _service?.LogError(ex, "Error opening second audio analysis window");
            await MessageWindow.ShowErrorDialog(_model.Text.GenericError, _model.Text.GenericErrorTitle, this);
        }
    }

    private void CompareWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        _compareWindow = null;
        _model.CompareEnabled = true;
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _service?.Stop();
        _compareWindow?.Close();
    }
}