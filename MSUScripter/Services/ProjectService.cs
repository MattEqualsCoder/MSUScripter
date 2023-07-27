using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using MSURandomizerLibrary.Configs;
using MSURandomizerLibrary.Services;
using MSUScripter.Configs;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MSUScripter.Services;

public class ProjectService
{
    private readonly IMsuTypeService _msuTypeService;
    private readonly IMsuLookupService _msuLookupService;
    private readonly IMsuDetailsService _msuDetailsService;
    private readonly AudioMetadataService _audioMetadataService;
    private MsuPcmService _msuPcmService;
    private readonly ILogger<ProjectService> _logger;
    
    public ProjectService(IMsuTypeService msuTypeService, IMsuLookupService msuLookupService, IMsuDetailsService msuDetailsService, MsuPcmService msuPcmService, ILogger<ProjectService> logger, AudioMetadataService audioMetadataService)
    {
        _msuTypeService = msuTypeService;
        _msuLookupService = msuLookupService;
        _msuDetailsService = msuDetailsService;
        _msuPcmService = msuPcmService;
        _logger = logger;
        _audioMetadataService = audioMetadataService;
    }
    
    public void SaveMsuProject(MsuProject project)
    {
        project.LastSaveTime = DateTime.Now;
        var serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        var yaml = serializer.Serialize(project);
        File.WriteAllText(project.ProjectFilePath, yaml);
    }

    public MsuProject? LoadMsuProject(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        
        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        var project = deserializer.Deserialize<MsuProject>(yaml);
        project.ProjectFilePath = path;
        project.MsuType = _msuTypeService.GetMsuType(project.MsuTypeName) ?? throw new InvalidOperationException();
        return project;
    }

    public MsuProject NewMsuProject(string projectPath, string msuTypeName, string msuPath, string? msuPcmTracksJsonPath, string? msuPcmWorkingDirectory)
    {
        var msuType = _msuTypeService.GetMsuType(msuTypeName) ?? throw new InvalidOperationException("Invalid MSU Type");
        
        var project = new MsuProject()
        {
            ProjectFilePath = projectPath,
            MsuType = msuType,
            MsuTypeName = msuType.DisplayName,
            MsuPath = msuPath,
        };

        project.BasicInfo.MsuType = project.MsuType.Name;
        project.BasicInfo.Game = project.MsuType.Name;
        
        foreach (var track in project.MsuType.Tracks.OrderBy(x => x.Number))
        {
            project.Tracks.Add(new MsuTrackInfo()
            {
                TrackNumber = track.Number,
                TrackName = track.Name
            });
        }

        if (File.Exists(msuPath))
        {
            ImportMsu(project, msuPath);
        }

        if (!string.IsNullOrEmpty(msuPcmTracksJsonPath) && File.Exists(msuPcmTracksJsonPath))
        {
            ImportMsuPcmTracksJson(project, msuPcmTracksJsonPath, msuPcmWorkingDirectory);
        }

        SaveMsuProject(project);

        return project;
    }

    public void ImportMsu(MsuProject project, string msuPath)
    {
        var msu = _msuLookupService.LoadMsu(msuPath, project.MsuType);

        if (msu == null)
            return;

        if (msu.MsuType != project.MsuType && msu.MsuType != null)
        {
            ConvertProjectMsuType(project, msu.MsuType);
        }

        project.BasicInfo.PackName = msu.Name;
        project.BasicInfo.PackCreator = msu.Creator;
        project.BasicInfo.PackVersion = msu.Version;

        if (msu.Tracks.Select(x => x.Artist).Distinct().Count() == 1)
        {
            project.BasicInfo.Artist = msu.Tracks.First().Artist;
        }
        
        if (msu.Tracks.Select(x => x.Album).Distinct().Count() == 1)
        {
            project.BasicInfo.Album = msu.Tracks.First().Album;
        }
        
        if (msu.Tracks.Select(x => x.Url).Distinct().Count() == 1)
        {
            project.BasicInfo.Url = msu.Tracks.First().Url;
        }

        foreach (var track in msu.Tracks)
        {
            if (track.IsCopied) continue;
            var projectTrack = project.Tracks.FirstOrDefault(x => x.TrackNumber == track.Number);
            if (projectTrack == null) continue;
            var song = projectTrack.Songs.FirstOrDefault(x => x.OutputPath == track.Path);
            if (song == null)
            {
                song = new MsuSongInfo()
                {
                    TrackNumber = track.Number,
                    TrackName = track.TrackName,
                    SongName = track.SongName,
                    Artist = track.Artist,
                    Album = track.Album,
                    Url = track.Url,
                    OutputPath = track.Path,
                    IsAlt = track.IsAlt
                };
                projectTrack.Songs.Add(song);
            }
        }
    }

