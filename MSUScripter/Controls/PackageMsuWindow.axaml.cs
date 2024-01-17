using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MSUScripter.ViewModels;
using Path = System.IO.Path;

namespace MSUScripter.Controls;

public partial class PackageMsuWindow : Window
{
    private readonly HashSet<string> _extensions = new()
    {
        ".txt",
        ".pcm",
        ".msu",
        ".bat",
        ".yml"
    };

    private bool _isRunning = false;
    
    private readonly CancellationTokenSource _cts = new();
    
    public PackageMsuWindow()
    {
        InitializeComponent();
        DataContext = Model = new PackageMsuViewModel();
    }

    public PackageMsuWindow(MsuProjectViewModel project)
    {
        DataContext = Model = new PackageMsuViewModel()
        {
            Project = project
        };
        InitializeComponent();
    }

    private async Task PackageTask()
    {
        var msuFileInfo = new FileInfo(Model.Project.MsuPath);
        var msuDirectory = msuFileInfo.DirectoryName!;
        
        var zipPath = await GetZipPath(msuDirectory);

        if (string.IsNullOrEmpty(zipPath))
        {
            Close();
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Creating zip file {zipPath}");
        Model.Response = sb.ToString();

        try
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            
            using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            
            foreach (var file in Directory.EnumerateFiles(msuDirectory, "*.*"))
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
                Model.Response = sb.ToString();

                try
                {
                    zip.CreateEntryFromFile(file, Path.GetFileName(file));
                }
                catch (Exception e2)
                {
                    sb.AppendLine($"Could not add {file} to zip file: {e2.Message}");
                    Model.Response = sb.ToString();
                    Model.ButtonText = "Close";
                    return;
                }
            }
            
        }
        catch (Exception e)
        {
            sb.AppendLine($"Could not create zip file: {e.Message}");
            Model.Response = sb.ToString();
            Model.ButtonText = "Close";
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
            _isRunning = false;
            sb.AppendLine("Complete!");
            Model.Response = sb.ToString();
            Model.ButtonText = "Close";
        }
    }

    private async Task<string?> GetZipPath(string msuDirectory)
    {
        var path = await StorageProvider.TryGetFolderFromPathAsync(msuDirectory);
        
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Select Desired MSU Zip File",
            FileTypeChoices = new List<FilePickerFileType>()
            {
                new("zip file") { Patterns = new List<string>() { "*.zip" } }
            },
            ShowOverwritePrompt = true,
            SuggestedStartLocation = path,
        });

        return file?.Path.LocalPath;
    }

    protected PackageMsuViewModel Model { get; set; }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _isRunning = true;
        Task.Run(PackageTask);
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isRunning)
        {
            _cts.Cancel();
        }
    }
}