using System;
using System.Collections.Concurrent;

namespace MSUScripter.Models;

public class MsuProjectGenerationCache
{
    public readonly ConcurrentDictionary<string, MsuProjectSongCache> Songs = [];
}

public class MsuProjectSongCache
{
    public ulong JsonHash { get; init; }
    public int JsonLength { get; init; }
    public DateTime FileGenerationTime { get; init; }
    public long FileLength { get; init; }

    public static bool IsValid(MsuProjectSongCache? a, MsuProjectSongCache? b)
    {
        if (a is null || b is null) return false;
        return a.JsonHash == b.JsonHash && a.JsonLength == b.JsonLength &&
               a.FileGenerationTime == b.FileGenerationTime & a.FileLength == b.FileLength;
    }
}