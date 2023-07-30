using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using MSUScripter.Configs;
using MSUScripter.Services;
using MSUScripter.UI.Tools;
using MSUScripter.ViewModels;

namespace MSUScripter.UI;

public partial class MsuSongInfoPanel : UserControl
{
    private readonly MsuTrackInfoPanel? _parent;
    private readonly MsuProject _project;
    
    public MsuSongInfoPanel() : this(null, null, new MsuProject())
    {
    }
    
    public MsuSongInfoPanel(MsuTrackInfoPanel? parent, MsuSongInfo? songInfo, MsuProject project)
    {
        _parent = parent;
        _project = project;
        InitializeComponent();
        DataContext = MsuSongInfo = new MsuSongInfoViewModel();
        
        if (songInfo != null)
        {
            if (!songInfo.IsAlt)
                OutputPathButton.IsEnabled = false;
            ConverterService.ConvertViewModel(songInfo, MsuSongInfo);
            MsuSongInfo.LastModifiedDate = songInfo.LastModifiedDate;
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
        RemoveButton.Opacity = canDelete ? 1 : 0.25;
    }

    public bool GeneratePcmFile(bool asPrimary)
    {
        if (MsuPcmService.Instance.IsGeneratingPcm) return false;
        EditPanel.Instance?.UpdateStatusBarText("Generating PCM");
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
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(Window.GetWindow(this)!, message!, "MsuPcm++ Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                EditPanel.Instance?.UpdateStatusBarText("MsuPcm++ Error");
            });
            return false;
        }
        
        MsuSongInfo.LastGeneratedDate = DateTime.Now;
        EditPanel.Instance?.UpdateStatusBarText("PCM Generated");
        return true;
    }
    
    public void ToggleMsuPcm(bool enable)
    {
        MsuSongMsuPcmInfoPanel.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
    }

    public void ImportAudioMetadata(string file, bool force = false)
    {
        var metadata =  _parent?.GetAudioMetadata(file);
        if (metadata?.HasData != true) return;
        if (force || string.IsNullOrEmpty(MsuSongInfo.SongName) || MsuSongInfo.SongName.StartsWith("Track #"))
            MsuSongInfo.SongName = metadata.SongName;
        if (force || (string.IsNullOrEmpty(MsuSongInfo.Artist) && !string.IsNullOrEmpty(metadata.Artist)))
            MsuSongInfo.Artist = metadata.Artist;
        if (force || (string.IsNullOrEmpty(MsuSongInfo.Album) && !string.IsNullOrEmpty(metadata.Album)))
            MsuSongInfo.Album = metadata.Album;
        if (force || (string.IsNullOrEmpty(MsuSongInfo.Url) && !string.IsNullOrEmpty(metadata.Url)))
            MsuSongInfo.Url = metadata.Url;
    }
    
    public bool HasChangesSince(DateTime time)
    {
        if (MsuSongInfo.LastModifiedDate > time)
        {
            return true;
        }

        if (IsMsuPcmProject)
        {
            return MsuSongMsuPcmInfoPanel.HasChangesSince(time);
        }

        return false;
    }

    public bool IsMsuPcmProject => _project.BasicInfo.IsMsuPcmProject;

    public DateTime LastPcmGenerationTime => MsuSongInfo.LastGeneratedDate;

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
        if (_parent == null) return;
        var result = MessageBox.Show("Are you sure you want to remove this song?", "Warning", MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
            _parent.RemoveSong(this);
    }

    private void ImportSongMetadataButton_OnClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new CommonOpenFileDialog()
        {
            EnsurePathExists = true,
            Title = "Select Music File",
            Filters = { new CommonFileDialogFilter("Audio Files", "*.mp3;*.flac") }
        };
        
        if (dialog.ShowDialog() != CommonFileDialogResult.Ok || string.IsNullOrEmpty(dialog.FileName) || !File.Exists(dialog.FileName)) return;

        ImportAudioMetadata(dialog.FileName, true);
    }
    
    private void PlaySongButton_OnClick(object sender, RoutedEventArgs e)
    {
        Task.Run(() => MsuSongMsuPcmInfoPanel.PlaySong(false));
    }

    private void TestLoopButton_OnClick(object sender, RoutedEventArgs e)
    {
        Task.Run(() => MsuSongMsuPcmInfoPanel.PlaySong(true));
    }
}