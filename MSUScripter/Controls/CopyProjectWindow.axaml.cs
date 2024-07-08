using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaControls.Controls;
using MSUScripter.Configs;
using MSUScripter.Services;
using MSUScripter.ViewModels;

namespace MSUScripter.Controls;

public partial class CopyProjectWindow : ScalableWindow
{
    private CopyProjectWindowViewModel Model = new();
    
    public CopyProjectWindow()
    {
        InitializeComponent();
        DataContext = Model;
    }

    public async Task<MsuProject?> ShowDialog(Window parentWindow, MsuProject project)
    {
        Model.OriginalProject = project;
        Model.ProjectViewModel = ConverterService.Instance.ConvertProject(project);
        
        var paths = new List<CopyProjectViewModel>();

        paths.Add(new CopyProjectViewModel(project.ProjectFilePath));

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

        Model.Paths = paths;
        CheckFiles();
        
        await ShowDialog(parentWindow);
        return Model.NewProject;
    }

    private void UpdatePathButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: CopyProjectViewModel viewModel })
        {
            return;
        }

        _ = UpdatePath(viewModel);
    }

    private async Task UpdatePath(CopyProjectViewModel viewModel)
    {
        var pattern = string.IsNullOrEmpty(viewModel.Extension)
            ? null
            : new List<FilePickerFileType>()
            {
                new($"{viewModel.Extension} File") { Patterns = new List<string>() { $"*{viewModel.Extension}" } }
            };


        IStorageFile? file;
        
        if (viewModel.Extension.Equals(".msup", StringComparison.OrdinalIgnoreCase) ||
            viewModel.Extension.Equals(".msu", StringComparison.OrdinalIgnoreCase))
        {
            file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = $"Select Replacement File for {viewModel.BaseFileName}",
                FileTypeChoices = pattern,
            });

            if (string.IsNullOrEmpty(file?.Path.LocalPath))
            {
                return;
            }
        }
        else
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = $"Select Replacement File for {viewModel.BaseFileName}",
                FileTypeFilter = pattern,
            });

            if (string.IsNullOrEmpty(files.FirstOrDefault()?.Path.LocalPath))
            {
                return;
            }

            file = files.First();
        }
        
        viewModel.NewPath = file.Path.LocalPath;

        var folderPath = (await file.GetParentAsync())!.Path.LocalPath;
        foreach (var folderFile in Directory.GetFiles(folderPath))
        {
            var folderFileInfo = new FileInfo(folderFile);
            
            var otherViewModel = Model.Paths.FirstOrDefault(x => x != viewModel && x.BaseFileName == folderFileInfo.Name && x.PreviousPath == x.NewPath);
            if (otherViewModel == null)
            {
                continue;
            }

            otherViewModel.NewPath = folderFile;
        }

        CheckFiles();
    }

    private void CheckFiles()
    {
        foreach (var path in Model.Paths)
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

        Model.IsValid = Model.Paths.All(x => x.IsValid);
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ImportProjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Model.ProjectViewModel == null)
        {
            return;
        }

        Model.ProjectViewModel.ProjectFilePath = Model.Paths
            .First(x => x.Extension.Equals(".msup", StringComparison.OrdinalIgnoreCase)).NewPath;

        foreach (var path in Model.Paths.Where(x => x.Extension.Equals(".msu", StringComparison.OrdinalIgnoreCase)))
        {
            if (Model.ProjectViewModel.MsuPath == path.PreviousPath)
            {
                Model.ProjectViewModel.MsuPath = path.NewPath;
            }
            else if (Model.ProjectViewModel.BasicInfo.MetroidMsuPath == path.PreviousPath)
            {
                Model.ProjectViewModel.BasicInfo.MetroidMsuPath = path.NewPath;
            }
            else if (Model.ProjectViewModel.BasicInfo.ZeldaMsuPath == path.PreviousPath)
            {
                Model.ProjectViewModel.BasicInfo.ZeldaMsuPath = path.NewPath;
            }
        }
        
        var oldMsuPath = Model.OriginalProject?.MsuPath.Replace(".msu", "", StringComparison.OrdinalIgnoreCase) ?? "";
        var newMsuPath = Model.ProjectViewModel?.MsuPath.Replace(".msu", "", StringComparison.OrdinalIgnoreCase) ?? "";
        
        foreach (var path in Model.Paths.Where(x => !x.Extension.Equals(".msu", StringComparison.OrdinalIgnoreCase) && !x.Extension.Equals(".msup", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var song in Model.ProjectViewModel!.Tracks.SelectMany(x => x.Songs))
            {
                UpdateSongPaths(song, path, oldMsuPath, newMsuPath);
            }
        }

        Model.NewProject = ConverterService.Instance.ConvertProject(Model.ProjectViewModel!);

        Close();
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
    
}