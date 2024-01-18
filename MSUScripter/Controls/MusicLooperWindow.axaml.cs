using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MSUScripter.Services;
using MSUScripter.ViewModels;

namespace MSUScripter.Controls;

public partial class MusicLooperWindow : Window
{
    private readonly PyMusicLooperPanel? _pyMusicLooperPanel;
    private readonly AudioControl? _audioControl = null!;
    private readonly IAudioPlayerService? _audioPlayerService;
    
    public MusicLooperWindow() : this(null, null, null)
    {
    }
    
    public MusicLooperWindow(PyMusicLooperPanel? pyMusicLooperPanel, AudioControl? audioControl, IAudioPlayerService? audioPlayerService)
    {
        _pyMusicLooperPanel = pyMusicLooperPanel;
        _audioControl = audioControl;
        _audioPlayerService = audioPlayerService;
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }
    
    public PyMusicLooperResultViewModel? Result { get; set; }

    public void SetDetails(MsuProjectViewModel project, MsuSongInfoViewModel song)
    {
        if (_pyMusicLooperPanel == null)
        {
            return;
        }

        Result = null;

        _pyMusicLooperPanel.Model = new PyMusicLooperPanelViewModel()
        {
            MsuProjectViewModel = project,
            MsuSongInfoViewModel = song
        };
    }
    
    public async Task<PyMusicLooperResultViewModel?> ShowDialog()
    {
        if (App.MainWindow == null) return null;
        return await ShowDialog(App.MainWindow);
    }
    
    public new async Task<PyMusicLooperResultViewModel?> ShowDialog(Window window)
    {
        if (_pyMusicLooperPanel == null) return null;
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
        Result = _pyMusicLooperPanel?.Model.SelectedResult;
        Close();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_pyMusicLooperPanel == null) return;
        this.Find<DockPanel>(nameof(DockPanel))!.Children.Add(_pyMusicLooperPanel);
        _pyMusicLooperPanel.Margin = new Thickness(5);

        if (_audioControl != null)
        {
            AudioPanelParent.Children.Add(_audioControl);    
        }
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        _ = _audioPlayerService?.StopSongAsync();
    }
}