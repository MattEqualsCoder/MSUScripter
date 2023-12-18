using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.Services;
using MSUScripter.Tools;

namespace MSUScripter.Controls;

public partial class FileControl : UserControl
{
    public FileControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    public static readonly StyledProperty<string?> FilePathProperty = AvaloniaProperty.Register<FileControl, string?>(
        "FilePath");

    public string? FilePath
    {
        get => GetValue(FilePathProperty);
        set => SetValue(FilePathProperty, value);
    }

    public static readonly StyledProperty<string> ButtonTextProperty = AvaloniaProperty.Register<FileControl, string>(
        "ButtonText", "Browse...");

    public string ButtonText
    {
        get => GetValue(ButtonTextProperty);
        set => SetValue(ButtonTextProperty, value);
    }

    public static readonly StyledProperty<bool> ShowClearButtonProperty = AvaloniaProperty.Register<FileControl, bool>(
        "ShowClearButton", true);

    public bool ShowClearButton
    {
        get => GetValue(ShowClearButtonProperty);
        set => SetValue(ShowClearButtonProperty, value);
    }

    public static readonly StyledProperty<FileInputControlType> FileInputTypeProperty = AvaloniaProperty.Register<FileControl, FileInputControlType>(
        "FileInputType");

    public FileInputControlType FileInputType
    {
        get => GetValue(FileInputTypeProperty);
        set => SetValue(FileInputTypeProperty, value);
    }

    public static readonly StyledProperty<string> FilterProperty = AvaloniaProperty.Register<FileControl, string>(
        "Filter", "All Files:*.*");

    public string Filter
    {
        get => GetValue(FilterProperty);
        set => SetValue(FilterProperty, value);
    }

    public static readonly StyledProperty<string?> DialogTitleProperty = AvaloniaProperty.Register<FileControl, string?>(
        "DialogTitle");

    public string? DialogTitle
    {
        get => GetValue(DialogTitleProperty);
        set => SetValue(DialogTitleProperty, value);
    }
    
    public static readonly StyledProperty<string> WatermarkProperty = AvaloniaProperty.Register<FileControl, string>(
        "Watermark", "");

    public string Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    private void ClearButton_OnClick(object? sender, RoutedEventArgs e)
    {
        FilePath = "";
        OnUpdated?.Invoke(this, new BasicEventArgs(FilePath!));
    }

    public static readonly StyledProperty<bool> WarnOnOverwriteProperty = AvaloniaProperty.Register<FileControl, bool>(
        "WarnOnOverwrite", true);

    public bool WarnOnOverwrite
    {
        get => GetValue(WarnOnOverwriteProperty);
        set => SetValue(WarnOnOverwriteProperty, value);
    }
    
    
    
    public static readonly StyledProperty<string?> ForceExtensionProperty = AvaloniaProperty.Register<FileControl, string?>(
        "ForceExtension", null);

    public string? ForceExtension
    {
        get => GetValue(ForceExtensionProperty);
        set => SetValue(ForceExtensionProperty, value);
    }

    public event EventHandler<BasicEventArgs>? OnUpdated;

    private static IStorageFolder? PreviousFolder;

    private async void BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel == null) return;

        if (PreviousFolder == null)
        {
            if (!string.IsNullOrEmpty(SettingsService.Instance.Settings.PreviousPath))
            {
                PreviousFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(SettingsService.Instance.Settings.PreviousPath);    
            }
            else
            {
                PreviousFolder = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
            }
        }

        if (FileInputType == FileInputControlType.OpenFile)
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = DialogTitle ?? "Select File",
                FileTypeFilter = ParseFilter(),
                SuggestedStartLocation = PreviousFolder,
            });
            
            if (!string.IsNullOrEmpty(files.FirstOrDefault()?.Path.LocalPath))
            {
                PreviousFolder = await files.First().GetParentAsync();
                FilePath = files.FirstOrDefault()?.Path.LocalPath;
                OnUpdated?.Invoke(this, new BasicEventArgs(FilePath!));
            }
        }
        else if (FileInputType == FileInputControlType.SaveFile)
        {
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = DialogTitle ?? "Save File",
                FileTypeChoices = ParseFilter(),
                ShowOverwritePrompt = WarnOnOverwrite,
                SuggestedStartLocation = PreviousFolder,
            });

            if (!string.IsNullOrEmpty(file?.Path.LocalPath))
            {
                FilePath = file.Path.LocalPath;
                PreviousFolder = await file.GetParentAsync();
                
                if (!string.IsNullOrEmpty(ForceExtension) && !string.IsNullOrEmpty(FilePath) && !FilePath.EndsWith($".{ForceExtension}", StringComparison.OrdinalIgnoreCase))
                {
                    FilePath += $".{ForceExtension}";
                }
                
                OnUpdated?.Invoke(this, new BasicEventArgs(FilePath!));
            }
        }
        else if (FileInputType == FileInputControlType.Folder)
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                Title = DialogTitle ?? "Select Folder",
                SuggestedStartLocation = PreviousFolder,
            });
            
            if (!string.IsNullOrEmpty(folders.FirstOrDefault()?.Path.LocalPath))
            {
                PreviousFolder = folders.First();
                FilePath = folders.FirstOrDefault()?.Path.LocalPath;
                OnUpdated?.Invoke(this, new BasicEventArgs(FilePath!));
            }
        }

        if (PreviousFolder != null)
        {
            SettingsService.Instance.Settings.PreviousPath = PreviousFolder.Path.LocalPath;
            try
            {
                SettingsService.Instance.SaveSettings();
            }
            catch (Exception)
            {
                // Do nothing
            }
        }
        
    }

    private List<FilePickerFileType> ParseFilter()
    {
        var toReturn = new List<FilePickerFileType>();

        foreach (var filter in Filter.Split(";"))
        {
            var filterParts = filter.Split(":");
            if (filterParts.Length != 2)
            {
                throw new InvalidOperationException($"{Name} has an invalid filter: {filter}");
            }

            var patterns = filterParts[1].Split(";").Select(x => x.Trim()).ToArray();
            
            toReturn.Add(new FilePickerFileType(filterParts[0].Trim()) { Patterns = patterns });
        }

        return toReturn;
    }
}