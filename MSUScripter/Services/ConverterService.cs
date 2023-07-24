using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MSUScripter.Configs;

namespace MSUScripter.Services;

public class ConverterService
{
    public static bool ConvertViewModel<A, B>(A input, B output, bool recursive = true) where B : new()
    {
        var propertiesA = typeof(A).GetProperties().Where(x => x.CanWrite && x.PropertyType.Namespace?.Contains("MSU") != true).ToDictionary(x => x.Name, x => x);
        var propertiesB = typeof(B).GetProperties().Where(x => x.CanWrite && x.PropertyType.Namespace?.Contains("MSU") != true).ToDictionary(x => x.Name, x => x);
        var updated = false;

        if (propertiesA.Count != propertiesB.Count)
        {
            throw new InvalidOperationException($"Types {typeof(A).Name} and {typeof(B).Name} are not compatible");
        }

        foreach (var propA in propertiesA.Values)
        {
            if (!propertiesB.TryGetValue(propA.Name, out var propB))
            {
                continue;
            }

            if (propA.PropertyType == typeof(List<A>))
            {
                if (recursive)
                {
                    var aValue = propA.GetValue(input) as List<A>;
                    var bValue = new List<B>();
                    if (aValue != null)
                    {
                        foreach (var aSubItem in aValue)
                        {
                            var bSubItem = new B();
                            ConvertViewModel(aSubItem, bSubItem);
                            bValue.Add(bSubItem);
                        }
                    }
                    propB.SetValue(output, bValue);
                }
            }
            else
            {
                var value = propA.GetValue(input);
                var originalValue = propA.GetValue(input);
                updated |= value != originalValue;
                propB.SetValue(output, value);
            }
        }

        return updated;
    }

    public static MsuSongMsuPcmInfo ConvertMsuPcmTrackInfo(Track_base trackBase, string rootPath)
    {
        var output = new MsuSongMsuPcmInfo();
        
        var propertiesA = typeof(Track_base).GetProperties().Where(x => x.CanWrite).ToDictionary(x => x.Name.ToLower().Replace("_", ""), x => x);
        var propertiesB = typeof(MsuSongMsuPcmInfo).GetProperties().Where(x => x.CanWrite).ToDictionary(x => x.Name.ToLower().Replace("_", ""), x => x);

        foreach (var key in propertiesA.Keys.Where(x => propertiesB.Keys.Contains(x)))
        {
            var propA = propertiesA[key];
            var propB = propertiesB[key];

            var valueA = propA.GetValue(trackBase);
            propB.SetValue(output, valueA);
        }

        if (!string.IsNullOrEmpty(output.File))
        {
            output.File = GetAbsolutePath(rootPath, output.File);
        }
        
        if (trackBase is Track track)
        {
            if (track.Sub_channels != null)
                output.SubChannels = track.Sub_channels.Select(x => ConvertMsuPcmTrackInfo(x, rootPath)).ToList();
            if (track.Sub_tracks != null)
                output.SubTracks = track.Sub_tracks.Select(x => ConvertMsuPcmTrackInfo(x, rootPath)).ToList();
        }
        else if (trackBase is Sub_track { Sub_channels: not null } subTrack)
        {
            output.SubChannels = subTrack.Sub_channels.Select(x => ConvertMsuPcmTrackInfo(x, rootPath)).ToList();
        }
        else if (trackBase is Sub_channel { Sub_tracks: not null } subChannel)
        {
            output.SubTracks = subChannel.Sub_tracks.Select(x => ConvertMsuPcmTrackInfo(x, rootPath)).ToList();
        }
        
        return output;
    }
    
    public static Track_base ConvertMsuPcmTrackInfo(MsuSongMsuPcmInfo trackBase, bool isSubTrack, bool isSubChannel)
    {
        Track_base output;
        if (!isSubTrack && !isSubChannel)
        {
            output = new Track();
        }
        else if (isSubChannel)
        {
            output = new Sub_channel();
        }
        else if (isSubTrack)
        {
            output = new Sub_track();
        }
        else
        {
            throw new InvalidOperationException();
        }
        
        var propertiesA = typeof(MsuSongMsuPcmInfo).GetProperties().Where(x => x.CanWrite).ToDictionary(x => x.Name.ToLower().Replace("_", ""), x => x);
        var propertiesB = typeof(Track_base).GetProperties().Where(x => x.CanWrite).ToDictionary(x => x.Name.ToLower().Replace("_", ""), x => x);

        foreach (var key in propertiesA.Keys.Where(x => propertiesB.Keys.Contains(x)))
        {
            var propA = propertiesA[key];
            var propB = propertiesB[key];

            var valueA = propA.GetValue(trackBase);
            propB.SetValue(output, valueA);
        }

        if (!isSubTrack && !isSubChannel && output is Track track)
        {
            if (trackBase.SubChannels.Any())
                track.Sub_channels = trackBase.SubChannels.Select(x => ConvertMsuPcmTrackInfo(x, false, true)).Cast<Sub_channel>().ToList();
            if (trackBase.SubTracks.Any())
                track.Sub_tracks = trackBase.SubTracks.Select(x => ConvertMsuPcmTrackInfo(x, true, false)).Cast<Sub_track>().ToList();
        }
        else if (isSubChannel && output is Sub_channel subChannel && trackBase.SubTracks.Any())
        {
            subChannel.Sub_tracks = trackBase.SubTracks.Select(x => ConvertMsuPcmTrackInfo(x, true, false)).Cast<Sub_track>().ToList();
        }
        else if (isSubTrack && output is Sub_track subTrack && trackBase.SubChannels.Any())
        {
            subTrack.Sub_channels = trackBase.SubChannels.Select(x => ConvertMsuPcmTrackInfo(x, false, true)).Cast<Sub_channel>().ToList();
        }
        
        return output;
    }
    
    public static string GetAbsolutePath(string basePath, string relativePath)
    {
        if (Path.GetFullPath(relativePath) == relativePath)
        {
            return relativePath;
        }
        
        var absolute = Path.Combine(basePath, relativePath);
        return Path.GetFullPath(absolute);
    }
}