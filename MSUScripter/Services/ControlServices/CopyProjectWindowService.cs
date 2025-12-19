using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using AvaloniaControls.Services;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

// ReSharper disable once ClassNeverInstantiated.Global
public class CopyProjectWindowService(ConverterService converterService, ILogger<CopyProjectWindowService> logger) : ControlService
{
    private readonly CopyProjectWindowViewModel _model = new();

    public CopyProjectWindowViewModel InitializeModel()
    {
        return _model;
    }

    public void SetProject(MsuProject project, bool isCopy)
    {
        if (isCopy)
        {
            _model.OriginalProject = project;
            _model.NewProject = converterService.CloneProject(project);
        }
        else
        {
            _model.OriginalProject = converterService.CloneProject(project);
            _model.NewProject = project;
        }

        _model.IsCopy = isCopy;
        _model.ButtonText = isCopy ? "Copy Project" : "Open Project";
        
        var title = string.IsNullOrEmpty(project.BasicInfo.PackName) ? "Project" : project.BasicInfo.PackName;
        _model.Title = isCopy ? $"Copy {title}" : $"Update {title}";

        var paths = new List<CopyProjectViewModel>();

        if (isCopy)
        {
            paths.Add(new CopyProjectViewModel(project.ProjectFilePath));
        }
        
        paths.Add(new CopyProjectViewModel(project.MsuPath));

        if (!string.IsNullOrEmpty(project.BasicInfo.ZeldaMsuPath))
        {
            paths.Add(new CopyProjectViewModel(project.BasicInfo.ZeldaMsuPath));
        }
        
        if (!string.IsNullOrEmpty(project.BasicInfo.MetroidMsuPath))
        {
            paths.Add(new CopyProjectViewModel(project.BasicInfo.MetroidMsuPath));
        }
        
        foreach (var song in project.Tracks.SelectMany(x => x.Songs))
        {
            foreach (var file in song.MsuPcmInfo.GetFiles())
            {
                if (paths.Any(x => x.NewPath == file))
                {
                    continue;
                }
                paths.Add(new CopyProjectViewModel(file));
            }
            
        }

        _model.Paths = paths;
        CheckFiles();
    }
    
    public async Task UpdatePath(CopyProjectViewModel viewModel, IStorageItem file)
    {
        viewModel.NewPath = file.Path.LocalPath;

        var folderPath = (await file.GetParentAsync())!.Path.LocalPath;
        foreach (var folderFile in Directory.GetFiles(folderPath))
        {
            var folderFileInfo = new FileInfo(folderFile);
            
            var otherViewModel = _model.Paths.FirstOrDefault(x => x != viewModel && x.BaseFileName == folderFileInfo.Name && x.PreviousPath == x.NewPath);
            if (otherViewModel == null)
            {
                continue;
            }

            otherViewModel.NewPath = folderFile;
        }

        CheckFiles();
    }

    public void ImportProject()
    {
        if (_model.NewProject == null)
        {
            return;
        }

        if (_model.IsCopy)
        {
            _model.NewProject.ProjectFilePath = _model.Paths
                .First(x => x.Extension.Equals(".msup", StringComparison.OrdinalIgnoreCase)).NewPath;        
        }

        foreach (var path in _model.Paths.Where(x => x.Extension.Equals(".msu", StringComparison.OrdinalIgnoreCase)))
        {
            if (_model.NewProject.MsuPath == path.PreviousPath)
            {
                _model.NewProject.MsuPath = path.NewPath;
            }
            else if (_model.NewProject.BasicInfo.MetroidMsuPath == path.PreviousPath)
            {
                _model.NewProject.BasicInfo.MetroidMsuPath = path.NewPath;
            }
            else if (_model.NewProject.BasicInfo.ZeldaMsuPath == path.PreviousPath)
            {
                _model.NewProject.BasicInfo.ZeldaMsuPath = path.NewPath;
            }
        }
        
        var oldMsuPath = _model.OriginalProject?.MsuPath.Replace(".msu", "", StringComparison.OrdinalIgnoreCase) ?? "";
        var newMsuPath = _model.NewProject?.MsuPath.Replace(".msu", "", StringComparison.OrdinalIgnoreCase) ?? "";
        
        foreach (var path in _model.Paths.Where(x => !x.Extension.Equals(".msu", StringComparison.OrdinalIgnoreCase) && !x.Extension.Equals(".msup", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var song in _model.NewProject!.Tracks.SelectMany(x => x.Songs))
            {
                UpdateSongPaths(song, path, oldMsuPath, newMsuPath);
            }
        }
        
        _model.SavedProject = _model.NewProject;
    }

    public void LogError(Exception e, string message)
    {
        logger.LogError(e, "{Message}", message);
    }
    
    private void UpdateSongPaths(MsuSongInfo song, CopyProjectViewModel update, string oldMsuPath, string newMsuPath)
    {
        song.OutputPath = song.OutputPath?.Replace(oldMsuPath, newMsuPath);
        song.MsuPcmInfo.Output = song.OutputPath;
        if (song.MsuPcmInfo.HasFiles())
        {
            UpdateMsuPcmInfo(song.MsuPcmInfo, update);    
        }
    }

    private void UpdateMsuPcmInfo(MsuSongMsuPcmInfo pcmInfo, CopyProjectViewModel update)
    {
        pcmInfo.File = pcmInfo.File?.Replace(update.PreviousPath, update.NewPath);
        foreach (var subchannel in pcmInfo.SubChannels)
        {
            UpdateMsuPcmInfo(subchannel, update);
        }
        foreach (var subtrack in pcmInfo.SubTracks)
        {
            UpdateMsuPcmInfo(subtrack, update);
        }
    }
    
    private void CheckFiles()
    {
        foreach (var path in _model.Paths)
        {
            if (path.Extension.Equals(".msup", StringComparison.OrdinalIgnoreCase) ||
                path.Extension.Equals(".msu", StringComparison.OrdinalIgnoreCase))
            {
                path.IsValid = !File.Exists(path.NewPath);
                path.Message = path.IsValid ? "" : "File already exists";
            }
            else
            {
                path.IsValid = File.Exists(path.NewPath);
                path.Message = path.IsValid ? "" : "File does not exist";
            }
        }

        _model.IsValid = !_model.IsCopy || _model.Paths.All(x => x.IsValid);
    }
    
}