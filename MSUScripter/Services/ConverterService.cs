using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using MSURandomizerLibrary.Configs;
using MSURandomizerLibrary.Services;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.ViewModels;
using Track = MSUScripter.Configs.Track;

namespace MSUScripter.Services;

public class ConverterService(IMsuTypeService msuTypeService)
{
    public bool ConvertViewModel<A, B>(A input, B output, bool recursive = true) where B : new()
    {
        var propertiesA = typeof(A).GetProperties().Where(x => x.CanWrite && x.PropertyType.Namespace?.Contains("MSU") != true && x.GetCustomAttribute<SkipConvertAttribute>() == null).ToDictionary(x => x.Name, x => x);
        var propertiesB = typeof(B).GetProperties().Where(x => x.CanWrite && x.PropertyType.Namespace?.Contains("MSU") != true && x.GetCustomAttribute<SkipConvertAttribute>() == null).ToDictionary(x => x.Name, x => x);
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

            if (propA.PropertyType == typeof(List<A>) || propA.PropertyType == typeof(ObservableCollection<A>))
            {
                if (recursive)
                {
                    IList<A>? aValue = propA.GetValue(input) as IList<A>;
                    IList<B> bValue = propB.GetValue(output) as IList<B> ?? (propB.PropertyType == typeof(ObservableCollection<B>) ? new ObservableCollection<B>() : new List<B>());
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
    
    public MsuProject CloneProject(MsuProject project)
    {
        var newProject = new MsuProject();
        ConvertViewModel(project, newProject);
        ConvertViewModel(project.BasicInfo, newProject.BasicInfo);
        
        foreach (var track in project.Tracks)
        {
            var trackViewModel = new MsuTrackInfo();
            ConvertViewModel(track, trackViewModel);

            foreach (var song in track.Songs)
            {
                var songViewModel = new MsuSongInfo();
                ConvertViewModel(song, songViewModel);
                ConvertViewModel(song.MsuPcmInfo, songViewModel.MsuPcmInfo);
            }

            newProject.Tracks.Add(trackViewModel);
        }

        newProject.MsuType = project.MsuType;
        newProject.LastSaveTime = DateTime.Now;

        return newProject;
    }
    
    public MsuProjectViewModel ConvertProject(MsuProject project)
    {
        var viewModel = new MsuProjectViewModel();
        ConvertViewModel(project, viewModel);
        ConvertViewModel(project.BasicInfo, viewModel.BasicInfo);
        // viewModel.BasicInfo.Project = viewModel;
        
        foreach (var track in project.Tracks)
        {
            var msuTypeTrack = project.MsuType.Tracks.FirstOrDefault(x => x.Number == track.TrackNumber);

            var trackViewModel = new MsuTrackInfoViewModel()
            {
                Project = viewModel,
                Description = track.IsScratchPad
                    ? "Use this page to add songs for keeping and editing without including them in the MSU. All songs included in this will not be included when generating and packaging the MSU."
                    : msuTypeTrack?.Description
            };
            
            ConvertViewModel(track, trackViewModel);

            foreach (var song in track.Songs)
            {
                var songViewModel = new MsuSongInfoViewModel();
                ConvertViewModel(song, songViewModel);
                ConvertViewModel(song.MsuPcmInfo, songViewModel.MsuPcmInfo);
                songViewModel.ApplyCascadingSettings(viewModel, trackViewModel, songViewModel.IsAlt, songViewModel.CanPlaySongs, false, false);
                trackViewModel.Songs.Add(songViewModel);
            }

            viewModel.Tracks.Add(trackViewModel);
        }

        viewModel.MsuType = project.MsuType;
        viewModel.LastSaveTime = DateTime.Now;

        return viewModel;
    }
    
    public MsuProject ConvertProject(MsuProjectViewModel viewModel)
    {
        var project = new MsuProject();
        ConvertViewModel(viewModel, project);
        ConvertViewModel(viewModel.BasicInfo, project.BasicInfo);
        project.MsuType = msuTypeService.GetMsuType(project.MsuTypeName) ??
                          throw new InvalidOperationException($"Invalid MSU Type {project.MsuTypeName}");

        foreach (var trackViewModel in viewModel.Tracks)
        {
            var track = new MsuTrackInfo();
            ConvertViewModel(trackViewModel, track);

            foreach (var songViewModel in trackViewModel.Songs)
            {
                var song = new MsuSongInfo();
                ConvertViewModel(songViewModel, song);
                ConvertViewModel(songViewModel.MsuPcmInfo, song.MsuPcmInfo);
                track.Songs.Add(song);
            }

            project.Tracks.Add(track);
        }
        
        return project;
    }

    public ICollection<MsuSongMsuPcmInfo> ConvertMsuPcmTrackInfo(Track_base trackBase, string rootPath)
    {
        var outputList = new List<MsuSongMsuPcmInfo>();
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
            if (track.Sub_channels?.Any() == true)
                output.SubChannels = track.Sub_channels.Select(x => ConvertMsuPcmTrackInfo(x, rootPath).First()).ToList();
            if (track.Sub_tracks?.Any() == true)
                output.SubTracks = track.Sub_tracks.Select(x => ConvertMsuPcmTrackInfo(x, rootPath).First()).ToList();

            if (track.Options?.Any() == true)
            {
                foreach (var option in track.Options.OrderBy(x => x.Option != track.Use_option))
                {
                    option.Copy(track);
                    var optionInfo = ConvertMsuPcmTrackInfo(option, rootPath).First();
                    outputList.Add(optionInfo);
                }
            }
        }
        else if (trackBase is Track_option trackOptions)
        {
            if (trackOptions.Sub_channels?.Any() == true)
                output.SubChannels = trackOptions.Sub_channels.Select(x => ConvertMsuPcmTrackInfo(x, rootPath).First()).ToList();
            if (trackOptions.Sub_tracks?.Any() == true)
                output.SubTracks = trackOptions.Sub_tracks.Select(x => ConvertMsuPcmTrackInfo(x, rootPath).First()).ToList();
        }
        else if (trackBase is Sub_track subTrack && subTrack.Sub_channels?.Any() == true)
        {
            output.SubChannels = subTrack.Sub_channels.Select(x => ConvertMsuPcmTrackInfo(x, rootPath).First()).ToList();
        }
        else if (trackBase is Sub_channel subChannel && subChannel.Sub_tracks?.Any() == true)
        {
            output.SubTracks = subChannel.Sub_tracks.Select(x => ConvertMsuPcmTrackInfo(x, rootPath).First()).ToList();
        }

        if (!outputList.Any())
        {
            outputList.Add(output);
        }
        
        return outputList;
    }
    
    public Track_base ConvertMsuPcmTrackInfo(MsuSongMsuPcmInfo trackBase, bool isSubTrack, bool isSubChannel)
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
    
    public string GetAbsolutePath(string basePath, string relativePath)
    {
        if (Path.GetFullPath(relativePath) == relativePath)
        {
            return relativePath;
        }
        
        var absolute = Path.Combine(basePath, relativePath);
        return Path.GetFullPath(absolute);
    }

    public MsuDetails ConvertMsuDetailsToMsuType(MsuDetails msuDetails, MsuType oldType, MsuType msuType, string oldPath, string newPath)
    {
        var newDetails = new MsuDetails()
        {
            PackName = msuDetails.PackName,
            PackAuthor = msuDetails.PackAuthor,
            PackVersion = msuDetails.PackVersion,
            Artist = msuDetails.Artist,
            Album = msuDetails.Album,
            MsuType = msuType.Name,
            Url = msuDetails.Url,
        };

        if (msuDetails.Tracks?.Any() != true)
        {
            return newDetails;
        }

        var conversion = msuType.Conversions[oldType];

        newDetails.Tracks = new Dictionary<string, MsuDetailsTrack>();

        var oldMsu = new FileInfo(oldPath);
        var oldBase = oldMsu.Name.Replace(oldMsu.Extension, "");
        var newMsu = new FileInfo(newPath);
        var newBase = newMsu.Name.Replace(newMsu.Extension, "");

        foreach (var track in msuDetails.Tracks!)
        {
            var oldDetails = track.Value;
            var oldTypeTrack = oldType.Tracks.FirstOrDefault(x =>
                x.YamlName == track.Key || x.YamlNameSecondary == track.Key ||
                x.Number == oldDetails.TrackNumber);

            if (oldTypeTrack == null)
            {
                continue;
            }
            
            var newTypeTrack =
                msuType.Tracks.FirstOrDefault(x =>
                    x.YamlName == track.Key || x.YamlNameSecondary == track.Key || x.Number == conversion(oldTypeTrack.Number));

            if (newTypeTrack == null)
            {
                continue;
            }

            var newTrackDetails = new MsuDetailsTrack()
            {
                TrackNumber = newTypeTrack.Number,
                Name = oldDetails.Name,
                Artist = oldDetails.Artist,
                Album = oldDetails.Album,
                FileLength = oldDetails.FileLength,
                Hash = oldDetails.Hash,
                Path = oldDetails.Path?.Replace($"{oldBase}-{oldTypeTrack.Number}", $"{newBase}-{newTypeTrack.Number}"),
                Url = oldDetails.Url,
                MsuName = oldDetails.MsuName,
                MsuAuthor = oldDetails.MsuAuthor,
                IsCopyrightSafe = oldDetails.IsCopyrightSafe
            };

            if (oldDetails.Alts?.Any() == true)
            {
                newTrackDetails.Alts = new List<MsuDetailsTrack>();
                foreach (var alt in oldDetails.Alts)
                {
                    newTrackDetails.Alts.Add(new MsuDetailsTrack()
                    {
                        Name = alt.Name,
                        Artist = alt.Artist,
                        Album = alt.Album,
                        FileLength = alt.FileLength,
                        Hash = alt.Hash,
                        Path = alt.Path?.Replace($"{oldBase}-{oldTypeTrack.Number}", $"{newBase}-{newTypeTrack.Number}"),
                        Url = alt.Url,
                        MsuName = alt.MsuName,
                        MsuAuthor = alt.MsuAuthor,
                        IsCopyrightSafe = alt.IsCopyrightSafe
                    });
                }
            }

            newDetails.Tracks[newTypeTrack.YamlNameSecondary ?? newTypeTrack.YamlName!] = newTrackDetails;
        }
        
        return newDetails;
    }
}