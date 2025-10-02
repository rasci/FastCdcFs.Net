using System.Diagnostics.CodeAnalysis;
using System.Text;
using ZstdSharp;

namespace FastCdcFs.Net;

public struct Range(uint offset, uint length)
{
    public uint Offset = offset, Length = length;

    public override string ToString()
        => $"Offset {Offset} Length {Length}";
}

public class Entry
{
    private readonly FastCdcFsReader reader;

    internal Entry(FastCdcFsReader reader, string fullName, string name, uint length, bool isFile)
    {
        this.reader = reader;

        FullName = fullName;
        Name = name;
        Length = length;
        IsFile = isFile;
    }

    public string FullName { get; init; }

    public string Name { get; init; }

    public uint Length { get; init; }

    public bool IsFile { get; init; }

    public bool IsDirectory => !IsFile;

    public Stream Open()
        => IsFile
            ? reader.OpenFile(FullName)
            : throw new InvalidOperationException("Cannot open a directory");

    public byte[] ReadAllBytes()
        => IsFile
            ? reader.ReadFile(FullName)
            : throw new InvalidOperationException("Cannot open a directory");
}

public abstract class FastCdcFsException(string message) : Exception(message);

public class InvalidFastCdcFsFileException(string path) : FastCdcFsException($"Invalid file {path}");

public class FastCdcFsReader : IDisposable
{
    private record InternalDirectoryEntry(uint Id, uint ParentId, string Name, string FullName);

    private readonly Dictionary<string, (uint Length, uint[] ChunkIds)> files = [];
    private readonly Stream s;
    private readonly BinaryReader br;
    private readonly bool leaveOpen, compressed;

    private Modes mode;
    private InternalDirectoryEntry[] directories;
    private byte[]? compressionDict;
    private Range[] chunks;
    private uint dataOffset;

    public FastCdcFsReader(string path)
        : this(new FileStream(path, FileMode.Open, FileAccess.Read), false)
    {
    }

    public FastCdcFsReader(Stream s, bool leaveOpen = true)
    {
        this.s = s;
        this.leaveOpen = leaveOpen;
        br = new(s, Encoding.UTF8, leaveOpen);

        if (br.ReadString() != "FastCdcFs")
            throw new InvalidFastCdcFsFileException("Not a FastCdcFs file");

        mode = (Modes)br.ReadByte();
        compressed = (mode & Modes.NoZstd) is 0;

        ReadDirectories();
        ReadFiles();
        ReadChunks();

        dataOffset = (uint)s.Position;
    }

    public Entry Get(string? path)
    {
        path = FastCdcFsHelper.Normalize(path);

        if (files.TryGetValue(path, out var e))
            return new(this, path, Path.GetFileName(path), e.Length, true);

        var entry = directories.FirstOrDefault(e => e.FullName == path);
        return entry is null
            ? throw new FileNotFoundException(path)
            : new(this, entry.FullName, entry.Name, 0, false);
    }

    public IReadOnlyCollection<Entry> List(string? directory = null)
    {
        directory = FastCdcFsHelper.Normalize(directory);

        var entry = this.directories.FirstOrDefault(e => e.FullName == directory);
        if (entry is null)
            throw new DirectoryNotFoundException(directory);

        var directories = this.directories
            .Where(d => d.ParentId == entry.Id && d.Id > 0)
            .OrderBy(d => d.Name)
            .Select(d => new Entry(this, d.FullName, d.Name, 0, false));

        var files = this.files
            .Where(f => FastCdcFsHelper.GetDirectoryName(f.Key) == entry.FullName)
            .Select(f => new Entry(this, f.Key, Path.GetFileName(f.Key), f.Value.Length, true))
            .OrderBy(e => e.Name);

        return directories.Concat(files).ToArray();
    }

    internal Stream OpenFile(string path)
        => files.TryGetValue(path, out var e)
            ? new FastCdcFsStream(s, dataOffset, compressionDict, chunks, e.ChunkIds, e.Length, compressed)
            : throw new FileNotFoundException(path);

    internal byte[] ReadFile(string path)
    {
        if (!files.TryGetValue(path, out var e))
            throw new FileNotFoundException(path);

        var data = new byte[e.Length];
        var offset = 0u;

        for (var i = 0; i < e.ChunkIds.Length; i++)
        {
            var range = chunks[e.ChunkIds[i]];

            s.Position = dataOffset + range.Offset;

            if (compressed)
            {
                using var decompressor = new Decompressor();
                decompressor.LoadDictionary(compressionDict);

                using var ds = new DecompressionStream(s, decompressor);

                var total = 0;

                while (total < range.Length)
                {
                    var read = ds.Read(data, (int)offset, (int)range.Length - total);
                    offset += (uint)read;
                    total += read;
                }
            }
            else
            {
                var total = 0;

                while (total < range.Length)
                {
                    var read = s.Read(data, (int)offset, (int)range.Length - total);
                    offset += (uint)read;
                    total += read;
                }
            }
        }

        return data;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine("directories:");

        for (var i = 0; i < directories.Length; i++)
        {
            sb.AppendLine($"{i}: {directories[i].FullName}");
        }

        sb.AppendLine();
        sb.AppendLine("files:");

        foreach (var name in files.Keys)
        {
            var info = files[name];
            sb.AppendLine($"{name}: len: {info.Length} [{string.Join(',', info.ChunkIds)}]");
        }

        return sb.ToString();
    }

    public void Dispose()
    {
        br.Dispose();

        if (leaveOpen)
        {
            s.Dispose();
        }
    }

    [MemberNotNull(nameof(chunks))]
    private void ReadChunks()
    {
        if (compressed)
        {
            compressionDict = new byte[br.ReadUInt32()];
            br.Read(compressionDict, 0, compressionDict.Length);
        }

        chunks = new Range[br.ReadUInt32()];
        var offset = 0u;

        for (var i = 0u; i < chunks.Length; i++)
        {
            var length = br.ReadUInt32();
            chunks[i] = new(offset, length);
            offset += compressed ? br.ReadUInt32() : length;
        }
    }

    private void ReadFiles()
    {
        var files = br.ReadUInt32();

        for (var i = 0u; i < files; i++)
        {
            ReadFile(i);
        }
    }

    private void ReadFile(uint fileId)
    {
        var directoryId = br.ReadUInt32();
        var name = br.ReadString();
        var length = br.ReadUInt32();

        var chunkIds = new uint[br.ReadUInt32()];

        for (var i = 0; i < chunkIds.Length; i++)
        {
            chunkIds[i] = br.ReadUInt32();
        }

        files.Add(FastCdcFsHelper.PathCombine(directories[directoryId].FullName, name), (length, chunkIds));
    }

    [MemberNotNull(nameof(directories))]
    private void ReadDirectories()
    {
        var length = br.ReadUInt32();
        directories = new InternalDirectoryEntry[length + 1];
        directories[0] = new(0, 0, "", "");

        for (var i = 0u; i < length; i++)
        {
            var parentId = br.ReadUInt32();
            var name = br.ReadString();
            directories[i + 1] = new(i + 1, parentId, name, FastCdcFsHelper.PathCombine(directories[parentId].FullName, name));
        }
    }
}
