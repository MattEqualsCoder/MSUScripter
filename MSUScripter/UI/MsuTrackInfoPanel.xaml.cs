using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using MSUScripter.Configs;
using MSUScripter.Services;
using MSUScripter.UI.Tools;

namespace MSUScripter.UI;

public partial class MsuTrackInfoPanel
{
    private MsuProject _project = null!;
    private MsuTrackInfo _trackInfo = null!;
    private List<MsuSongInfoPanel> _songPanels = new();
        
    public MsuTrackInfoPanel()
    {
        InitializeComponent();
    }
    
    public void SetTrackInfo(MsuProject project, MsuTrackInfo trackInfo)
    {
        _project = project;
        _trackInfo = trackInfo;
        foreach (var songInfo in trackInfo.Songs.OrderBy(x => x.IsAlt))
        {
            AddSong(songInfo);
        }
    }

    public void AddSong(MsuSongInfo? songInfo)
    {
        if (songInfo == null)
        {
            songInfo = new MsuSongInfo()
            {
                TrackNumber = _trackInfo.TrackNumber,
                TrackName = _trackInfo.TrackName,
                IsAlt = _songPanels.Count > 0,
            };

            if (!songInfo.IsAlt)
            {
                var msu = new FileInfo(_project.MsuPath);
                songInfo.OutputPath = msu.FullName.Replace(msu.Extension, $"-{_trackInfo.TrackNumber}.pcm");
            }
        }

        var newPanel = new MsuSongInfoPanel(this, songInfo.IsAlt);
        ConverterService.ConvertViewModel(songInfo, newPanel.MsuSongInfo);
        newPanel.ApplyMsuSongMsuPcmInfo(songInfo.MsuPcmInfo);
        SongStackPanel.Children.Add(newPanel);
        _songPanels.Add(newPanel);
        if (_songPanels.Count() > 1)
        {
            _songPanels[0].SetCanDelete(false);
        }
        AddSongButton.Content = "Add Alternate Song";
    }

    public void RemoveSong(MsuSongInfoPanel songPanel)
    {
        SongStackPanel.Children.Remove(songPanel);
        _songPanels.Remove(songPanel);
        if (_songPanels.Count == 0)
        {
            AddSongButton.Content = "Add Song";
        }
        else if (_songPanels.Count == 1)
        {
            _songPanels[0].SetCanDelete(true);
        }
    }

    public void UpdateData()
    {
        var songs = new List<MsuSongInfo>();
        
        foreach (var songPanel in _songPanels)
        {
            songPanel.UpdateControlBindings();
            var song = new MsuSongInfo();
            ConverterService.ConvertViewModel(songPanel.MsuSongInfo, song);
            song.MsuPcmInfo = songPanel.MsuSongMsuPcmInfoPanel.GetData();
            songs.Add(song);
        }

        _trackInfo.Songs = songs;
    }

    private void AddSongButton_OnClick(object sender, RoutedEventArgs e)
    {
        AddSong(null);
    }
}