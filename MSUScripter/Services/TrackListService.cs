using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using MSURandomizerLibrary.Services;
using MSUScripter.Configs;
using MSUScripter.Tools;

namespace MSUScripter.Services;

public class TrackListService(ILogger<TrackListService> logger, IMsuTypeService msuTypeService)
{
    public void WriteTrackListFile(MsuProject project)
    {
        var msuFileInfo = new FileInfo(project.MsuPath);
        var tracklistPath = Path.Combine(msuFileInfo.DirectoryName!, "Track List.txt");
        var sb = new StringBuilder();

        var title = $"{project.BasicInfo.PackName}";
        if (!title.Contains(" MSU", StringComparison.OrdinalIgnoreCase))
        {
            title += " MSU Pack";
        }
        if (!string.IsNullOrEmpty(project.BasicInfo.PackCreator))
        {
            title += $" by {project.BasicInfo.PackCreator}";
        }

        sb.AppendLine(title);
        sb.AppendLine(new string('-', title.Length));
        sb.AppendLine();
        
        if (project.BasicInfo.CreateSplitSmz3Script)
        {
            var zeldaTrackRange = (0, 98);
            var metroidTrackRange = (101, 199);
            var zeldaTrackModifier = 0;
            var metroidTrackModifier = -100;

            if (project.MsuType == msuTypeService.GetSMZ3LegacyMSUType())
            {
                zeldaTrackRange = (101, 199);
                metroidTrackRange = (0, 98);
                zeldaTrackModifier = -100;
                metroidTrackModifier = 0;
            }

            var zeldaTracks = project.Tracks.Where(t =>
                t.TrackNumber >= zeldaTrackRange.Item1 && t.TrackNumber <= zeldaTrackRange.Item2 && t.Songs.Any());

            var metroidTracks = project.Tracks.Where(t =>
                t.TrackNumber >= metroidTrackRange.Item1 && t.TrackNumber <= metroidTrackRange.Item2 &&
                t.Songs.Any());

            var smz3Tracks = project.Tracks.Where(t =>
                !(t.TrackNumber >= zeldaTrackRange.Item1 && t.TrackNumber <= zeldaTrackRange.Item2) &&
                !(t.TrackNumber >= metroidTrackRange.Item1 && t.TrackNumber <= metroidTrackRange.Item2) &&
                t.Songs.Any());

            if (project.BasicInfo.TrackList == TrackListType.List)
            {
                sb.AppendLine("Zelda Tracks:");
                sb.AppendLine();
                AppendTrackList(zeldaTracks, sb, zeldaTrackModifier);

                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine();

                sb.AppendLine("Metroid Tracks:");
                sb.AppendLine();
                AppendTrackList(metroidTracks, sb, metroidTrackModifier);

                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine();

                sb.AppendLine("SMZ3 Tracks:");
                sb.AppendLine();
                AppendTrackList(smz3Tracks, sb, 0);
            }
            else
            {
                var songs = project.Tracks.SelectMany(x => x.Songs).ToList();
                var numberLength = songs.Any(x => x.IsAlt) ? 12 : 6;
                var trackLength = project.Tracks.Max(x => x.TrackName.Length) + 4;
                var albumLength = songs.Max(x => string.IsNullOrEmpty(x.Album) ? 0 : x.Album.CleanString().Length + 4);
                var songLength = songs.Max(x => string.IsNullOrEmpty(x.SongName) ? 0 : x.SongName.CleanString().Length + 4);
                var artistLength = songs.Max(x => string.IsNullOrEmpty(x.Artist) ? 0 : x.Artist.CleanString().Length + 4);
                
                sb.AppendLine("Zelda Tracks:");
                sb.AppendLine();
                AppendTrackTable(zeldaTracks, sb, zeldaTrackModifier, numberLength, trackLength, albumLength, songLength, artistLength); 
                
                sb.AppendLine();

                sb.AppendLine("Metroid Tracks:");
                sb.AppendLine();
                AppendTrackTable(metroidTracks, sb, metroidTrackModifier, numberLength, trackLength, albumLength, songLength, artistLength);
                
                sb.AppendLine();

                sb.AppendLine("SMZ3 Tracks:");
                sb.AppendLine();
                AppendTrackTable(smz3Tracks, sb, 0, numberLength, trackLength, albumLength, songLength, artistLength);
            }
        }
        else
        {
            var allTracks = project.Tracks.Where(t => t.Songs.Any());
            
            if (project.BasicInfo.TrackList == TrackListType.List)
            {
                AppendTrackList(allTracks, sb, 0);    
            }
            else
            {
                var songs = project.Tracks.SelectMany(x => x.Songs).ToList();
                var numberLength = songs.Any(x => x.IsAlt) ? 12 : 6;
                var trackLength = project.Tracks.Max(x => x.TrackName.Length) + 3;
                var albumLength = songs.Max(x => string.IsNullOrEmpty(x.Album) ? 0 : x.Album.CleanString().Length + 3);
                var songLength = songs.Max(x => string.IsNullOrEmpty(x.SongName) ? 0 : x.SongName.CleanString().Length + 3);
                var artistLength = songs.Max(x => string.IsNullOrEmpty(x.Artist) ? 0 : x.Artist.CleanString().Length + 3);
                AppendTrackTable(allTracks, sb, 0, numberLength, trackLength, albumLength, songLength, artistLength);   
            }
        }

        try
        {
            File.WriteAllText(tracklistPath, sb.ToString());
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unable to write tracklist file");
        }
    }

