using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MSURandomizerLibrary.Configs;
using MSURandomizerLibrary.Services;
using MSUScripter.Configs;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Track = MSUScripter.Configs.Track;

namespace MSUScripter.Services;

public class ProjectService
{
    private IMsuTypeService _msuTypeService;
    private IMsuLookupService _msuLookupService;
    private IMsuDetailsService _msuDetailsService;
    private MsuPcmService _msuPcmService;
    
    public ProjectService(IMsuTypeService msuTypeService, IMsuLookupService msuLookupService, IMsuDetailsService msuDetailsService, MsuPcmService msuPcmService)
    {
        _msuTypeService = msuTypeService;
        _msuLookupService = msuLookupService;
        _msuDetailsService = msuDetailsService;
        _msuPcmService = msuPcmService;
    }
    
    public void SaveMsuProject(MsuProject project)
    {
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
            MsuPcmTracksJsonPath = msuPcmTracksJsonPath
        };

        project.BasicInfo.MsuType = project.MsuType.DisplayName;
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
                var trackPath = "";
                
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

                if (i < songs.Count)
                {
                    songs[i].MsuPcmInfo = msuPcmInfo[i];
                }
                else
                {
                    projectTrack.Songs.Add(new MsuSongInfo()
                    {
                        TrackNumber = trackNumber,
                        TrackName = projectTrack.TrackName,
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