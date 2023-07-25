﻿using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using MSUScripter.Configs;
using MSUScripter.Services;
using MSUScripter.UI.Tools;
using MSUScripter.ViewModels;

namespace MSUScripter.UI;

public partial class MsuSongInfoPanel : UserControl
{
    private MsuTrackInfoPanel? _parent;
    private MsuProject _project;
    
    public MsuSongInfoPanel() : this(null, false, new MsuProject())
    {
    }
    
    public MsuSongInfoPanel(MsuTrackInfoPanel? parent, bool isAltTrack, MsuProject project)
    {
        _parent = parent;
        _project = project;
        InitializeComponent();
        DataContext = MsuSongInfo = new MsuSongInfoViewModel();
        if (!isAltTrack)
        {
            OutputPathButton.IsEnabled = false;
        }

        MsuSongMsuPcmInfoPanel.ShowMsuPcmButtons(this);
    }
    
    public MsuSongInfoViewModel MsuSongInfo { get; set; }

    public void ApplyMsuSongMsuPcmInfo(MsuSongMsuPcmInfo data)
    {
        MsuSongMsuPcmInfoPanel.ApplyMsuSongMsuPcmInfo(data);
    }

    public void SetCanDelete(bool canDelete)
    {
        RemoveButton.IsEnabled = canDelete;
    }

    public void GeneratePcmFile(bool asPrimary)
    {
        this.UpdateControlBindings();
        var song = new MsuSongInfo();
        ConverterService.ConvertViewModel(MsuSongInfo, song);
        song.MsuPcmInfo = MsuSongMsuPcmInfoPanel.GetData();

        if (asPrimary)
        {
            var msu = new FileInfo(_project.MsuPath);
            var path = msu.FullName.Replace(msu.Extension, $"-{song.TrackNumber}.pcm");
            song.OutputPath = path;
        }
        
        if (!MsuPcmService.Instance.CreatePcm(_project, song, out var message))
        {
            MessageBox.Show(Window.GetWindow(this)!, message!, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        else
        {
            MessageBox.Show(Window.GetWindow(this)!, message!, "Success!", MessageBoxButton.OK);
        }
    }


    private void OutputPathButton_OnClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new CommonSaveFileDialog()
        {
            EnsurePathExists = true,
            Title = "Select PCM File",
            DefaultExtension = ".pcm",
            AlwaysAppendDefaultExtension = true,
            Filters = { new CommonFileDialogFilter("PCM Files", "*.pcm") }
        };
        
        if (dialog.ShowDialog() != CommonFileDialogResult.Ok || string.IsNullOrEmpty(dialog.FileName)) return;

        MsuSongInfo.OutputPath = dialog.FileName;
    }

    private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_parent != null)
        {
            _parent.RemoveSong(this);
        }
    }
}