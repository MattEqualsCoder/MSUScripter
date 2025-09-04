using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AvaloniaControls.Services;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using MSUScripter.Models;

namespace MSUScripter.Services;

public class DependencyInstallerService(ILogger<DependencyInstallerService> logger)
{
    private const string PythonWindowsDownloadUrl = "https://github.com/astral-sh/python-build-standalone/releases/download/20250828/cpython-3.13.7+20250828-x86_64-pc-windows-msvc-install_only_stripped.tar.gz";
    private const string PythonLinuxDownloadUrl = "https://github.com/astral-sh/python-build-standalone/releases/download/20250828/cpython-3.13.7+20250828-x86_64_v3-unknown-linux-gnu-install_only_stripped.tar.gz";
    private const string MsuPcmWindowsDownloadUrl = "https://github.com/qwertymodo/msupcmplusplus/releases/download/v1.0RC3/msupcm.exe";
    private const string MsuPcmLinuxDownloadUrl = "https://github.com/qwertymodo/msupcmplusplus/releases/download/v1.0RC3/msupcm";
    private const string FfmpegWindowsDownloadUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2024-08-31-12-50/ffmpeg-n7.0.2-6-g7e69129d2f-win64-lgpl-shared-7.0.zip";
    private const string FfmpegLinuxDownloadUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2024-08-31-12-50/ffmpeg-n7.0.2-6-g7e69129d2f-linux64-lgpl-shared-7.0.tar.xz";

    public async Task<bool> InstallPyApp(Action<string> response, Func<string, string, Task<RunPyResult>> runPyFunc)
    {
        var successful = false;

        try
        {
            response.Invoke("Creating directories");
            var destination = Path.Combine(Directories.Dependencies, "python");

            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, true);
            }

            EnsureFolders(destination);

