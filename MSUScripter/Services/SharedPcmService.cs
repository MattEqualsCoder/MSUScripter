using System;
using System.IO;
using System.Threading.Tasks;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.Services;

public class SharedPcmService(MsuPcmService msuPcmService, IAudioPlayerService audioPlayerService, ConverterService converterService)
{
    public bool GeneratePcmFile(MsuSongInfoViewModel songInfo, bool asPrimary, bool asEmpty, out string error, out bool msuPcmError)
    {
        error = "";
        msuPcmError = false;
        
        if (msuPcmService.IsGeneratingPcm) return false;

        if (asEmpty)
        {
            var emptySong = new MsuSongInfo();
            converterService.ConvertViewModel(songInfo, emptySong);
            var successful = msuPcmService.CreateEmptyPcm(emptySong);
            if (!successful)
            {
                error = "Could not generate empty pcm file";
                return false;
            }
            
            return true;
        }
        
        if (!songInfo.HasFiles())
        {
            error = "No files specified to generate into a pcm file";
            return false;
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
        
        if (!msuPcmService.CreatePcm(tempProject, song, out var msuPcmMessage, out var generated, false))
        {
            if (generated)
            {
                if (!songInfo.Project.IgnoreWarnings.Contains(song.OutputPath ?? ""))
                {
                    msuPcmError = true;
                    error = msuPcmMessage ?? "Unknown error with msupcm++";
                }
                
                songInfo.LastGeneratedDate = DateTime.Now;
                return true;
            }
            else
            {
                error = msuPcmMessage ?? "Unknown error with msupcm++";
                return false;
            }
        }
        
        songInfo.LastGeneratedDate = DateTime.Now;
        return true;
    }
    
    public async Task<string?> PlaySong(MsuSongInfoViewModel songInfo, bool testLoop)
    {
        await audioPlayerService.StopSongAsync(null, true);
        
        if (!GeneratePcmFile(songInfo, false, false, out var error, out _))
            return error;
        
        if (string.IsNullOrEmpty(songInfo.OutputPath) || !File.Exists(songInfo.OutputPath))
        {
            return "No pcm file detected";
        }
        
        await audioPlayerService.PlaySongAsync(songInfo.OutputPath, testLoop);
        return null;
    }
    
    public async Task StopSong()
    {
        await audioPlayerService.StopSongAsync(null, true);
    }

    public bool CanPlaySongs => audioPlayerService.CanPlayMusic;
}