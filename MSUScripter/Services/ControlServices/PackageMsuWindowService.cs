using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using AvaloniaControls.ControlServices;
using AvaloniaControls.Services;
using MSUScripter.ViewModels;

namespace MSUScripter.Services.ControlServices;

public class PackageMsuWindowService : ControlService
{
    private readonly PackageMsuWindowViewModel _model = new();
    
    private readonly HashSet<string> _extensions =
    [
        ".txt",
        ".pcm",
        ".msu",
        ".bat",
        ".yml"
    ];

    private readonly CancellationTokenSource _cts = new();

    public PackageMsuWindowViewModel InitializeModel(MsuProjectViewModel project)
    {
        _model.Project = project;
        return _model;
    }
    
    public void PackageProject(string zipPath)
    {
        ITaskService.Run(() => PackageProjectTask(zipPath));
    }

    public void PackageProjectTask(string zipPath)
    {
        _model.IsRunning = true;
        
        var sb = new StringBuilder();
        sb.AppendLine($"Creating zip file {zipPath}");
        _model.Response = sb.ToString();

        try
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            
            using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            
            foreach (var file in Directory.EnumerateFiles(MsuDirectory, "*.*"))
            {
                if (_cts.Token.IsCancellationRequested)
                {
                    break;
                }
                
                if (!_extensions.Contains(Path.GetExtension(file)))
                {
                    continue;
                }
                
                sb.AppendLine($"... adding {file}");
                _model.Response = sb.ToString();

                try
                {
                    zip.CreateEntryFromFile(file, Path.GetFileName(file));
                }
                catch (Exception e2)
                {
                    sb.AppendLine($"Could not add {file} to zip file: {e2.Message}");
                    _model.Response = sb.ToString();
                    _model.ButtonText = "Close";
                    return;
                }
            }
            
        }
        catch (Exception e)
        {
            sb.AppendLine($"Could not create zip file: {e.Message}");
            _model.Response = sb.ToString();
            _model.ButtonText = "Close";
            return;
        }

        if (_cts.Token.IsCancellationRequested)
        {
            try
            {
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
            }
            catch
            {
                // Do nothing
            }
        }
        else
        {
            _model.IsRunning = false;
            sb.AppendLine("Complete!");
            _model.Response = sb.ToString();
            _model.ButtonText = "Close";
        }
    }

    public void Cancel()
    {
        if (_model.IsRunning)
        {
            _cts.Cancel();
        }
    }

    public string MsuDirectory
    {
        get
        {
            var msuFileInfo = new FileInfo(_model.Project.MsuPath);
            return msuFileInfo.DirectoryName!;
        }
    }
}