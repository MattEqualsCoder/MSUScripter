using System;
using System.Collections.Concurrent;

namespace MSUScripter.Models;

public class MsuProjectGenerationCache
{
    public ConcurrentDictionary<string, MsuProjectSongCache> Songs { get; set; } = [];
}

public class MsuProjectSongCache
{
    public const int CurrentCacheVersion = 1;
    public ulong JsonHash { get; init; }
    public int JsonLength { get; init; }
    public DateTime FileGenerationTime { get; init; }
    public long FileLength { get; init; }
    public int? CacheVersion { get; init; }
    public float PostGenerateVolumeModifier { get; init; }
    public bool IsPostGenerateVolumeDecibels { get; init; }

    public static bool IsValid(MsuProjectSongCache? a, MsuProjectSongCache? b)
    {
        if (a is null || b is null) return false;
        return a.JsonHash == b.JsonHash && a.JsonLength == b.JsonLength &&
               a.FileGenerationTime == b.FileGenerationTime & a.FileLength == b.FileLength && a.CacheVersion == b.CacheVersion &&
               Math.Abs(a.PostGenerateVolumeModifier - b.PostGenerateVolumeModifier) < 0.01 &&  a.IsPostGenerateVolumeDecibels == b.IsPostGenerateVolumeDecibels;
    }
}