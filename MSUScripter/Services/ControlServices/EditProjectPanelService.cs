using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using AvaloniaControls;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Models;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class EditProjectPanelService(
    ProjectService projectService,
    MsuPcmService msuPcmService,
    IAudioPlayerService audioService,
    ConverterService converterService,
    TrackListService trackListService,
    StatusBarService statusBarService,
    AudioAnalysisService audioAnalysisService) : ControlService
{
    private EditProjectPanelViewModel _model = new();
    private bool _isFirstInit = true;
    private readonly Timer _backupTimer = new(TimeSpan.FromSeconds(60));
    
    public EditProjectPanelViewModel InitializeModel(MsuProject project)
    {
        if (_isFirstInit)
        {
            statusBarService.StatusBarTextUpdated += (sender, args) =>
            {
                _model.StatusBarText = args.Data;
            };
            
            _backupTimer.Elapsed += BackupTimerOnElapsed;
        }
        
        var projectModel = converterService.ConvertProject(project);
        
        foreach (var songViewModel in projectModel.Tracks.SelectMany(x => x.Songs))
        {
            songViewModel.MsuPcmInfo.UpdateHertzWarning(audioAnalysisService.GetAudioSampleRate(songViewModel.MsuPcmInfo.File));
            songViewModel.MsuPcmInfo.UpdateMultiWarning();
            songViewModel.MsuPcmInfo.UpdateSubTrackSubChannelWarning();
        }

        _model = new EditProjectPanelViewModel
        {
            MsuProject = project,
            MsuProjectViewModel = projectModel,
            Tracks = projectModel.Tracks.OrderBy(x => x.TrackNumber).ToList(),
            LastAutoSave =  projectModel.LastSaveTime
        };

        List<ComboBoxAndSearchItem> searchItems =
        [
            new ComboBoxAndSearchItem(0, "MSU Details"),
            new ComboBoxAndSearchItem(1, "Track Overview"),
        ];
        searchItems.AddRange(_model.Tracks.Select((t, i) => new ComboBoxAndSearchItem(i + 2, t.ToString())));
        _model.TrackSearchItems = searchItems;
        
        _backupTimer.Start();

        if (project.IsNewProject)
        {
            statusBarService.UpdateStatusBar("Created New Project");
        }
        else
        {
            statusBarService.UpdateStatusBar("Loaded Project");
        }

        return _model;
    }

    public void IncrementPage(int mod)
    {
        _model.PageNumber = Math.Clamp(_model.PageNumber + mod, 0, _model.Tracks.Count + 1);
    }
    
    public void SetPage(int page)
    {
        _model.PageNumber = Math.Clamp(page, 0, _model.Tracks.Count + 1);
    }
    
    public void SetToTrackPage(int trackNumber)
    {
        _model.PageNumber = _model.Tracks.IndexOf(_model.Tracks.First(x => x.TrackNumber == trackNumber)) + 2;
    }

    public void SaveProject()
    {
        if (_model.MsuProjectViewModel == null) return;
        var project = converterService.ConvertProject(_model.MsuProjectViewModel!);
        projectService.SaveMsuProject(project, false);
        _model.MsuProjectViewModel.LastSaveTime = project.LastSaveTime;
        _model.LastAutoSave = project.LastSaveTime;
    }
    
    public string? ExportYaml(MsuProject? project = null)
    {
        if (_model.MsuProjectViewModel?.BasicInfo.WriteYamlFile != true) return null;
        project ??= converterService.ConvertProject(_model.MsuProjectViewModel);
        projectService!.ExportMsuRandomizerYaml(project, out var error);
        return error;
    }

    public string? ValidateProject()
    {
        if (_model.MsuProjectViewModel == null) return null;
        projectService.ValidateProject(_model.MsuProjectViewModel, out var message);
        return message;
    }
    
    public void WriteTrackList(MsuProject? project = null)
    {
        if (_model.MsuProjectViewModel == null) return;
        project ??= converterService.ConvertProject(_model.MsuProjectViewModel);
        trackListService.WriteTrackListFile(project);
    }

    public void WriteTrackJson()
    {
        if (_model.MsuProjectViewModel == null) return;
        var project = converterService.ConvertProject(_model.MsuProjectViewModel);
        msuPcmService.ExportMsuPcmTracksJson(false, project);
    }
    
    public string? WriteSwapperBatchFiles()
    {
        if (_model.MsuProjectViewModel == null) return null;
        var project = converterService.ConvertProject(_model.MsuProjectViewModel);
        
        var extraProjects = new List<MsuProject>();

        if (project.BasicInfo.CreateSplitSmz3Script)
        {
            extraProjects = projectService.GetSmz3SplitMsuProjects(project, out _, out var error).ToList();
            if (!string.IsNullOrEmpty(error))
            {
                return error;
            }
        }

        return !projectService.CreateAltSwapperFile(project, extraProjects)
            ? "Could not create alt swapper bat file. Project file may be corrupt. Verify output pcm file paths."
            : null;
    }
    
    public string? CreateSmz3SplitBatchFile()
    {
        if (_model.MsuProjectViewModel == null) return null;
        var project = converterService.ConvertProject(_model.MsuProjectViewModel);
        projectService.GetSmz3SplitMsuProjects(project, out var conversions, out var error);
        if (!string.IsNullOrEmpty(error))
        {
            return error;
        }

        return !projectService.CreateSmz3SplitScript(project, conversions)
            ? "Insufficient tracks to create the SMZ3 to ALttP and SM MSUs batch file."
            : null;
    }

    public string? SetupForMsuGenerationWindow()
    {
        if (_model.MsuProjectViewModel == null) return null;
        var project = converterService.ConvertProject(_model.MsuProjectViewModel);

        if (!projectService.CreateMsuFiles(project))
        {
            return "Unable to create MSU files";
        }
        
        var extraProjects = new List<MsuProject>();

        if (project.BasicInfo.CreateSplitSmz3Script)
        {
            extraProjects = projectService.GetSmz3SplitMsuProjects(project, out var conversions, out var error).ToList();
            
            if (!string.IsNullOrEmpty(error))
            {
                return error;
            }
            
            projectService.CreateSmz3SplitScript(project, conversions);
        }
        
        if (project.BasicInfo.CreateAltSwapperScript)
        {
            if (!projectService.CreateAltSwapperFile(project, extraProjects))
            {
                return "Could not create alt swapper bat file. Project file may be corrupt. Verify output pcm file paths.";
            }
        }

        if (project.BasicInfo.TrackList != TrackListType.Disabled)
        {
            WriteTrackList(project);
        }
        
        if (!project.BasicInfo.IsMsuPcmProject)
        {
            return ExportYaml(project);
        }
        
        msuPcmService.ExportMsuPcmTracksJson(false, project);

        return null;
    }

    public bool OpenFolder()
    {
        return _model.MsuProjectViewModel == null ||
               CrossPlatformTools.OpenDirectory(_model.MsuProjectViewModel.MsuPath, true);
    }

    public void UpdateExportMenuOptions()
    {
        _model.DisplayAltSwapperExportButton =
            _model.CreateAltSwapper && _model.MsuProjectViewModel?.Tracks.Any(x => x.Songs.Count > 1) == true;
    }

    public void Disable()
    {
        _backupTimer.Stop();
        _ = audioService.StopSongAsync();
    }

    public bool ArePcmFilesUpToDate()
    {
        return _model.MsuProjectViewModel?.BasicInfo.IsMsuPcmProject == true && _model.MsuProjectViewModel.Tracks
            .SelectMany(x => x.Songs).Any(x => x.HasChangesSince(x.LastGeneratedDate));
    }
    
    public bool HasPendingChanges() => _model.MsuProjectViewModel?.HasPendingChanges() == true;

    public MsuProjectViewModel? MsuProjectViewModel => _model.MsuProjectViewModel;
    
    private void BackupTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_model.MsuProjectViewModel?.HasChangesSince(_model.LastAutoSave) != true)
            return;
        var backupProject = converterService.ConvertProject(_model.MsuProjectViewModel);
        projectService.SaveMsuProject(backupProject, true);
        _model.LastAutoSave = DateTime.Now;
        _model.StatusBarText = "Created Project Backup";
    }
}