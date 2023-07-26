using System;
using System.IO;
using System.Linq;
using FlacLibSharp;
using Id3;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;

namespace MSUScripter.Services;

public class AudioMetadataService
{
    private readonly ILogger<AudioMetadataService> _logger;

    public AudioMetadataService(ILogger<AudioMetadataService> logger)
    {
        _logger = logger;
    }
    
    public AudioMetadata GetAudioMetadata(string file)
    {
        if (!File.Exists(file))
        {
            return new AudioMetadata();
        }
        
        var fileInfo = new FileInfo(file);

        if (fileInfo.Extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            return GetMp3AudioMetadata(file);
        }
        else if (fileInfo.Extension.Equals(".flac", StringComparison.OrdinalIgnoreCase))
        {
            return GetFlacAudioMetadata(file);
        }

        return new AudioMetadata();
    }

    public AudioMetadata GetMp3AudioMetadata(string file)
    {
        try
        {
            using var mp3 = new Mp3(file);
            var tag = mp3.GetTag(Id3TagFamily.Version2X);
            var fileInfo = new FileInfo(file);

            if (tag != null)
            {
                return new AudioMetadata()
                {
                    SongName = !string.IsNullOrEmpty(tag.Title.Value)
                        ? tag.Title.Value 
                        : fileInfo.Name.Replace(fileInfo.Extension, ""),
                    Artist = string.Join(", ", tag.Artists.Value),
                    Album = tag.Album.Value,
                    Url = tag.ArtistUrls.Any()
                        ? string.Join(", ", tag.ArtistUrls.Select(x => x.Url).ToList())
                        : tag.CopyrightUrl.Url
                };
            }

            return new AudioMetadata()
            {
                SongName = fileInfo.Name.Replace(fileInfo.Extension, "")
            };

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to retrieve metadata for {File}", file);
            return new AudioMetadata();
        }
    }
    
    public AudioMetadata GetFlacAudioMetadata(string file)
    {
        try
        {
            var fileInfo = new FileInfo(file);
            using FlacFile flacFile = new FlacFile(file);
            var vorbisComment = flacFile.VorbisComment;
            if (vorbisComment != null)
            {
                return new AudioMetadata()
                {
                    SongName = vorbisComment.Title.FirstOrDefault() ?? fileInfo.Name.Replace(fileInfo.Extension, ""),
                    Artist = string.Join(", ", vorbisComment.Artist),
                    Album = vorbisComment.Album.FirstOrDefault()
                };
            }
            return new AudioMetadata()
            {
                SongName = fileInfo.Name.Replace(fileInfo.Extension, "")
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to retrieve metadata for {File}", file);
            return new AudioMetadata();
        }
    }
}