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
            
            Console.WriteLine($"{Songs[i].SongName} from {oldIndex} => {i}");

            if (!oldIsAlt)
            {
                oldDefaultOutputPath = msu.FullName.Replace(msu.Extension, $"-{TrackNumber}.pcm");
            }
            else
            {
                var altSuffix = oldIndex == 1 ? "alt" : $"alt{oldIndex}";
                oldDefaultOutputPath = msu.FullName.Replace(msu.Extension, $"-{TrackNumber}_{altSuffix}.pcm");
            }
            
            Console.WriteLine($"{Songs[i].OutputPath} == {oldDefaultOutputPath}");

            if (Songs[i].OutputPath == oldDefaultOutputPath)
            {
                var altSuffix = i == 1 ? "alt" : $"alt{i}";
                var newOutputPath = msu.FullName.Replace(msu.Extension, $"-{TrackNumber}_{altSuffix}.pcm");
                Songs[i].OutputPath = newOutputPath;
                Songs[i].MsuPcmInfo.Output = newOutputPath;
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
        var currentIndex = oldTrack.Songs.IndexOf(song);
        var newIndex = index;
        
        for (var i = oldTrack.Songs.Count - 1; i >= currentIndex + 1; i--)
        {
            oldTrack.Songs[i].OutputPath = oldTrack.Songs[i - 1].OutputPath;
            oldTrack.Songs[i].MsuPcmInfo.Output = oldTrack.Songs[i - 1].MsuPcmInfo.Output;
            oldTrack.Songs[i].IsAlt = oldTrack.Songs[i - 1].IsAlt;
        }

        oldTrack.Songs.Remove(song);
        Songs.Insert(newIndex, song);

        var lastIndex = Songs.Count - 1;
        for (var i = newIndex; i < lastIndex; i++)
        {
            Songs[i].OutputPath = Songs[i + 1].OutputPath;
            Songs[i].MsuPcmInfo.Output = Songs[i + 1].MsuPcmInfo.Output;
            Songs[i].IsAlt = Songs[i + 1].IsAlt;
        }

        song.TrackName = TrackName;
        song.TrackNumber = TrackNumber;
        
        UpdateSongPath(project, Songs[lastIndex], lastIndex);
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
            song.OutputPath = msu.FullName.Replace(msu.Extension, $"-{TrackNumber}.pcm");
        }
        else
        {
            var altSuffix = index == 1 ? "alt" : $"alt{index}";
            song.OutputPath = msu.FullName.Replace(msu.Extension, $"-{TrackNumber}_{altSuffix}.pcm");
        }

        song.MsuPcmInfo.Output = song.OutputPath;
    }
}