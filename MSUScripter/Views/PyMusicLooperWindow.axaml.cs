using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaControls.Controls;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class PyMusicLooperWindow : ScalableWindow
{ 
    public PyMusicLooperWindow()
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }

    private PyMusicLooperResultViewModel? Result { get; set; }

    public void UpdateDetails(PyMusicLooperDetails request)
    {
        Result = null;
        this.Find<PyMusicLooperPanel>(nameof(PyMusicLooperPanel))?.UpdateDetails(request);
    }
    
    public async Task<PyMusicLooperResultViewModel?> ShowDialog()
    {
        return await ShowDialog(App.MainWindow);
    }
    
    public async Task<PyMusicLooperResultViewModel?> ShowPyMusicLooperWindowDialog(Window? parentWindow = null)
    {
        return await ShowDialog(parentWindow ?? App.MainWindow);
    }

    private new async Task<PyMusicLooperResultViewModel?> ShowDialog(Window window)
    {
        await base.ShowDialog(window);
        return Result;
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }
    
    private void AcceptButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Result = this.FindControl<PyMusicLooperPanel>(nameof(PyMusicLooperPanel))?.SelectedResult;
        Close();
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        this.FindControl<PyMusicLooperPanel>(nameof(PyMusicLooperPanel))?.Stop();
        _ = this.FindControl<AudioControl>(nameof(AudioControl))?.StopAsync();
    }
}