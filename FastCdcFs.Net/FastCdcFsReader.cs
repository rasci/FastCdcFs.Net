using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;
using System.Text;

namespace FastCdcFs.Net;

public record ChunkInfo(uint Offset, uint Length, ulong Hash)
{
    public override string ToString()
        => Hash is 0
            ? $"Offset {Offset} Length {Length}"
            : $"Offset {Offset} Length {Length} Hash {Hash}";
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

public class FastCdcFsReader : IDisposable
{
    private record InternalDirectoryEntry(uint Id, uint ParentId, string Name, string FullName);

    private readonly Dictionary<string, (uint Length, uint[] ChunkIds)> files = [];
    private readonly ChunkReader chunkReader;
    private readonly Stream s;
    private readonly BinaryReader br;
    private readonly bool leaveOpen, compressed, hashed;

    private InternalDirectoryEntry[] directories;
    private byte[]? compressionDict;
    private ChunkInfo[] chunks;
    private int dataOffset;

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

        Version = br.ReadByte();
        if (Version is not 1)
            throw new InvalidFastCdcFsVersionException(Version);

        var mode = (Modes)br.ReadByte();
        compressed = (mode & Modes.NoZstd) is 0;
        hashed = (mode & Modes.NoHash) is 0;

        ReadMetaData();

        dataOffset = (int)s.Position;
        chunkReader = new(s, compressed, hashed, compressionDict, dataOffset);
    }

    [MemberNotNull(nameof(directories), nameof(chunks))]
    private void ReadMetaData()
    {
        var metaDataLength = br.ReadUInt32();
        var metaData = br.ReadBytes((int)metaDataLength);

        if (hashed)
        {
            VerifyMetaHash(metaData);
        }

        using var memoryStream = new MemoryStream(metaData);
        using var metaStream = compressed
            ? (Stream)new ZstdSharp.DecompressionStream(memoryStream)
            : memoryStream;
        using var metaBr = new BinaryReader(metaStream, Encoding.UTF8, true);

        ReadDirectories(metaBr);
        ReadFiles(metaBr);
        ReadChunks(metaBr);
    }

    public byte Version { get; private set; }

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
            ? new FastCdcFsStream(chunkReader, chunks, e.Length, e.ChunkIds)
            : throw new FileNotFoundException(path);

    internal byte[] ReadFile(string path)
    {
        if (!files.TryGetValue(path, out var e))
            throw new FileNotFoundException(path);

        var data = new byte[e.Length];
        var offset = 0;

        for (var i = 0; i < e.ChunkIds.Length; i++)
        {
            var chunkId = e.ChunkIds[i];
            var chunkInfo = chunks[e.ChunkIds[i]];
            chunkReader.ReadChunk(chunkId, chunkInfo, data, offset);
            offset += (int)chunkInfo.Length;
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

    private void VerifyMetaHash(byte[] metaData)
    {
        var hasher = new XxHash64();
        hasher.Append(metaData);
        var currentHash = hasher.GetCurrentHashAsUInt64();
        var expectedHash = br.ReadUInt64();
        if (currentHash != expectedHash)
            throw new CorruptedMetaDataException();
    }

    [MemberNotNull(nameof(chunks))]
    private void ReadChunks(BinaryReader br)
    {
        if (compressed)
        {
            compressionDict = new byte[br.ReadUInt32()];
            FastCdcFsHelper.Read(br.BaseStream, compressionDict, 0, compressionDict.Length);
        }

        chunks = new ChunkInfo[br.ReadUInt32()];
        var offset = 0u;

        for (var i = 0u; i < chunks.Length; i++)
        {
            var length = br.ReadUInt32();
            var noff = compressed ? br.ReadUInt32() : length;
            var hash = hashed ? br.ReadUInt64() : 0;
            chunks[i] = new(offset, length, hash);
            offset += noff;
        }
    }

    private void ReadFiles(BinaryReader br)
    {
        var files = br.ReadUInt32();

        for (var i = 0u; i < files; i++)
        {
            ReadFile(br, i);
        }
    }

    private void ReadFile(BinaryReader br, uint fileId)
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
    private void ReadDirectories(BinaryReader br)
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
