using System.Text;
using static FastCdcFs.Net.FastCdcFsReader;

namespace FastCdcFs.Net;

internal class FastCdcFsHelper
{

    public static string Normalize(string? path)
    {
        path = path ?? "";

        if (path.StartsWith('/'))
        {
            path = path[1..];
        }

        return path;
    }

    public static string PathCombine(string a, string b)
        => a is "" ? b : $"{a}/{b}";

    public static string? GetDirectoryName(string path)
    {
        var directory = Path.GetDirectoryName(path);
        return directory is null
            ? null
            : directory.Replace('\\', '/');
    }

    public static int Read(Stream s, byte[] buffer, int offset, int count)
    {
        var total = 0;

        while (count > 0)
        {
            var read = s.Read(buffer, offset, count);
            offset += read;
            total += read;
            count -= read;
        }

        return total;
    }

    public static string Dump(FastCdcFsReader reader)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Version {reader.Version}");
        sb.AppendLine();

        sb.AppendLine("Directories:");
        DumpDirectories(sb, reader.Directories);
        sb.AppendLine();

        sb.AppendLine("Files:");
        DumpFiles(sb, reader.Files);
        sb.AppendLine();

        sb.AppendLine("Chunks:");
        DumpChunks(sb, reader.Chunks);
        sb.AppendLine();

        return sb.ToString();
    }

    private static void DumpChunks(StringBuilder sb, IReadOnlyCollection<ChunkInfo> chunks)
    {
        var grid = new ConsoleGrid(4);

        grid.Add("Id", "Offset", "Length", "Hash");

        var id = 0;
        foreach (var chunk in chunks)
        {
            grid.Add(id, chunk.Offset, chunk.Length, chunk.Hash is not 0 ? chunk.Hash.ToString() : null);
            id++;
        }

        sb.AppendLine(grid.ToString());
    }

    private static void DumpFiles(StringBuilder sb, IReadOnlyDictionary<string, (uint Length, uint[] ChunkIds)> files)
    {
        var grid = new ConsoleGrid(4);

        grid.Add("Name", "Directory", "Length", "SolidBlockId");

        foreach (var name in files.Keys)
        {
            var entry = files[name];
            grid.Add(Path.GetFileName(name), GetDirectoryName(name), entry.Length);
        }

        sb.AppendLine(grid.ToString());
    }

    private static void DumpDirectories(StringBuilder sb, IReadOnlyCollection<InternalDirectoryEntry> directories)
    {
        var grid = new ConsoleGrid(4);

        grid.Add("Id", "ParentId", "Name", "FullName");

        foreach (var entry in directories)
        {
            grid.Add(entry.Id, entry.ParentId, entry.Name, entry.FullName);
        }

        sb.AppendLine(grid.ToString());
    }
}
