using Avalonia.Controls;
using Avalonia.Interactivity;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class SongPanelPromptWindow : Window
{
    private SongPanelPromptWindowViewModel _model;
    
    public SongPanelPromptWindow()
    {
        InitializeComponent();
        DataContext = _model = new SongPanelPromptWindowViewModel();
    }

    private void AddSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(DataContext);
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _model.DontAskAgain = false;
    }
}