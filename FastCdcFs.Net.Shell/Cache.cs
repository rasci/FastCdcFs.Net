using System.Text.Json;

namespace FastCdcFs.Net.Shell;

internal record CacheEntry(string SourcePath, string? TargetPath);

internal record CacheDirectoryEntry(string SourcePath, string? TargetPath, bool Recursive) : CacheEntry(SourcePath, TargetPath);

internal class Cache
{
    private const string CacheFileName = ".cdcfs.cache";

    public List<CacheEntry> Files { get; set; } = [];

    public List<CacheDirectoryEntry> Directories { get; set; } = [];

    public static void AddFile(string file, string? targetPath)
    {
        var cache = Deserialize();
        cache.Files.Add(new(file, targetPath));
        cache.Serialize();
    }

    public static void AddDirectory(string dir, string? targetPath, bool recursive)
    {
        var cache = Deserialize();
        cache.Directories.Add(new(dir, targetPath, recursive));
        cache.Serialize();
    } 
    
    public static Cache ReadAndDelete()
    {
        var cache = Deserialize();

        if (File.Exists(CacheFileName))
        {
            File.Delete(CacheFileName);
        }

        return cache;
    }

    private static Cache Deserialize()
        => File.Exists(CacheFileName)
            ? JsonSerializer.Deserialize<Cache>(File.ReadAllText(CacheFileName)) ?? new()
            : new();

    private void Serialize()
        => File.WriteAllText(CacheFileName, JsonSerializer.Serialize(this) ?? "");
}
