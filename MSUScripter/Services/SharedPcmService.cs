using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.ViewModels;

namespace MSUScripter.Services;

public class SharedPcmService(MsuPcmService msuPcmService, IAudioPlayerService audioPlayerService, ConverterService converterService)
{
    public async Task<GeneratePcmFileResponse> GeneratePcmFile(MsuSongInfoViewModel songInfo, bool asPrimary, bool asEmpty)
    {
        if (msuPcmService.IsGeneratingPcm)
        {
            return new GeneratePcmFileResponse(false, false, "Currently generating another file", null);
        }

        if (songInfo.Track.IsScratchPad && songInfo.OutputPath?.StartsWith(Directories.TempFolder) != true)
        {
            var msuFile = new FileInfo(songInfo.Project.MsuPath);
            var pcmFileName = msuFile.Name.Replace(msuFile.Extension, $"-{Guid.NewGuid()}.pcm");
            songInfo.OutputPath = Path.Combine(Directories.TempFolder, pcmFileName);
        }
        
        await audioPlayerService.StopSongAsync(null, true);

        if (asEmpty)
        {
            var emptySong = new MsuSongInfo();
            converterService.ConvertViewModel(songInfo, emptySong);
            var successful = msuPcmService.CreateEmptyPcm(emptySong);

            return !successful
                ? new GeneratePcmFileResponse(false, false, "Currently generating another file", null)
                : new GeneratePcmFileResponse(true, true, "Successful", songInfo.OutputPath);
        }
        
        if (!songInfo.HasFiles())
        {
            return new GeneratePcmFileResponse(false, false, "No files specified to generate into a pcm file", null);
        }
        
        var song = new MsuSongInfo();
        converterService.ConvertViewModel(songInfo, song);
        converterService.ConvertViewModel(songInfo.MsuPcmInfo, song.MsuPcmInfo);
        var tempProject = converterService.ConvertProject(songInfo.Project);

        if (asPrimary)
        {
            var msu = new FileInfo(songInfo.Project.MsuPath);
            var path = msu.FullName.Replace(msu.Extension, $"-{song.TrackNumber}.pcm");
            song.OutputPath = path;
        }

        var response = await msuPcmService.CreatePcm(true, tempProject, song);
        if (!response.Successful)
        {
            if (response.GeneratedPcmFile && songInfo.Project.IgnoreWarnings.Contains(song.OutputPath ?? ""))
            {
                songInfo.LastGeneratedDate = DateTime.Now;
                return new GeneratePcmFileResponse(true, true, null, song.OutputPath);
            }
            else
            {
                return response;
            }
        }
        
        songInfo.LastGeneratedDate = DateTime.Now;
        return response;
    }
    
    public async Task<GeneratePcmFileResponse> GeneratePcmFile(MsuProject project, MsuSongInfo songInfo, bool asPrimary, bool asEmpty)
    {
        if (msuPcmService.IsGeneratingPcm)
        {
            return new GeneratePcmFileResponse(false, false, "Currently generating another file", null);
        }

        if (songInfo.TrackNumber > 1000 && songInfo.OutputPath?.StartsWith(Directories.TempFolder) != true)
        {
            var msuFile = new FileInfo(project.MsuPath);
            var pcmFileName = msuFile.Name.Replace(msuFile.Extension, $"-{Guid.NewGuid()}.pcm");
            songInfo.OutputPath = Path.Combine(Directories.TempFolder, pcmFileName);
        }
        
        await audioPlayerService.StopSongAsync(null, true);

        if (asEmpty)
        {
            var emptySong = new MsuSongInfo();
            converterService.ConvertViewModel(songInfo, emptySong);
            var successful = msuPcmService.CreateEmptyPcm(emptySong);

            return !successful
                ? new GeneratePcmFileResponse(false, false, "Currently generating another file", null)
                : new GeneratePcmFileResponse(true, true, "Successful", songInfo.OutputPath);
        }
        
        if (!songInfo.HasAudioFiles())
        {
            return new GeneratePcmFileResponse(false, false, "No files specified to generate into a pcm file", null);
        }
        
        if (asPrimary)
        {
            var msu = new FileInfo(project.MsuPath);
            var path = msu.FullName.Replace(msu.Extension, $"-{songInfo.TrackNumber}.pcm");
            songInfo.OutputPath = path;
        }

        var response = await msuPcmService.CreatePcm(true, project, songInfo);
        if (!response.Successful)
        {
            if (response.GeneratedPcmFile && project.IgnoreWarnings.Contains(songInfo.OutputPath ?? ""))
            {
                songInfo.LastGeneratedDate = DateTime.Now;
                return new GeneratePcmFileResponse(true, true, null, songInfo.OutputPath);
            }
            else
            {
                return response;
            }
        }
        
        songInfo.LastGeneratedDate = DateTime.Now;
        return response;
    }
    
    public async Task<string?> PlaySong(MsuSongInfoViewModel songInfo, bool testLoop)
    {
        var generateResponse = await GeneratePcmFile(songInfo, false, false); 
        if (!generateResponse.Successful)
            return generateResponse.Message;
        
        if (string.IsNullOrEmpty(songInfo.OutputPath) || !File.Exists(songInfo.OutputPath))
        {
            return "No pcm file detected";
        }

        var msuTypeTrackInfo = songInfo.Project.MsuType.Tracks.First(x => x.Number == songInfo.TrackNumber);
        
        await audioPlayerService.PlaySongAsync(songInfo.OutputPath, testLoop, !msuTypeTrackInfo.NonLooping);
        return null;
    }

    public async Task<string?> PlaySong(MsuProject project, MsuSongInfo song, bool testLoop)
    {
        var generateResponse = await GeneratePcmFile(project, song, false, false); 
        if (!generateResponse.Successful)
            return generateResponse.Message;
        
        if (string.IsNullOrEmpty(song.OutputPath) || !File.Exists(song.OutputPath))
        {
            return "No pcm file detected";
        }

        var msuTypeTrackInfo = project.MsuType.Tracks.First(x => x.Number == song.TrackNumber);
        
        await audioPlayerService.PlaySongAsync(song.OutputPath, testLoop, !msuTypeTrackInfo.NonLooping);
        return null;
    }

    public async Task PauseSong()
    {
        if (CanPauseSongs)
            audioPlayerService.Pause();
        else
            await audioPlayerService.StopSongAsync(null, true);
    }

    public async Task StopSong()
    {
        await audioPlayerService.StopSongAsync(null, true);
    }

    public bool CanPlaySongs => audioPlayerService.CanPlayMusic;

    public bool CanPauseSongs => audioPlayerService.CanPauseMusic;
}