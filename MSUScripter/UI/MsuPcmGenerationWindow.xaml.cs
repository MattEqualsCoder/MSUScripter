using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MSUScripter.Configs;
using MSUScripter.Services;

namespace MSUScripter.UI;

public partial class MsuPcmGenerationWindow : Window
{
    private int _numCompleted;
    private readonly int _totalSongs;
    private readonly List<MsuSongInfo> _songs;
    private readonly List<string> _results = new();
    private readonly MsuProject _project;
    private bool _running;
    private int _errors;
    private bool _hasFinished;
    private readonly CancellationTokenSource _cts = new();
    
    public MsuPcmGenerationWindow(MsuProject project, ICollection<MsuSongInfo> songs)
    {
        InitializeComponent();
        _songs = songs.ToList();
        _totalSongs = _songs.Count;
        _project = project;
        MsuPcmProgressBar.Minimum = 0;
        MsuPcmProgressBar.Maximum = _totalSongs;
        
    }

    private void UpdateCurrent()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(UpdateCurrent);
            return;
        }

        MsuPcmProgressBar.Value = _numCompleted;
        var songName = _songs[_numCompleted].SongName ?? _songs[_numCompleted].TrackName;

        CurrentRunningTextBlock.Text = $"Converting Track {_songs[_numCompleted].TrackNumber} - {songName}...";
    }
    
    private void DisplayResults()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(DisplayResults);
            return;
        }

        ErrorTextBlock.Text = string.Join("\r\n", _results);
    }

    private void UpdateComplete()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(UpdateComplete);
            return;
        }

        MsuPcmProgressBar.Value = _totalSongs;
        CurrentRunningTextBlock.Text = "Complete!";
        if (_results.Count == 0)
        {
            ErrorTextBlock.Text = "No errors!";
        }

        if (_errors > 0)
        {
            MessageBox.Show(this, $"There were {_errors} error(s) when running MsuPcm++", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void MsuPcmGenerationWindow_OnActivated(object? sender, EventArgs e)
    {
        if (_running) return;
        _running = true;

        Task.Run(() =>
        {
            foreach (var song in _songs.OrderBy(x => x.TrackNumber).ThenBy(x => x.IsAlt))
            {
                UpdateCurrent();

                if (!MsuPcmService.Instance.CreatePcm(_project, song, out var error))
                {
                    _errors++;
                }
                _results.Add(error!);
                DisplayResults();
                
                _numCompleted++;

                if (_cts.IsCancellationRequested)
                    break;
            }

            UpdateComplete();
            _hasFinished = true;
        }, _cts.Token);
    }

    private void MsuPcmGenerationWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_hasFinished)
        {
            _cts.Cancel();    
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}