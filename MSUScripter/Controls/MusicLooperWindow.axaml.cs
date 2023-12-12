using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MSUScripter.ViewModels;

namespace MSUScripter.Controls;

public partial class MusicLooperWindow : Window
{
    private PyMusicLooperPanel? _pyMusicLooperPanel;
    
    public MusicLooperWindow() : this(null)
    {
    }
    
    public MusicLooperWindow(PyMusicLooperPanel? pyMusicLooperPanel)
    {
        _pyMusicLooperPanel = pyMusicLooperPanel;
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
        if (App._mainWindow == null) return null;
        return await ShowDialog(App._mainWindow);
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

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_pyMusicLooperPanel == null) return;
        this.Find<DockPanel>(nameof(DockPanel))!.Children.Add(_pyMusicLooperPanel);
        _pyMusicLooperPanel.Margin = new Thickness(5);
    }
}