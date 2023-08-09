using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MSUScripter.Configs;
using MSUScripter.Services;

namespace MSUScripter.Controls;

public partial class MsuPcmGenerationWindow : Window
{
    private readonly MsuPcmService? _msuPcmService;
    
    private int _numCompleted;
    private int _totalSongs;
    private List<MsuSongInfo> _songs = new();
    private readonly List<string> _results = new();
    
    private bool _running;
    private int _errors;
    private bool _hasFinished;
    private readonly CancellationTokenSource _cts = new();

    public MsuPcmGenerationWindow() : this(null)
    {
        
    }
    
    public MsuPcmGenerationWindow(MsuPcmService? msuPcmService)
    {
        _msuPcmService = msuPcmService;
        InitializeComponent();
    }

    public MsuProject Project { get; set; } = new();

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _songs = Project.Tracks.SelectMany(x => x.Songs).ToList();
        _totalSongs = _songs.Count;
        Project = Project;
        
        var progressBar = this.Find<ProgressBar>(nameof(MsuPcmProgressBar));
        if (progressBar == null) return;
        progressBar.Minimum = 0;
        progressBar.Maximum = _totalSongs;
        
        if (_running || _msuPcmService == null) return;
        _running = true;
        
        Task.Run(() =>
        {
            foreach (var song in _songs.OrderBy(x => x.TrackNumber).ThenBy(x => x.IsAlt))
            {
                UpdateCurrent();

                if (!_msuPcmService.CreatePcm(Project, song, out var error))
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
    
    private void UpdateCurrent()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(UpdateCurrent);
            return;
        }

        this.Find<ProgressBar>(nameof(MsuPcmProgressBar))!.Value = _numCompleted;
        var songName = _songs[_numCompleted].SongName ?? _songs[_numCompleted].TrackName;

        this.Find<TextBlock>(nameof(CurrentRunningTextBlock))!.Text = $"Converting Track {_songs[_numCompleted].TrackNumber} - {songName}...";
    }
    
    private void DisplayResults()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(DisplayResults);
            return;
        }

        ErrorTextBlock.Text = string.Join("\r\n", _results);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        
        if (!_hasFinished)
        {
            _cts.Cancel();    
        }
    }

    private void UpdateComplete()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(UpdateComplete);
            return;
        }

        var progressBar = this.Find<ProgressBar>(nameof(MsuPcmProgressBar));
        progressBar!.Value = _totalSongs;
        
        var currentRunningTextBlock = this.Find<TextBlock>(nameof(CurrentRunningTextBlock));
        currentRunningTextBlock!.Text = "Complete!";
        if (_results.Count == 0)
        {
            this.Find<TextBlock>(nameof(ErrorTextBlock))!.Text = "No errors!";
        }
        this.Find<Button>(nameof(CloseButton))!.Content = "Close";

        if (_errors > 0)
        {
            _ = new MessageWindow($"There were {_errors} error(s) when running MsuPcm++", MessageWindowType.Error,
                "Error").ShowDialog();
        }
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}