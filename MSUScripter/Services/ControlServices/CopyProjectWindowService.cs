using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using AvaloniaControls;
using AvaloniaControls.ControlServices;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class CopyProjectWindowService(ConverterService converterService) : ControlService
{
    private CopyProjectWindowViewModel _model = new();

    public CopyProjectWindowViewModel InitializeModel()
    {
        return _model;
    }

    public void SetProject(MsuProject project, bool isCopy)
    {
        _model.OriginalProject = project;
        _model.ProjectViewModel = converterService.ConvertProject(project);
        _model.ButtonText = isCopy ? "Copy Project" : "Open Project";
        
        var paths = new List<CopyProjectViewModel>
        {
            new(project.ProjectFilePath),
            new(project.MsuPath)
        };

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
        if (_model.ProjectViewModel == null)
        {
            return;
        }

        _model.ProjectViewModel.ProjectFilePath = _model.Paths
            .First(x => x.Extension.Equals(".msup", StringComparison.OrdinalIgnoreCase)).NewPath;

        foreach (var path in _model.Paths.Where(x => x.Extension.Equals(".msu", StringComparison.OrdinalIgnoreCase)))
        {
            if (_model.ProjectViewModel.MsuPath == path.PreviousPath)
            {
                _model.ProjectViewModel.MsuPath = path.NewPath;
            }
            else if (_model.ProjectViewModel.BasicInfo.MetroidMsuPath == path.PreviousPath)
            {
                _model.ProjectViewModel.BasicInfo.MetroidMsuPath = path.NewPath;
            }
            else if (_model.ProjectViewModel.BasicInfo.ZeldaMsuPath == path.PreviousPath)
            {
                _model.ProjectViewModel.BasicInfo.ZeldaMsuPath = path.NewPath;
            }
        }
        
        var oldMsuPath = _model.OriginalProject?.MsuPath.Replace(".msu", "", StringComparison.OrdinalIgnoreCase) ?? "";
        var newMsuPath = _model.ProjectViewModel?.MsuPath.Replace(".msu", "", StringComparison.OrdinalIgnoreCase) ?? "";
        
        foreach (var path in _model.Paths.Where(x => !x.Extension.Equals(".msu", StringComparison.OrdinalIgnoreCase) && !x.Extension.Equals(".msup", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var song in _model.ProjectViewModel!.Tracks.SelectMany(x => x.Songs))
            {
                UpdateSongPaths(song, path, oldMsuPath, newMsuPath);
            }
        }

        _model.NewProject = converterService.ConvertProject(_model.ProjectViewModel!);
        return;
    }
    
    private void UpdateSongPaths(MsuSongInfoViewModel song, CopyProjectViewModel update, string oldMsuPath, string newMsuPath)
    {
        song.OutputPath = song.OutputPath?.Replace(oldMsuPath, newMsuPath);
        if (song.MsuPcmInfo.HasFiles())
        {
            UpdateMsuPcmInfo(song.MsuPcmInfo, update);    
        }
    }

    private void UpdateMsuPcmInfo(MsuSongMsuPcmInfoViewModel pcmInfo, CopyProjectViewModel update)
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

        _model.IsValid = _model.Paths.All(x => x.IsValid);
    }
    
}