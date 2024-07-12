using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class AudioAnalysisWindow : ScalableWindow
{
    private readonly AudioAnalysisWindowService? _service;
    
    public AudioAnalysisWindow()
    {
        InitializeComponent();
        DataContext = new AudioAnalysisViewModel().DesignerExample();
    }
    
    public AudioAnalysisWindow(MsuProjectViewModel project)
    {
        InitializeComponent();
        _service = this.GetControlService<AudioAnalysisWindowService>();
        AudioAnalysisViewModel? model;
        DataContext = model = _service?.InitializeModel(project) ?? new AudioAnalysisViewModel();

        if (_service == null) return;
        
        _service.Completed += (sender, args) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Title = $"Audio Analysis - MSU Scripter (Completed in {model.Duration} seconds)";
            });
        };
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _service?.Run();
    }

    private void RefreshSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service == null) return;
        if (sender is not Button button) return;
        if (button.Tag is not AudioAnalysisSongViewModel song) return;
        _service.RunSong(song);
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _service?.Stop();
    }
}