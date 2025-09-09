using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Platform.Storage;
using DynamicData;
using Microsoft.Extensions.Logging;
using MSURandomizerLibrary.Configs;
using MSURandomizerLibrary.Services;
using MSUScripter.Configs;
using MSUScripter.Models;
using MSUScripter.ViewModels;
using Newtonsoft.Json;

namespace MSUScripter.Services;

public class ProjectService(
    IMsuTypeService msuTypeService,
    IMsuLookupService msuLookupService,
    IMsuDetailsService msuDetailsService,
    ILogger<ProjectService> logger,
    AudioMetadataService audioMetadataService,
    SettingsService settingsService,
    ConverterService converterService,
    YamlService yamlService,
    StatusBarService statusBarService)
{
    public void SaveMsuProject(MsuProject project, bool isBackup)
    {
        project.LastSaveTime = DateTime.Now;
        
        if (!isBackup)
        {
            project.BackupFilePath = GetProjectBackupFilePath(project.ProjectFilePath);
            settingsService.AddRecentProject(project);
            SaveMsuProject(project, true);
        }
        
        var yaml = yamlService.ToYaml(project, YamlType.Pascal);

        if (isBackup && !Directory.Exists(GetBackupDirectory()))
        {
            try
            {
                Directory.CreateDirectory(GetBackupDirectory());
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Could not create backups directory");
                return;
            }
        }

        try
        {
            var path = isBackup ? Path.Combine(project.BackupFilePath) : project.ProjectFilePath;
            File.WriteAllText(path, yaml);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Could not save project file");
            if (!isBackup)
                throw;
        }
        
        var info = new FileInfo(project.MsuPath);
        if (info.Directory?.Exists != true)
        {
            Directory.CreateDirectory(info.Directory!.FullName);
        }

        if (!File.Exists(project.MsuPath))
        {
            using (File.Create(project.MsuPath)) {}
        }

        if (!isBackup)
        {
            logger.LogInformation("Saved project");
            statusBarService.UpdateStatusBar("Project Saved");
        }
        else
        {
            logger.LogInformation("Saved project backup");
            statusBarService.UpdateStatusBar("Backup Created");
        }
    }

    public MsuProject? LoadMsuProject(string path, bool isBackup)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        
        var yaml = File.ReadAllText(path);
        if (!yamlService.FromYaml<MsuProject>(yaml, YamlType.Pascal, out var project, out _) || project == null)
        {
            return null;
        }
        
        if (!isBackup)
        {
            project.ProjectFilePath = path;
            project.BackupFilePath = GetProjectBackupFilePath(path);
        }

        if (string.IsNullOrEmpty(project.Id))
        {
            project.Id = Guid.NewGuid().ToString("N");
        }

        project.MsuType = msuTypeService.GetMsuType(project.MsuTypeName) ?? throw new InvalidOperationException();
        
        // Whoops, I screwed up. Fix up broken tracks.
        var msuBasePath = project.MsuPath.Replace(".msu", "", StringComparison.OrdinalIgnoreCase);
        foreach (var track in project.Tracks)
        {
            foreach (var song in track.Songs)
            {
                foreach (var duplicate in project.Tracks.SelectMany(x => x.Songs).Where(x => x != song && x.OutputPath == song.OutputPath))
                {
                    var duplicateTrack = project.Tracks.First(x => x.Songs.Contains(duplicate));
                    duplicate.TrackNumber = duplicateTrack.TrackNumber;
                    duplicate.TrackName = duplicateTrack.TrackName;
                    var index = duplicateTrack.Songs.IndexOf(duplicate);
                    if (index == 0)
                    {
                        duplicate.OutputPath = $"{msuBasePath}-{duplicateTrack.TrackNumber}.pcm";
                        duplicate.IsAlt = false;
                    }
                    else
                    {
                        var altSuffix = index == 1 ? "alt" : $"alt{index}";
                        duplicate.OutputPath = $"{msuBasePath}-{duplicateTrack.TrackNumber}_{altSuffix}.pcm";
                        duplicate.IsAlt = true;
                    }
                }

                if (song.DisplayAdvancedMode == null)
                {
                    song.DisplayAdvancedMode = song.MsuPcmInfo.HasAdvancedData();
                }

                if (string.IsNullOrEmpty(song.Id))
                {
                    song.Id = Guid.NewGuid().ToString("N");
                }
            }
        }

        // Add the scratch pad if one doesn't exist already
        if (!project.Tracks.Any(x => x.IsScratchPad))
        {
            project.Tracks.Add(new MsuTrackInfo()
            {
                TrackNumber = 9999,
                TrackName = "Scratch Pad",
                IsScratchPad = true,
            });
        }

        // Save whether it's an SMZ3 MSU
        if (project.MsuType == msuTypeService.GetSMZ3LegacyMSUType() || project.MsuType == msuTypeService.GetSMZ3MsuType())
        {
            project.BasicInfo.IsSmz3Project = true;
        }
        
        // Convert track list
        if (!string.IsNullOrEmpty(project.BasicInfo.TrackList))
        {
            if (project.BasicInfo.TrackList == TrackListTypeDeprecated.List)
            {
                project.BasicInfo.TrackListType = TrackList.ListAlbumFirst;
            }
            else if (project.BasicInfo.TrackList == TrackListTypeDeprecated.Table)
            {
                project.BasicInfo.TrackListType = TrackList.Table;
            }
        }

        if (project.BasicInfo.Dither != null)
        {
            project.BasicInfo.DitherType = project.BasicInfo.Dither == true ? DitherType.All : DitherType.None;
            project.BasicInfo.Dither = null;
        }

        if (!isBackup)
        {
            settingsService.AddRecentProject(project);    
        }

        var generationCacheFile = project.GetMsuGenerationCacheFilePath();
        var cacheFolder = Path.Combine(Directories.CacheFolder, "Generation");
        if (File.Exists(generationCacheFile))
        {
            var cacheYaml = File.ReadAllText(generationCacheFile);
            if (yamlService.FromYaml<MsuProjectGenerationCache>(cacheYaml, YamlType.Pascal, out var cacheObject, out var error) && cacheObject != null)
            {
                project.GenerationCache = cacheObject;
            }
        }
        else if (!Directory.Exists(cacheFolder))
        {
            Directory.CreateDirectory(cacheFolder);
        }
        
        
        statusBarService.UpdateStatusBar("Project Loaded");
        
        return project;
    }

    public MsuProject NewMsuProject(string projectPath, string msuTypeName, string msuPath, string? msuPcmTracksJsonPath, string? msuPcmWorkingDirectory)
    {
        var msuType = msuTypeService.GetMsuType(msuTypeName) ?? throw new InvalidOperationException("Invalid MSU Type");

        return NewMsuProject(projectPath, msuType, msuPath, msuPcmTracksJsonPath, msuPcmWorkingDirectory, null, null);
    }
    
    public MsuProject NewMsuProject(string projectPath, MsuType msuType, string msuPath, string? msuPcmTracksJsonPath, string? msuPcmWorkingDirectory, string? projectName, string? creatorName)
    {
        var project = new MsuProject()
        {
            Id = Guid.NewGuid().ToString("N"),
            ProjectFilePath = projectPath,
            BackupFilePath = GetProjectBackupFilePath(projectPath),
            MsuType = msuType,
            MsuTypeName = msuType.DisplayName,
            MsuPath = msuPath,
            IsNewProject = true,
            BasicInfo = new MsuBasicInfo
            {
                PackName = projectName,
                PackCreator = creatorName,
                PackVersion = "",
            }
        };

        foreach (var track in project.MsuType.Tracks.OrderBy(x => x.Number))
        {
            project.Tracks.Add(new MsuTrackInfo()
            {
                TrackNumber = track.Number,
                TrackName = track.Name
            });
        }
        
        project.Tracks.Add(new MsuTrackInfo()
        {
            TrackNumber = 9999,
            TrackName = "Scratch Pad",
            IsScratchPad = true
        });

        if (!File.Exists(msuPath))
        {
            var fileInfo = new FileInfo(msuPath);
            var directoryInfo = fileInfo.Directory;
            var pattern = $"{Path.GetFileNameWithoutExtension(fileInfo.Name)}-*.pcm";
            if (directoryInfo?.EnumerateFiles(pattern).Any() == true)
            {
                using (File.Create(project.MsuPath)) {}
                ImportMsu(project, msuPath);
            }
        }
        else if (File.Exists(msuPath))
        {
            ImportMsu(project, msuPath);
        }

        if (!string.IsNullOrEmpty(msuPcmTracksJsonPath) && File.Exists(msuPcmTracksJsonPath))
        {
            try
            {
                ImportMsuPcmTracksJson(project, msuPcmTracksJsonPath, msuPcmWorkingDirectory);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Could not import msupcm++ json file");
                throw new InvalidOperationException("Invalid msupcm++ json file");
            }
        }
        
        if (msuType == msuTypeService.GetSMZ3LegacyMSUType() || msuType == msuTypeService.GetSMZ3MsuType())
        {
            project.BasicInfo.IsSmz3Project = true;
            project.BasicInfo.CreateSplitSmz3Script = true;
        }

        SaveMsuProject(project, false);

        return project;
    }

    public void ImportMsu(MsuProject project, string msuPath)
    {
        var msu = msuLookupService.LoadMsu(msuPath, project.MsuType);

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
                    IsAlt = track.IsAlt,
                    Id = Guid.NewGuid().ToString("N")
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
        project.BasicInfo.Game = project.MsuTypeName;
        project.BasicInfo.MsuType = project.MsuTypeName;

        var conversion = project.MsuType.Conversions[oldType];
        
        var msu = new FileInfo(project.MsuPath);
        var baseName = msu.Name.Replace(msu.Extension, "");

        HashSet<string> swappedFiles = new HashSet<string>();
        var newTracks = new List<MsuTrackInfo>();
        foreach (var oldTrack in project.Tracks)
        {
            if (oldTrack.IsScratchPad)
            {
                newTracks.Add(oldTrack);
                continue;
            }
            
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
                var oldSongFile = new FileInfo(oldSong.OutputPath ?? "");
                var songBaseName = oldSongFile.Name;
                if (!songBaseName.StartsWith($"{baseName}-{oldTrack.TrackNumber}"))
                    continue;

                var songDirectory = oldSongFile.DirectoryName;

                if (string.IsNullOrEmpty(songDirectory))
                {
                    continue;
                }
                
                var newSongName =
                    songBaseName.Replace($"{baseName}-{oldTrack.TrackNumber}", $"{baseName}-{newTrackNumber}");
                
                var newSong = new MsuSongInfo()
                {
                    TrackNumber = newTrackNumber,
                    TrackName = newMsuTypeTrack.Name,
                    SongName = oldSong.SongName,
                    Artist = oldSong.Artist,
                    Album = oldSong.Album,
                    Url = oldSong.Url,
                    OutputPath = Path.Combine(songDirectory, newSongName),
                    IsAlt = oldSong.IsAlt,
                    MsuPcmInfo = oldSong.MsuPcmInfo,
                    Id = Guid.NewGuid().ToString("N")
                };
                
                newSongs.Add(newSong);

                if (swapPcmFiles && File.Exists(oldSong.OutputPath) && !swappedFiles.Contains(newSong.OutputPath))
                {
                    if (File.Exists(newSong.OutputPath))
                    {
                        logger.LogInformation("{New} <=> {Old}", newSong.OutputPath, oldSong.OutputPath);
                        swappedFiles.Add(newSong.OutputPath);
                        swappedFiles.Add(oldSong.OutputPath);
                        File.Move(newSong.OutputPath, newSong.OutputPath + ".tmp");
                        File.Move(oldSong.OutputPath, oldSong.OutputPath + ".tmp");
                        File.Move(newSong.OutputPath + ".tmp", oldSong.OutputPath);
                        File.Move(oldSong.OutputPath + ".tmp", newSong.OutputPath);
                    }
                    else
                    {
                        logger.LogInformation("{New} <== {Old}", newSong.OutputPath, oldSong.OutputPath);
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

    public ICollection<MsuProject> GetSmz3SplitMsuProjects(MsuProject project, out Dictionary<string, string> convertedPaths, out string? error)
    {
        var toReturn = new List<MsuProject>();
        convertedPaths = new Dictionary<string, string>();
        
        if (project.MsuType != msuTypeService.GetSMZ3LegacyMSUType() && project.MsuType != msuTypeService.GetSMZ3MsuType())
        {
            error = "Invalid MSU Type";
            return toReturn;
        }

        if (string.IsNullOrEmpty(project.BasicInfo.MetroidMsuPath) ||
            string.IsNullOrEmpty(project.BasicInfo.ZeldaMsuPath))
        {
            error = "Missing Metroid or Zelda MSU path";
            return toReturn;
        }

        if (project.BasicInfo.MetroidMsuPath == project.MsuPath || project.BasicInfo.ZeldaMsuPath == project.MsuPath ||
            project.BasicInfo.MetroidMsuPath == project.BasicInfo.ZeldaMsuPath)
        {
            error = "The main MSU path, Metroid MSU path, and Zelda MSU path must all be unique.";
            return toReturn;
        }

        var msuType = msuTypeService.GetMsuType("Super Metroid") ??
                      throw new InvalidOperationException("Super Metroid MSU Type not found");
        toReturn.Add(InternalGetSmz3MsuProject(project, msuType, project.BasicInfo.MetroidMsuPath, convertedPaths));

        msuType = msuTypeService.GetMsuType("The Legend of Zelda: A Link to the Past") ??
                  throw new InvalidOperationException("A Link to the Past MSU Type not found");
        toReturn.Add(InternalGetSmz3MsuProject(project, msuType, project.BasicInfo.ZeldaMsuPath, convertedPaths));

        error = null;
        return toReturn;
    }

    private MsuProject InternalGetSmz3MsuProject(MsuProject project, MsuType msuType, string newMsuPath, Dictionary<string, string> convertedPaths)
    {
        var basicInfo = new MsuBasicInfo();
        converterService.ConvertViewModel(project.BasicInfo, basicInfo);

        var conversion = msuType.Conversions[project.MsuType];

        var trackConversions = project.Tracks
            .Where(x => !x.IsScratchPad)
            .Select(x => (x.TrackNumber, conversion(x.TrackNumber)))
            .Where(x => msuType.ValidTrackNumbers.Contains(x.Item2));

        var newTracks = new List<MsuTrackInfo>();

        var oldMsuFile = new FileInfo(project.MsuPath);
        var oldMsuBaseName = oldMsuFile.Name.Replace(oldMsuFile.Extension, "");
        var newMsuFile = new FileInfo(newMsuPath);
        var newMsuBaseName = newMsuFile.Name.Replace(newMsuFile.Extension, "");
        var folder = oldMsuFile.DirectoryName ?? "";

        foreach (var trackNumbers in trackConversions)
        {
            var oldTrackNumber = trackNumbers.TrackNumber;
            var newTrackNumber = trackNumbers.Item2;
            var trackName = msuType.Tracks.First(x => x.Number == newTrackNumber).Name;

            if (project.Tracks.First(x => x.TrackNumber == oldTrackNumber).Songs.Count > 1)
            {
                convertedPaths[Path.Combine(folder, $"{oldMsuBaseName}-{oldTrackNumber}_Original.pcm")] =
                    Path.Combine(folder, $"{newMsuBaseName}-{newTrackNumber}_Original.pcm");    
            }

            var newSongs = new List<MsuSongInfo>();
            foreach (var song in project.Tracks.First(x => x.TrackNumber == oldTrackNumber).Songs)
            {
                var newSong = new MsuSongInfo();
                converterService.ConvertViewModel(song, newSong);
                newSong.TrackNumber = newTrackNumber;
                newSong.TrackName = trackName;
                newSong.OutputPath =
                    song.OutputPath?.Replace($"{oldMsuBaseName}-{oldTrackNumber}", $"{newMsuBaseName}-{newTrackNumber}");

                convertedPaths[song.OutputPath ?? ""] = newSong.OutputPath ?? "";

                newSongs.Add(newSong);
            }
            
            newTracks.Add(new MsuTrackInfo()
            {
                TrackNumber = newTrackNumber,
                TrackName = trackName,
                Songs = newSongs
            });
        }
        
        return new MsuProject()
        {
            MsuPath = newMsuPath,
            MsuTypeName = msuType.Name,
            MsuType = msuType,
            BasicInfo = basicInfo,
            Tracks = newTracks
        };
    }

    public bool CreateSmz3SplitScript(MsuProject smz3Project, Dictionary<string, string>? convertedPaths = null)
    {
        var extraProjects = GetSmz3SplitMsuProjects(smz3Project, out var conversions, out var error).ToList();
        convertedPaths = conversions;
        
        var testTrack = smz3Project.Tracks.FirstOrDefault(x => x.TrackNumber > 100 && x.Songs.Any())?.TrackNumber;

        if (testTrack == null)
        {
            statusBarService.UpdateStatusBar("Insufficient tracks");
            return false;
        }
        
        CreateMsuFiles(smz3Project);
        
        var msu = new FileInfo(smz3Project.MsuPath);
        var folder = msu.DirectoryName ?? "";
        var testPath =Path.GetRelativePath(folder,  msu.FullName.Replace(msu.Extension, $"-{testTrack}.pcm"));

        var sbIsCombined = new StringBuilder();
        var sbIsSplit = new StringBuilder();
        
        foreach (var conversion in convertedPaths)
        {
            var combinedPath = Path.GetRelativePath(folder, conversion.Key);
            var splitPath = Path.GetRelativePath(folder, conversion.Value);

            sbIsCombined.AppendLine($"\tIF EXIST \"{combinedPath}\" ( RENAME \"{combinedPath}\" \"{splitPath}\" )");
            sbIsSplit.AppendLine($"\tIF EXIST \"{splitPath}\" ( RENAME \"{splitPath}\" \"{combinedPath}\" )");
        }

        var sbTotal = new StringBuilder();
        sbTotal.AppendLine($"IF EXIST \"{testPath}\" (");
        sbTotal.Append(sbIsCombined);
        sbTotal.AppendLine(") ELSE (");
        sbTotal.Append(sbIsSplit);
        sbTotal.Append(")");
        
        File.WriteAllText(smz3Project.GetSmz3SwapperPath(), sbTotal.ToString());

        statusBarService.UpdateStatusBar("SMZ3 Split Script Created");
        
        return true;
    }

    public void ImportMsuPcmTracksJson(MsuProject project, string jsonPath, string? msuPcmWorkingDirectory)
    {
        var data = File.ReadAllText(jsonPath);
        var msuPcmData = JsonConvert.DeserializeObject<MsuPcmPlusPlusConfig>(data);

        if (msuPcmData == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(msuPcmWorkingDirectory))
        {
            msuPcmWorkingDirectory = new FileInfo(jsonPath).DirectoryName!;
        }

        project.BasicInfo.PackName = msuPcmData.Pack;
        project.BasicInfo.Artist = msuPcmData.Artist;
        project.BasicInfo.Game = msuPcmData.Game;
        project.BasicInfo.Normalization = msuPcmData.Normalization;

        if (msuPcmData.Dither == true)
        {
            project.BasicInfo.DitherType = DitherType.All;
        }
        else if (msuPcmData.Dither == false)
        {
            project.BasicInfo.DitherType = DitherType.None;
        }

        var msuFileInfo = new FileInfo(project.MsuPath);
        var msuDirectory = msuFileInfo.DirectoryName!;
        var msuName = msuFileInfo.Name.Replace(msuFileInfo.Extension, "");

        foreach (var track in msuPcmData.Tracks.GroupBy(x => x.Track_number))
        {
            var trackNumber = track.First().Track_number;
            var projectTrack = project.Tracks.FirstOrDefault(x => x.TrackNumber == trackNumber);
            if (projectTrack == null) continue;

            var msuPcmInfo = track.SelectMany(x => converterService.ConvertMsuPcmTrackInfo(x, msuPcmWorkingDirectory)).ToList();
            var songs = projectTrack.Songs.OrderBy(x => x.IsAlt).ToList();
            
            for (var i = 0; i < msuPcmInfo.Count; i++)
            {
                string trackPath;
                
                if (!string.IsNullOrEmpty(msuPcmInfo[i].Output))
                {
                    trackPath = converterService.GetAbsolutePath(msuPcmWorkingDirectory, msuPcmInfo[i].Output!);
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
                    metadata = audioMetadataService.GetAudioMetadata(files.First());
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

    public bool CreateSmz3SplitRandomizerYaml(MsuProject project, bool metroidOnly, bool zeldaOnly, out string? error)
    {
        var data = new List<(MsuType?, string?)>();

        if (!metroidOnly)
        {
            data.Add((msuTypeService.GetMsuType("The Legend of Zelda: A Link to the Past"), project.BasicInfo.ZeldaMsuPath));
        }

        if (!zeldaOnly)
        {
            data.Add((msuTypeService.GetMsuType("Super Metroid"), project.BasicInfo.MetroidMsuPath));
        }

        var msu = new FileInfo(project.MsuPath);
        var yamlPath = msu.FullName.Replace(msu.Extension, ".yml");
        MsuDetails? msuDetails;
        
        try
        {
            var yamlText = File.ReadAllText(yamlPath);
            if (!yamlService.FromYaml(yamlText, YamlType.UnderscoreIgnoreDefaults, out msuDetails, out _) || msuDetails == null)
            {
                error = $"Could not retrieve MSU Details from {yamlPath}";
                logger.LogError(error);
                statusBarService.UpdateStatusBar("YAML File Write Error");
                return false;
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Could not retrieve MSU Details from {YamlPath}", yamlPath);
            error = $"Could not retrieve MSU Details from {yamlPath}";
            statusBarService.UpdateStatusBar("YAML File Write Error");
            return false;
        }
        
        foreach (var msuTypeInfo in data)
        {
            var msuType = msuTypeInfo.Item1;
            var msuPath = msuTypeInfo.Item2;
            
            if (msuType == null)
            {
                error = "Invalid MSU Type";
                logger.LogError(error);
                statusBarService.UpdateStatusBar("YAML File Write Error");
                return false;
            }

            if (string.IsNullOrEmpty(msuPath))
            {
                error = $"Invalid MSU path for {msuType.Name}";
                logger.LogError(error);
                statusBarService.UpdateStatusBar("YAML File Write Error");
                return false;
            }

            try
            {
                var newMsu = new FileInfo(msuPath);
                var newYamlPath = newMsu.FullName.Replace(newMsu.Extension, ".yml");
                var newMsuType = converterService.ConvertMsuDetailsToMsuType(msuDetails, project.MsuType, msuType, project.MsuPath, msuPath);
                var outYaml = yamlService.ToYaml(newMsuType, YamlType.UnderscoreIgnoreDefaults);
                statusBarService.UpdateStatusBar("YAML File Written");
                File.WriteAllText(newYamlPath, outYaml);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unable to convert MSU YAML");
                error = "Unable to convert MSU YAML";
                statusBarService.UpdateStatusBar("YAML File Write Error");
                return false;
            }
            
        }
        
        error = null;
        return true;
        
    }

    public void ExportMsuRandomizerYaml(MsuProject project, out string? error)
    {
        var msuFile = new FileInfo(project.MsuPath);
        var msuDirectory = msuFile.Directory!;

        var tracks = new List<MSURandomizerLibrary.Configs.Track>();

        foreach (var projectTrack in project.Tracks.Where(x => !x.IsScratchPad))
        {
            foreach (var projectSong in projectTrack.Songs)
            {
                tracks.Add(new MSURandomizerLibrary.Configs.Track(
                    trackName: projectTrack.TrackName,
                    number: projectTrack.TrackNumber,
                    songName: projectSong.SongName ?? "",
                    path: projectSong.OutputPath ?? "",
                    artist: projectSong.Artist,
                    album: projectSong.Album,
                    url: projectSong.Url,
                    isAlt: projectSong.IsAlt
                )
                {
                    IsCopyrightSafe = projectSong.IsCopyrightSafe,
                });
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
        if (!msuDetailsService.SaveMsuDetails(msu, yamlPath, out error))
        {
            logger.LogError(error);
            statusBarService.UpdateStatusBar("YAML File Write Failed");
        }

        statusBarService.UpdateStatusBar("YAML File Written");
        //
        // if (project.BasicInfo.CreateSplitSmz3Script)
        // {
        //     if (CreateSmz3SplitRandomizerYaml(project, out error))
        //     {
        //         statusBarService.UpdateStatusBar("YAML File Written");    
        //     }
        //     else
        //     {
        //         statusBarService.UpdateStatusBar("YAML File Write Failed");
        //     }
        // }
        // else
        // {
        //     statusBarService.UpdateStatusBar("YAML File Written");
        // }
        
    }

    public bool CreateMsuFiles(MsuProject project)
    {
        try
        {
            var msuFileInfo = new FileInfo(project.MsuPath);
            if (!Directory.Exists(msuFileInfo.DirectoryName) && !string.IsNullOrEmpty(msuFileInfo.DirectoryName))
            {
                Directory.CreateDirectory(msuFileInfo.DirectoryName);
            }
            
            if (!File.Exists(project.MsuPath))
            {
                using (File.Create(project.MsuPath))
                {
                }
            }

            if (project.BasicInfo.CreateSplitSmz3Script && !string.IsNullOrEmpty(project.BasicInfo.MetroidMsuPath) && !File.Exists(project.BasicInfo.MetroidMsuPath))
            {
                using (File.Create(project.BasicInfo.MetroidMsuPath))
                {
                }
            }

            if (project.BasicInfo.CreateSplitSmz3Script && !string.IsNullOrEmpty(project.BasicInfo.ZeldaMsuPath) && !File.Exists(project.BasicInfo.ZeldaMsuPath))
            {
                using (File.Create(project.BasicInfo.ZeldaMsuPath))
                {
                }
            }

            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unable to create msu file");
            return false;
        }
        
    }

    public bool CreateAltSwapperFile(MsuProject project, ICollection<MsuProject>? otherProjects = null)
    {
        if (project.Tracks.All(x => x.Songs.Count <= 1)) return true;
        
        otherProjects = new List<MsuProject>();

        if (project.BasicInfo.CreateSplitSmz3Script)
        {
            otherProjects = GetSmz3SplitMsuProjects(project, out _, out var error).ToList();
            if (!string.IsNullOrEmpty(error))
            {
                return false;
            }
        }
        
        var msuPath = new FileInfo(project.MsuPath).DirectoryName;

        if (string.IsNullOrEmpty(msuPath)) return true;

        try
        {
            var sb = new StringBuilder();

            var trackCombos = project.Tracks.Where(t => t is { IsScratchPad: false, Songs.Count: > 1 })
                .Select(t => (t.Songs.First(s => !s.IsAlt), t.Songs.First(s => s.IsAlt))).ToList();

            if (otherProjects != null)
            {
                foreach (var otherProject in otherProjects)
                {
                    trackCombos.AddRange(otherProject.Tracks.Where(t => t is { IsScratchPad: false, Songs.Count: > 1 })
                        .Select(t => (t.Songs.First(s => !s.IsAlt), t.Songs.First(s => s.IsAlt))));
                }
            }

            foreach (var combo in trackCombos)
            {
                var basePath = Path.GetRelativePath(msuPath, combo.Item1.OutputPath ?? "");
                var baseAltPath = basePath.Replace($"-{combo.Item1.TrackNumber}.pcm",
                    $"-{combo.Item1.TrackNumber}_Original.pcm");
                var altSongPath = Path.GetRelativePath(msuPath, combo.Item2.OutputPath ?? "");

                sb.AppendLine($"IF EXIST \"{baseAltPath}\" (");
                sb.AppendLine($"\tRENAME \"{basePath}\" \"{altSongPath}\"");
                sb.AppendLine($"\tRENAME \"{baseAltPath}\" \"{basePath}\"");
                sb.AppendLine($") ELSE IF EXIST \"{altSongPath}\" (");
                sb.AppendLine($"\tRENAME \"{basePath}\" \"{baseAltPath}\"");
                sb.AppendLine($"\tRENAME \"{altSongPath}\" \"{basePath}\"");
                sb.AppendLine($")");
                sb.AppendLine();
            }

            var text = sb.ToString();
            File.WriteAllText(project.GetAltSwapperPath(), text);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unable to export AltSwapperFile");
            return false;
        }
    }

    public bool ValidateProject(MsuProjectViewModel project, out string message)
    {
        var msuPath = project.MsuPath;
        var msu = msuLookupService.LoadMsu(msuPath, saveToCache: false, ignoreCache: true, forceLoad: true);

        if (msu == null)
        {
            message = "Could not load MSU.";
            statusBarService.UpdateStatusBar("YAML File Validation Failed");
            return false;
        }
        
        var projectSongs = project.Tracks.Where(x => !x.IsScratchPad).SelectMany(x => x.Songs).ToList();

        var projectTrackNumbers = projectSongs.Select(x => x.TrackNumber).Order().ToList();
        var msuTrackNumbers = msu.Tracks.Where(x => !x.IsCopied).Select(x => x.Number).Order().ToList();
        if (!projectTrackNumbers.SequenceEqual(msuTrackNumbers))
        {
            foreach (var trackNumber in projectTrackNumbers.Concat(msuTrackNumbers).Distinct())
            {
                var projPaths = project.Tracks.First(x => x.TrackNumber == trackNumber).Songs
                    .Select(x => x.OutputPath)
                    .ToList();
                var msuPaths = msu.Tracks.Where(x => x.Number == trackNumber).Select(x => x.Path).ToList();

                var missingMsuPaths = projPaths.Where(x => !msuPaths.Contains(x ?? "")).ToList();
                if (missingMsuPaths.Any())
                {
                    message = $"{string.Join(", ", missingMsuPaths)} found in the project but not the generated MSU YAML file";
                    statusBarService.UpdateStatusBar("YAML File Validation Failed");
                    return false;
                }
                
                var missingProjPaths = msuPaths.Where(x => !projPaths.Contains(x ?? "")).ToList();
                if (missingProjPaths.Any())
                {
                    message = $"{string.Join(", ", missingProjPaths)} found in the generated MSU YAML file but not the project";
                    statusBarService.UpdateStatusBar("YAML File Validation Failed");
                    return false;
                }
            }
            message = "Could not load all tracks from the YAML file.";
            statusBarService.UpdateStatusBar("YAML File Validation Failed");
            return false;
        }

        foreach (var projectSong in projectSongs)
        {
            var filename = new FileInfo(projectSong.OutputPath!).Name;
            var msuTrack = msu.Tracks.FirstOrDefault(x => x.Path.EndsWith(filename));

            if (msuTrack == null)
            {
                message = $"Could not find track for song {projectSong.SongName} in the YAML file.";
                statusBarService.UpdateStatusBar("YAML File Validation Failed");
                return false;
            }
            else if ((projectSong.SongName ?? "") != msuTrack.SongName || (projectSong.Album ?? "") != (msuTrack.Album ?? "") ||
                     (projectSong.Artist ?? "") != (msuTrack.Artist ?? "") || (projectSong.Url ?? "") != (msuTrack.Url ?? ""))
            {
                message = $"Detail mismatch for song {projectSong.SongName} under track #{projectSong.TrackNumber}.";
                statusBarService.UpdateStatusBar("YAML File Validation Failed");
                return false;
            }
        }
            
        message = "";
        statusBarService.UpdateStatusBar("YAML File Validated Successfully");
        return true;
    }

    private string GetProjectBackupFilePath(string projectFilePath)
    {
        var file = new FileInfo(projectFilePath);
        byte[] inputBytes = Encoding.ASCII.GetBytes(file.FullName);
        byte[] hashBytes = System.Security.Cryptography.MD5.HashData(inputBytes);
        return Path.Combine(GetBackupDirectory(), $"{Convert.ToHexString(hashBytes)}_{file.Name}");
    }
    
    private string GetProjectMsuPcmCacheFilePath(string projectFilePath)
    {
        var file = new FileInfo(projectFilePath);
        var inputBytes = Encoding.ASCII.GetBytes(file.FullName);
        var hashBytes = System.Security.Cryptography.MD5.HashData(inputBytes);
        return Path.Combine(Directories.CacheFolder, $"{Convert.ToHexString(hashBytes)}_{Path.GetFileNameWithoutExtension(projectFilePath)}");
    }
    
    private string GetProjectMsuPcmGenerationFilePath(string projectFilePath)
    {
        var file = new FileInfo(projectFilePath);
        var inputBytes = Encoding.ASCII.GetBytes(file.FullName);
        var hashBytes = System.Security.Cryptography.MD5.HashData(inputBytes);
        return Path.Combine(Directories.TempFolder, $"{Convert.ToHexString(hashBytes)}_{Path.GetFileNameWithoutExtension(projectFilePath)}");
    }

    private string GetBackupDirectory()
    {
        return Path.Combine(Directories.BaseFolder, "backups");
    }
}