            var copyFrom = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? "", "dependencies", "python");
            if (Directory.Exists(copyFrom))
            {
                response.Invoke("Copying files");
                if (!await CopyDirectory(copyFrom, destination))
                {
                    return false;
                }
            }
            else
            {
                var tempFile = Path.Combine(Directories.TempFolder, "python.tar.gz");
                var url = OperatingSystem.IsWindows() ? PythonWindowsDownloadUrl : PythonLinuxDownloadUrl;

                response.Invoke("Downloading python");
                if (!await DownloadFileAsync(url, tempFile))
                {
                    return false;
                }

                response.Invoke("Extracting files");
                if (!await ExtractTarGzFile(tempFile, Directories.Dependencies))
                {
                    return false;
                }
            }

            var pythonPath = OperatingSystem.IsWindows()
                ? Path.Combine(destination, "python.exe")
                : Path.Combine(destination, "bin", "python3.13");

            if (OperatingSystem.IsLinux())
            {
                File.SetUnixFileMode(pythonPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            response.Invoke("Verifying Python version");

            var runPyResult = await runPyFunc(pythonPath, "--version");
            if (!runPyResult.Success || !runPyResult.Result.StartsWith("Python 3"))
            {
                logger.LogError("Python version response incorrect: {Response} | {Error}", runPyResult.Result,
                    runPyResult.Error);
                return false;
            }
            
            response.Invoke("Installing companion app");

            runPyResult = await runPyFunc(pythonPath, "-m pip install py-msu-scripter-app");
            if (!runPyResult.Success && !runPyResult.Error.StartsWith("[notice]"))
            {
                logger.LogError("Failed to install Python companion app: {Error}", runPyResult.Error);
                return false;
            }

            successful = true;
            return true;
        }
        catch (TaskCanceledException)
        {
            // Do nothing
            return successful;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unknown error installing Python companion app");
            return false;
        }
    }
    
    public async Task<bool> InstallMsuPcm(Action<string> progress)
    {
        var successful = false;
        
        try
        {
            progress.Invoke("Creating directory");
            EnsureFolders();
            
            var destination = Path.Combine(Directories.Dependencies, OperatingSystem.IsWindows() ? "msupcm.exe" : "msupcm");

            if (File.Exists(destination))
            {
                File.Delete(destination);
            }
            
            var url = OperatingSystem.IsWindows() ? MsuPcmWindowsDownloadUrl : MsuPcmLinuxDownloadUrl;
            progress.Invoke("Downloading MsuPcm++");
            if (!await DownloadFileAsync(url, destination))
            {
                return false;
            }

            if (OperatingSystem.IsLinux())
            {
                File.SetUnixFileMode(destination, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        
            successful = File.Exists(destination);
            return successful;
        }
        catch (TaskCanceledException)
        {
            // Do nothing
            return successful;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unknown error installing MsuPcm++");
            return false;
        }
    }

    public async Task<bool> InstallFfmpeg(Action<string> progress)
    {
        var successful = false;
        
        try
        {
            progress.Invoke("Creating directories");
            var tempExtractionPath = Path.Combine(Directories.TempFolder, "ffmpeg");
            var destination = Path.Combine(Directories.Dependencies, "ffmpeg");
            
            if (Directory.Exists(tempExtractionPath))
            {
                Directory.Delete(tempExtractionPath, true);
            }
            
            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, true);
            }
            
            EnsureFolders(tempExtractionPath, destination);
            
            var tempFileName = OperatingSystem.IsWindows() ? "ffmpeg.zip" : "ffmpeg.tar.xz";
            var tempFilePath = Path.Combine(Directories.TempFolder, tempFileName);
            var url = OperatingSystem.IsWindows() ? FfmpegWindowsDownloadUrl : FfmpegLinuxDownloadUrl;
            
            progress.Invoke("Downloading FFmpeg");
            if (!await DownloadFileAsync(url, tempFilePath))
            {
                return false;
            }
            
            progress.Invoke("Extracting files");
            if (OperatingSystem.IsWindows() && !await ExtractZipFile(tempFilePath, tempExtractionPath))
            {
                return false;
            }
            if (OperatingSystem.IsLinux() && !await ExtractTarXzFile(tempFilePath, tempExtractionPath))
            {
                return false;
            }

            var subDirectory = Directory.GetDirectories(tempExtractionPath).FirstOrDefault();
            if (!Directory.Exists(subDirectory))
            {
                return false;
            }

            progress.Invoke("Copying files");
            successful = await CopyDirectory(subDirectory, destination);
            
            if (OperatingSystem.IsLinux())
            {
                var executablePath = Path.Combine(destination, "bin", "ffmpeg");
                File.SetUnixFileMode(executablePath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                executablePath = Path.Combine(destination, "bin", "ffprobe");
                File.SetUnixFileMode(executablePath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                executablePath = Path.Combine(destination, "bin", "ffplay");
                File.SetUnixFileMode(executablePath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            
            return successful;

        }
        catch (TaskCanceledException)
        {
            // Do nothing
            return successful;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unknown error installing FFmpeg");
            return false;
        }
    }
    
    private async Task<bool> ExtractTarGzFile(string inputPath, string outputDirectory)
    {
        logger.LogInformation("Extracting files from {File} to {Destination}", inputPath, outputDirectory);

        var success = false;
        try
        {
            await ITaskService.Run(() =>
            {
                try
                {
                    using var inStream = File.OpenRead(inputPath);
                    using var gzipStream = new GZipInputStream(inStream);
                    var tarArchive = TarArchive.CreateInputTarArchive(gzipStream, Encoding.UTF8);
                    tarArchive.ExtractContents(outputDirectory);
                    tarArchive.Close();
                    success = true;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error extracting files");
                }
            });
        }
        catch (TaskCanceledException)
        {
            // Do nothing
        }
        
        return success;
    }

    private async Task<bool> ExtractTarXzFile(string inputPath, string outputDirectory)
    {
        logger.LogInformation("Extracting files from {File} to {Destination}", inputPath, outputDirectory);
        var procStartInfo = new ProcessStartInfo("tar")
        {
            Arguments = $"xf \"{inputPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = outputDirectory
        };
        using var process = new Process();
        process.StartInfo = procStartInfo;
        process.Start();
        await process.WaitForExitAsync();
        return process.ExitCode == 0;
    }
    
    private async Task<bool> ExtractZipFile(string inputPath, string outputDirectory)
    {
        logger.LogInformation("Extracting files from {File} to {Destination}", inputPath, outputDirectory);

        var success = false;
        try
        {
            await ITaskService.Run(() =>
            {
                try
                {
                    var fastZip = new FastZip();
                    fastZip.ExtractZip(inputPath, outputDirectory, null);
                    success = true;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error extracting files");
                }
            });
        }
        catch (TaskCanceledException)
        {
            // Do nothing
        }
        
        return success;
    }

    private async Task<bool> CopyDirectory(string sourceDir, string targetDir)
    {
        logger.LogInformation("Copying {Source} to {Destination}", sourceDir, targetDir);
        
        var success = false;
        try
        {
            await ITaskService.Run(() =>
            {
                try
                {
                    CopyDirectoryInternal(sourceDir, targetDir);
                    logger.LogInformation("Successfully copied files");
                    success = true;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error copying directory");
                }
            });
        }
        catch (TaskCanceledException)
        {
            // Do nothing
        }
        
        return success;
    }
    
    private void CopyDirectoryInternal(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach(var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));

        foreach(var directory in Directory.GetDirectories(sourceDir))
            CopyDirectoryInternal(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
    }
    
    private async Task<bool> DownloadFileAsync(string url, string target)
    {
        logger.LogInformation("Downloading {Url} to {Target}", url, target);
        using var httpClient = new HttpClient();

        try
        {
            await using (var downloadStream = await httpClient.GetStreamAsync(url))
            await using (var fileStream = new FileStream(target, FileMode.Create))
            {
                await downloadStream.CopyToAsync(fileStream);
            }
            logger.LogInformation("Successfully downloaded file");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Error downloading file");
            return false;
        }
    }

    private void EnsureFolders(params string[] additionalFolders)
    {
        if (!Directory.Exists(Directories.Dependencies))
        {
            Directory.CreateDirectory(Directories.Dependencies);
        }

        if (!Directory.Exists(Directories.TempFolder))
        {
            Directory.CreateDirectory(Directories.TempFolder);
        }

        foreach (var additionalFolder in additionalFolders)
        {
            if (!string.IsNullOrEmpty(additionalFolder) && !Directory.Exists(additionalFolder))
            {
                Directory.CreateDirectory(additionalFolder);
            }
        }
    }
}