    private void AppendTrackList(IEnumerable<MsuTrackInfo> tracks, StringBuilder sb, int trackModifier)
    {
        foreach (var track in tracks.OrderBy(x => x.TrackNumber))
        {
            sb.AppendLine($"Track {track.TrackNumber + trackModifier} ({track.TrackName})");

            foreach (var song in track.Songs.OrderBy(x => x.IsAlt))
            {
                sb.AppendLine(GetTrackListSongInfo(song));
            }

            sb.AppendLine();
        }
    }

    private string GetTrackListSongInfo(MsuSongInfo song)
    {
        var songInfo = "";
            
        if (!string.IsNullOrEmpty(song.Album))
        {
            songInfo += $"{song.Album?.CleanString()} - ";
        }

        songInfo += song.SongName?.CleanString();

        if (!string.IsNullOrEmpty(song.Artist))
        {
            songInfo += $" ({song.Artist?.CleanString()})";
        }

        if (song.IsAlt)
        {
            songInfo += " (Alt)";
        }

        return songInfo;
    }
    
    private void AppendTrackTable(IEnumerable<MsuTrackInfo> tracks, StringBuilder sb, int trackModifier, int numberLength, int trackLength, int albumLength, int songLength, int artistLength)
    {
        var headerNumber = "###".PadRight(numberLength);
        var headerTrack = "Track".PadRight(trackLength);
        var headerAlbum = albumLength > 0 ? "Album".PadRight(albumLength) : "";
        var headerSong = songLength > 0 ? "Song".PadRight(songLength) : "";
        var headerArtist = artistLength > 0 ? "Artist".PadRight(artistLength) : "";
        var header = $"{headerNumber}{headerTrack}{headerAlbum}{headerSong}{headerArtist}";
        sb.AppendLine(header);
        sb.AppendLine(new string('-', header.Length));
        foreach (var track in tracks.OrderBy(x => x.TrackNumber))
        {
            foreach (var song in track.Songs.OrderBy(x => x.IsAlt))
            {
                sb.AppendLine(GetTrackTableSongInfo(track, song, trackModifier, numberLength, trackLength, albumLength, songLength, artistLength));
            }
        }
    }
    
    private string GetTrackTableSongInfo(MsuTrackInfo track, MsuSongInfo song, int trackModifier, int numberLength, int trackLength, int albumLength, int songLength, int artistLength)
    {
        var trackNumber = (track.TrackNumber + trackModifier).ToString();
        if (song.IsAlt)
        {
            trackNumber += " (Alt)";
        }

        var songInfo = trackNumber.PadRight(numberLength);
        songInfo += track.TrackName.PadRight(trackLength);
            
        if (albumLength > 0)
        {
            songInfo += (song.Album?.CleanString() ?? "").PadRight(albumLength);
        }

        if (songLength > 0)
        {
            songInfo += (song.SongName?.CleanString() ?? "").PadRight(songLength);
        }
        
        if (artistLength > 0)
        {
            songInfo += (song.Artist?.CleanString() ?? "").PadRight(artistLength);
        }

        return songInfo;
    }
}