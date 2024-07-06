using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MSUScripter.ViewModels;

public class PyMusicLooperPanelViewModel : INotifyPropertyChanged
{
    public event EventHandler? FilteredResultsUpdated;
    
    private double _minDurationMultiplier = 0.25;
    public double MinDurationMultiplier
    {
        get => _minDurationMultiplier;
        set => SetField(ref _minDurationMultiplier, value);
    }

    private int? _minLoopDuration;
    public int? MinLoopDuration
    {
        get => _minLoopDuration;
        set => SetField(ref _minLoopDuration, value);
    }
    
    private int? _maxLoopDuration;
    public int? MaxLoopDuration
    {
        get => _maxLoopDuration;
        set => SetField(ref _maxLoopDuration, value);
    }
    
    private int? _approximateStart;
    public int? ApproximateStart
    {
        get => _approximateStart;
        set => SetField(ref _approximateStart, value);
    }
    
    private int? _approximateEnd;
    public int? ApproximateEnd
    {
        get => _approximateEnd;
        set => SetField(ref _approximateEnd, value);
    }
    
    private int? _filterStart;
    public int? FilterStart
    {
        get => _filterStart;
        set
        {
            SetField(ref _filterStart, value);
            FilterResults();
        }
    }

    private int? _filterEnd;
    public int? FilterEnd
    {
        get => _filterEnd;
        set
        {
            SetField(ref _filterEnd, value);
            FilterResults();
        }
    }
    
    private List<PyMusicLooperResultViewModel> _pyMusicLooperResults = new();
    public List<PyMusicLooperResultViewModel> PyMusicLooperResults
    {
        get => _pyMusicLooperResults;
        set 
        { 
            SetField(ref _pyMusicLooperResults, value);
            FilterResults();
        }
    }

    private List<PyMusicLooperResultViewModel> _filteredResults = new();
    public List<PyMusicLooperResultViewModel> FilteredResults
    {
        get => _filteredResults;
        set
        {
            SetField(ref _filteredResults, value);
            Page = 0;
            LastPage = (_filteredResults.Count - 1) / NumPerPage;
        }
    }

    private PyMusicLooperResultViewModel? _selectedResult;

    public PyMusicLooperResultViewModel? SelectedResult
    {
        get => _selectedResult;
        set => SetField(ref _selectedResult, value);
    }

    private MsuSongInfoViewModel _msuSongInfoViewModel = new();
    public MsuSongInfoViewModel MsuSongInfoViewModel
    {
        get => _msuSongInfoViewModel;
        set => SetField(ref _msuSongInfoViewModel, value);
    }
    
    private MsuProjectViewModel _msuProjectViewModel = new();
    public MsuProjectViewModel MsuProjectViewModel
    {
        get => _msuProjectViewModel;
        set => SetField(ref _msuProjectViewModel, value);
    }
    
    private MsuSongMsuPcmInfoViewModel _msuSongMsuPcmInfoViewModel = new();
    public MsuSongMsuPcmInfoViewModel MsuSongMsuPcmInfoViewModel
    {
        get => _msuSongMsuPcmInfoViewModel;
        set => SetField(ref _msuSongMsuPcmInfoViewModel, value);
    }

    private int _numPerPage = 8;

    public int NumPerPage
    {
        get => _numPerPage;
        set => SetField(ref _numPerPage, value);
    }
    
    private int _lastPage = 0;

    public int LastPage
    {
        get => _lastPage;
        set => SetField(ref _lastPage, value);
    }

    private int _page = 0;
    public int Page
    {
        get => _page;
        set
        {
            SetField(ref _page, value);
            OnPropertyChanged(nameof(CurrentPageResults));
            OnPropertyChanged(nameof(CanClickOnPrev));
            OnPropertyChanged(nameof(CanClickOnNext));
        }
    }
    
    private string? _message = "";

    public string? Message
    {
        get => _message;
        set
        {
            SetField(ref _message, value);
            OnPropertyChanged(nameof(DisplayResultsTable));
            OnPropertyChanged(nameof(DisplayMessage));
        }
    }

    public bool DisplayResultsTable => string.IsNullOrEmpty(_message);

    public bool DisplayMessage => !DisplayResultsTable;

    private bool _displayGitHubLink;
    public bool DisplayGitHubLink
    {
        get => _displayGitHubLink;
        set => SetField(ref _displayGitHubLink, value);
    }
    
    private bool _displayOldVersionWarning;
    public bool DisplayOldVersionWarning
    {
        get => _displayOldVersionWarning;
        set => SetField(ref _displayOldVersionWarning, value);
    }
    
    public bool CanClickOnPrev => Page > 0 && !GeneratingPcms;
    
    public bool CanClickOnNext => Page < LastPage && !GeneratingPcms;
    
    private bool _generatingPcms;
    public bool GeneratingPcms
    {
        get => _generatingPcms;
        set
        {
            SetField(ref _generatingPcms, value);   
            OnPropertyChanged(nameof(CanClickOnPrev));
            OnPropertyChanged(nameof(CanClickOnNext));
        }
    }
    
    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set => SetField(ref _isRunning, value);
    }

    public List<PyMusicLooperResultViewModel> CurrentPageResults =>
        _filteredResults.Skip(_page * NumPerPage).Take(NumPerPage).ToList();
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void FilterResults()
    {
        if (_filterStart == null && _filterEnd == null)
        {
            FilteredResults = PyMusicLooperResults;
        }
        else
        {
            FilteredResults = PyMusicLooperResults.Where(x =>
                    (FilterStart == null || x.LoopStart >= FilterStart) &&
                    (FilterEnd == null || x.LoopEnd <= FilterEnd))
                .ToList();
        }
        
        FilteredResultsUpdated?.Invoke(this, EventArgs.Empty);
    }
}