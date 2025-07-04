using System;
using System.Collections.Concurrent;

namespace MSUScripter.Models;

public class MsuProjectGenerationCache
{
    public ConcurrentDictionary<string, MsuProjectSongCache> Songs = [];
}

public class MsuProjectSongCache
{
    public ulong JsonHash { get; set; }
    public int JsonLength { get; set; }
    public DateTime FileGenerationTime { get; set; }
    public long FileLength { get; set; }

    public static bool IsValid(MsuProjectSongCache? a, MsuProjectSongCache? b)
    {
        if (a is null || b is null) return false;
        return a.JsonHash == b.JsonHash && a.JsonLength == b.JsonLength &&
               a.FileGenerationTime == b.FileGenerationTime & a.FileLength == b.FileLength;
    }
}