using System.Collections.Concurrent;

namespace qiyana.Services;

public static class LcuImageCache
{
    private static readonly ConcurrentDictionary<string, byte[]> Cache = new();

    public static byte[]? Get(string path)
    {
        return Cache.TryGetValue(path, out var bytes) ? bytes : null;
    }

    public static void Set(string path, byte[] bytes)
    {
        Cache[path] = bytes;
    }

    public static void Clear()
    {
        Cache.Clear();
    }
}