    public void ConvertProjectMsuType(MsuProject project, MsuType newMsuType, bool swapPcmFiles = false)
    {
        if (!project.MsuType.IsCompatibleWith(newMsuType) && project.MsuType != newMsuType)
            return;

        var oldType = project.MsuType;
        project.MsuType = newMsuType;
        project.MsuTypeName = newMsuType.Name;

        var conversion = project.MsuType.Conversions[oldType];
        
        var msu = new FileInfo(project.MsuPath);
        var basePath = msu.FullName.Replace(msu.Extension, "");
        var baseName = msu.Name.Replace(msu.Extension, "");

        HashSet<string> swappedFiles = new HashSet<string>();
        var newTracks = new List<MsuTrackInfo>();
        foreach (var oldTrack in project.Tracks)
        {
            
            var newTrackNumber = conversion(oldTrack.TrackNumber);

            if (oldTrack.TrackNumber == newTrackNumber)
            {
                newTracks.Add(oldTrack);
                continue;
            }
            
            var newMsuTypeTrack = newMsuType.Tracks.FirstOrDefault(x => x.Number == newTrackNumber);
            if (newMsuTypeTrack == null) continue;

            var newSongs = new List<MsuSongInfo>();
            foreach (var oldSong in oldTrack.Songs)
            {
                var songBaseName = new FileInfo(oldSong.OutputPath).Name;
                if (!songBaseName.StartsWith($"{baseName}-{oldTrack.TrackNumber}"))
                    continue;
                
                var newSong = new MsuSongInfo()
                {
                    TrackNumber = newTrackNumber,
                    TrackName = newMsuTypeTrack.Name,
                    SongName = oldSong.SongName,
                    Artist = oldSong.Artist,
                    Album = oldSong.Album,
                    Url = oldSong.Url,
                    OutputPath = oldSong.OutputPath.Replace($"{basePath}-{oldTrack.TrackNumber}",
                        $"{basePath}-{newTrackNumber}"),
                    IsAlt = oldSong.IsAlt,
                    MsuPcmInfo = oldSong.MsuPcmInfo
                };
                
                newSongs.Add(newSong);

                if (swapPcmFiles && File.Exists(oldSong.OutputPath) && !swappedFiles.Contains(newSong.OutputPath))
                {
                    if (File.Exists(newSong.OutputPath))
                    {
                        _logger.LogInformation("{New} <=> {Old}", newSong.OutputPath, oldSong.OutputPath);
                        swappedFiles.Add(newSong.OutputPath);
                        swappedFiles.Add(oldSong.OutputPath);
                        File.Move(newSong.OutputPath, newSong.OutputPath + ".tmp");
                        File.Move(oldSong.OutputPath, oldSong.OutputPath + ".tmp");
                        File.Move(newSong.OutputPath + ".tmp", oldSong.OutputPath);
                        File.Move(oldSong.OutputPath + ".tmp", newSong.OutputPath);
                    }
                    else
                    {
                        _logger.LogInformation("{New} <== {Old}", newSong.OutputPath, oldSong.OutputPath);
                        File.Move(oldSong.OutputPath, newSong.OutputPath );
                    }
                }
            }
            
            newTracks.Add(new MsuTrackInfo()
            {
                TrackNumber = newTrackNumber,
                TrackName = newMsuTypeTrack.Name,
                Songs = newSongs
            });
        }

        project.Tracks = newTracks;
    }

