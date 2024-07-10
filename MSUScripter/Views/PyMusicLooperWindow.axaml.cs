﻿using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaControls.Controls;
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

    public void SetDetails(MsuProjectViewModel project, MsuSongInfoViewModel song, MsuSongMsuPcmInfoViewModel pcmDetails)
    {
        Result = null;
        this.Find<PyMusicLooperPanel>(nameof(PyMusicLooperPanel))?.UpdateModel(project, song, pcmDetails);
    }
    
    public async Task<PyMusicLooperResultViewModel?> ShowDialog()
    {
        if (App.MainWindow == null) return null;
        return await ShowDialog(App.MainWindow);
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
        _ = this.FindControl<AudioControl>(nameof(AudioControl))?.StopAsync();
    }
}