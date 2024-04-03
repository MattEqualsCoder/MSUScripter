using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using MSUScripter.Configs;
using File = System.IO.File;
using Tag = TagLib.NonContainer.Tag;
using UrlLinkFrame = TagLib.Id3v2.UrlLinkFrame;

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

        try
        {
            var toReturn = new AudioMetadata()
            {
                SongName = Path.GetFileNameWithoutExtension(file),
                Artist = "",
                Album = "",
                Url = ""
            };
            
            var tagFile = TagLib.File.Create(file);
            
            if (tagFile == null)
            {
                return toReturn;
            }

            if (!string.IsNullOrEmpty(tagFile.Tag?.Title))
            {
                toReturn.SongName = tagFile.Tag.Title;
            }

            if (tagFile.Tag?.Composers?.Any() == true)
            {
                toReturn.Artist = string.Join(", ", tagFile.Tag.Composers);
            }
            else if (tagFile.Tag?.Performers?.Any() == true)
            {
                toReturn.Artist = string.Join(", ", tagFile.Tag.Performers);
            }
            else if (tagFile.Tag?.AlbumArtists?.Any() == true)
            {
                toReturn.Artist = string.Join(", ", tagFile.Tag.AlbumArtists);
            }

            if (!string.IsNullOrEmpty(tagFile.Tag?.Album))
            {
                toReturn.Album = tagFile.Tag.Album;
            }

            if (tagFile.Tag is Tag baseTag)
            {
                foreach (var tag in baseTag.Tags)
                {
                    if (tag is TagLib.Id3v2.Tag id3Tag)
                    {
                        var urlFrame = id3Tag.FirstOrDefault(x => x is UrlLinkFrame) as UrlLinkFrame;
                        if (!(urlFrame?.Text.Length > 0)) continue;
                        toReturn.Url = urlFrame.Text[0];
                        break;
                    }
                    
                }
            }
            
            return toReturn;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to retrieve metadata for {File}", file);
            return new AudioMetadata();
        }
    }

}