    public void ImportMsuPcmTracksJson(MsuProject project, string jsonPath, string? msuPcmWorkingDirectory)
    {
        var data = File.ReadAllText(jsonPath);
        var msuPcmData = JsonConvert.DeserializeObject<MsuPcmPlusPlusConfig>(data);

        if (string.IsNullOrEmpty(msuPcmWorkingDirectory))
        {
            msuPcmWorkingDirectory = new FileInfo(jsonPath).DirectoryName!;
        }

        project.BasicInfo.PackName = msuPcmData.Pack;
        project.BasicInfo.Artist = msuPcmData.Artist;
        project.BasicInfo.Game = msuPcmData.Game;
        project.BasicInfo.Normalization = msuPcmData.Normalization;
        project.BasicInfo.Dither = msuPcmData.Dither;

        var msuFileInfo = new FileInfo(project.MsuPath);
        var msuDirectory = msuFileInfo.DirectoryName!;
        var msuName = msuFileInfo.Name.Replace(msuFileInfo.Extension, "");

        foreach (var track in msuPcmData.Tracks.GroupBy(x => x.Track_number))
        {
            var trackNumber = track.First().Track_number;
            var projectTrack = project.Tracks.FirstOrDefault(x => x.TrackNumber == trackNumber);
            if (projectTrack == null) continue;

            var msuPcmInfo = track.SelectMany(x => ConverterService.ConvertMsuPcmTrackInfo(x, msuPcmWorkingDirectory)).ToList();
            var songs = projectTrack.Songs.OrderBy(x => x.IsAlt).ToList();
            
            for (var i = 0; i < msuPcmInfo.Count; i++)
            {
                string trackPath;
                
                if (!string.IsNullOrEmpty(msuPcmInfo[i].Output))
                {
                    trackPath = ConverterService.GetAbsolutePath(msuPcmWorkingDirectory, msuPcmInfo[i].Output!);
                }
                else if (i == 0)
                {
                    trackPath = Path.Combine(msuDirectory, $"{msuName}-{trackNumber}.pcm");
                }
                else
                {
                    trackPath = Path.Combine(msuDirectory, $"{msuName}-{trackNumber}_alt{i}.pcm");
                }

                AudioMetadata? metadata = null;
                var files = msuPcmInfo[i].GetFiles();
                if (files.Count == 1)
                {
                    metadata = _audioMetadataService.GetAudioMetadata(files.First());
                }

                if (i < songs.Count)
                {
                    var song = songs[i];
                    song.MsuPcmInfo = msuPcmInfo[i];

                    if (metadata?.HasData == true)
                    {
                        if (string.IsNullOrEmpty(song.SongName) || song.SongName.StartsWith("Track #"))
                        {
                            song.SongName = metadata.SongName;
                        }

                        if (string.IsNullOrEmpty(song.Artist))
                        {
                            song.Artist = metadata.Artist;
                        }
                        
                        if (string.IsNullOrEmpty(song.Album))
                        {
                            song.Album = metadata.Album;
                        }
                        
                        if (string.IsNullOrEmpty(song.Url))
                        {
                            song.Url = metadata.Url;
                        }
                    }
                }
                else
                {
                    projectTrack.Songs.Add(new MsuSongInfo()
                    {
                        TrackNumber = trackNumber,
                        TrackName = projectTrack.TrackName,
                        SongName = metadata?.SongName,
                        Artist = metadata?.Artist,
                        Album = metadata?.Album,
                        Url = metadata?.Url,
                        OutputPath = trackPath,
                        IsAlt = i != 0,
                        MsuPcmInfo = msuPcmInfo[i]
                    });
                }
            }
        }
    }

    public void ExportMsuRandomizerYaml(MsuProject project)
    {
        var msuFile = new FileInfo(project.MsuPath);
        var msuDirectory = msuFile.Directory!;

        var tracks = new List<MSURandomizerLibrary.Configs.Track>();

        foreach (var projectTrack in project.Tracks)
        {
            foreach (var projectSong in projectTrack.Songs)
            {
                tracks.Add(new MSURandomizerLibrary.Configs.Track(projectTrack.TrackName, projectTrack.TrackNumber, 
                    projectSong.SongName ?? "", projectSong.OutputPath, project.MsuPath, 
                    null, null, projectSong.Artist, projectSong.Album, projectSong.Url, 
                    projectSong.IsAlt));
            }
        }

        var msu = new Msu()
        {
            MsuType = project.MsuType,
            Name = project.BasicInfo.PackName ?? msuDirectory.Name,
            Creator = project.BasicInfo.PackCreator,
            Version = project.BasicInfo.PackVersion,
            FolderName = msuDirectory.Name,
            FileName = msuFile.Name,
            Path = project.MsuPath,
            Album = project.BasicInfo.Album,
            Artist = project.BasicInfo.Artist,
            Url = project.BasicInfo.Url,
            Tracks = tracks
        };

        var yamlPath = msuFile.FullName.Replace(msuFile.Extension, ".yml");
        _msuDetailsService.SaveMsuDetails(msu, yamlPath, out var error);
    }
}