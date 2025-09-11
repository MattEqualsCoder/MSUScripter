using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MSUScripter.Configs;
using MSUScripter.Models;

namespace MSUScripter.Services;

public class SharedPcmService(MsuPcmService msuPcmService, IAudioPlayerService audioPlayerService)
{
    public async Task<GeneratePcmFileResponse> GeneratePcmFile(MsuProject project, MsuSongInfo songInfo, bool asPrimary, bool asEmpty, bool isBulkGeneration)
    {
        if (!isBulkGeneration && msuPcmService.IsGeneratingPcm)
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
            return !msuPcmService.CreateEmptyPcm(songInfo)
                ? new GeneratePcmFileResponse(false, false, "Currently generating another file", null)
                : new GeneratePcmFileResponse(true, true, "Successful", songInfo.OutputPath);
        }
        
        if (!songInfo.HasAudioFiles())
        {
            return new GeneratePcmFileResponse(false, false, "No files specified to generate into a pcm file", null);
        }

        var response = await msuPcmService.CreatePcm(project, songInfo, asPrimary, isBulkGeneration);
        return response;
    }
    
    public async Task<string?> PlaySong(MsuProject project, MsuSongInfo song, bool testLoop)
    {
        var generateResponse = await GeneratePcmFile(project, song, false, false, false); 
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

    public void SaveGenerationCache(MsuProject project)
    {
        msuPcmService.SaveGenerationCache(project);
    }

    public bool CanPlaySongs => audioPlayerService.CanPlayMusic;

    public bool CanPauseSongs => audioPlayerService.CanPauseMusic;
}