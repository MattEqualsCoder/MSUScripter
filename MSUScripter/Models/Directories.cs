using System;
using System.Diagnostics;
using System.IO;

namespace MSUScripter.Models;

public static class Directories
{
    public static string BaseFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MSUScripter");

    public static string LogFolder => Path.Combine(BaseFolder, "logs");
    
    public static string Dependencies => Path.Combine(BaseFolder, "dependencies");
    
    public static string CacheFolder
    {
        get
        {
            var path = Path.Combine(BaseFolder, "cache");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
    }

    public static string TempFolder
    {
        get
        {
            var path = Path.Combine(Path.GetTempPath(), "MSUScripter");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
    }
}