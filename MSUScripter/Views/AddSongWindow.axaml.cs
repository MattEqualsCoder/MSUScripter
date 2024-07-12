using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaControls;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Events;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class AddSongWindow : ScalableWindow
{
    private bool _forceClosing;
    private readonly AddSongWindowService? _service;

    public AddSongWindow()
    {
        InitializeComponent();
        DataContext = new AddSongWindowViewModel().DesignerExample();
    }
    
    public AddSongWindow(MsuProjectViewModel msuProjectViewModel, int? trackNumber)
    {
        _service = this.GetControlService<AddSongWindowService>();
        var model = _service?.InitializeModel(msuProjectViewModel, trackNumber) ?? new AddSongWindowViewModel();
        DataContext = model;

        model.TrimStartUpdated += (sender, args) =>
        {
            this.FindControl<PyMusicLooperPanel>(nameof(PyMusicLooperPanel))?.UpdateFilterStart(model.TrimStart);
        };
        
        InitializeComponent();
        
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        
        AddHandler(DragDrop.DropEvent, DropFile);
    }

    private void DropFile(object? sender, DragEventArgs e)
    {
        if (_service?.IsPyMusicLooperRunning() == true)
        {
            return;
        }
        
        var file = e.Data.GetFiles()?.FirstOrDefault();
        if (file == null)
        {
            return;
        }

        FilePathUpdated(file.Path.LocalPath);
    }
    
    private void FilePathUpdated(string? path)
    {
        _service?.UpdateFilePath(path);
        _service?.UpdatePyMusicLooperPanel(this.FindControl<PyMusicLooperPanel>(nameof(PyMusicLooperPanel)));
    }

    private void TestAudioLevelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.AnalyzeAudio();
    }

    private void PlaySongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = _service?.PlaySong(false);
    }

    private void TestLoopButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = _service?.PlaySong(true);
    }

    private void StopSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = _service?.StopSong();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _ = _service?.StopSong();
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void AddSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service == null) return;
        await _service.AddSongToProject(this);
        _service.ClearModel();
    }

    private async void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        _ = _service?.StopSong();

        if (_forceClosing || _service?.HasChanges != true)
        {
            return;
        }

        e.Cancel = true;

        if (!await MessageWindow.ShowYesNoDialog(
                "You currently have unsaved changes. Are you sure you want to close this window?", parentWindow: this)) return;
        _forceClosing = true;
        Close();
    }

    private async void AddSongAndCloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service == null) return;
        await _service.AddSongToProject(this);
        _forceClosing = true;
        Close();
    }

    private void FileControl_OnOnUpdated(object? sender, FileControlUpdatedEventArgs e)
    {
        FilePathUpdated(e.Path);
    }

    private void PyMusicLooperPanel_OnOnUpdated(object? sender, PyMusicLooperPanelUpdatedArgs e)
    {
        _service?.UpdateFromPyMusicLooper(e.Result);
    }
}