using System;
using System.IO;
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
    
    public async Task<string?> PlaySong(MsuSongInfoViewModel songInfo, bool testLoop)
    {
        var generateResponse = await GeneratePcmFile(songInfo, false, false); 
        if (!generateResponse.Successful)
            return generateResponse.Message;
        
        if (string.IsNullOrEmpty(songInfo.OutputPath) || !File.Exists(songInfo.OutputPath))
        {
            return "No pcm file detected";
        }
        
        await audioPlayerService.PlaySongAsync(songInfo.OutputPath, testLoop);
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