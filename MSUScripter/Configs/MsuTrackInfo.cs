using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MSUScripter.Models;

namespace MSUScripter.Configs;

public class MsuTrackInfo
{
    public int TrackNumber { get; set; }
    public string TrackName { get; set; } = "";
    public DateTime LastModifiedDate { get; set; }
    public bool IsScratchPad { get; set; }
    
    [SkipConvert]
    public List<MsuSongInfo> Songs { get; set; } = [];

    public MsuSongInfo AddSong(MsuProject project, int index = 0, bool advancedMode = false)
    {
        var msu = new FileInfo(project.MsuPath);
        
        var newSong = new MsuSongInfo
        {
            Id = Guid.NewGuid().ToString("N"),
            TrackNumber = TrackNumber,
            TrackName = TrackName,
            DisplayAdvancedMode = advancedMode
        };

        UpdateSongPath(project, newSong, index);
        
        Songs.Insert(index, newSong);

        for (var i = index + 1; i < Songs.Count; i++)
        {
            var oldIndex = i - 1;
            var oldIsAlt = oldIndex > 0;
            string oldDefaultOutputPath;
            
            if (!oldIsAlt)
            {
                oldDefaultOutputPath = msu.FullName.Replace(msu.Extension, $"-{TrackNumber}.pcm");
            }
            else
            {
                var altSuffix = oldIndex == 1 ? "alt" : $"alt{oldIndex}";
                oldDefaultOutputPath = msu.FullName.Replace(msu.Extension, $"-{TrackNumber}_{altSuffix}.pcm");
            }
            
            if (Songs[i].OutputPath == oldDefaultOutputPath)
            {
                var altSuffix = i == 1 ? "alt" : $"alt{i}";
                var newOutputPath = msu.FullName.Replace(msu.Extension, $"-{TrackNumber}_{altSuffix}.pcm");
                Songs[i].OutputPath = newOutputPath;
                Songs[i].MsuPcmInfo.Output = newOutputPath;
                Songs[i].IsAlt = true;
            }
        }

        return newSong;
    }

    public void RemoveSong(MsuSongInfo song)
    {
        var index = Songs.IndexOf(song);

        for (var i = Songs.Count - 1; i >= index + 1; i--)
        {
            Songs[i].OutputPath = Songs[i - 1].OutputPath;
            Songs[i].MsuPcmInfo.Output = Songs[i - 1].MsuPcmInfo.Output;
            Songs[i].IsAlt = Songs[i - 1].IsAlt;
        }

        Songs.Remove(song);
    }

    public void MoveSong(MsuProject project, MsuSongInfo song, int index)
    {
        var oldTrack = project.Tracks.First(x => x.TrackNumber == song.TrackNumber);

        oldTrack.Songs.Remove(song);
        for (var i = 0; i < oldTrack.Songs.Count; i++)
        {
            UpdateSongPath(project, oldTrack.Songs[i], i);
        }
        
        song.TrackName = TrackName;
        song.TrackNumber = TrackNumber;
        
        Songs.Insert(index, song);
        for (var i = 0; i < Songs.Count; i++)
        {
            UpdateSongPath(project, Songs[i], i);
        }
    }

    private void UpdateSongPath(MsuProject project, MsuSongInfo song, int? index = null)
    {
        index ??= Songs.IndexOf(song);
        
        var msu = new FileInfo(project.MsuPath);
        song.IsAlt = index > 0;

        if (song.TrackNumber >= 9999)
        {
            song.OutputPath = Path.Combine(Directories.TempFolder, project.Id, song.Id, "temp.pcm"); 
        }
        else if (!song.IsAlt)
        {
            song.OutputPath = msu.FullName.Replace(msu.Extension, $"-{song.TrackNumber}.pcm");
        }
        else
        {
            var altSuffix = index == 1 ? "alt" : $"alt{index}";
            song.OutputPath = msu.FullName.Replace(msu.Extension, $"-{song.TrackNumber}_{altSuffix}.pcm");
        }

        song.MsuPcmInfo.Output = song.OutputPath;
    }
}