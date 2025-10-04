using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;
using ZstdSharp;

namespace FastCdcFs.Net;

internal record DirectoryInfo(uint Id, uint ParentId, string Name);

internal record FileInfo(uint Id, uint DirectoryId, string Name, uint Length)
{
    public List<uint> ChunkIds { get; } = [];
}

public record Options(uint FastCdcMinSize, uint FastCdcAverageSize, uint FastCdcMaxSize, bool NoZstd, bool NoHash)
{
    public static Options Default => new(1024 * 32, 1024 * 64, 1024 * 256, false, false);

    public Options WithNoZstd(bool noZstd = true)
        => this with { NoZstd = noZstd };
        
    public Options WithNoHash(bool noHash = true)
        => this with { NoHash = noHash };

    public Options WithChunkSizes(uint minSize, uint averageSize, uint maxSize)
    {
        if (minSize == 0 || averageSize == 0 || maxSize == 0)
            throw new ArgumentException("Chunk sizes must be greater than zero");

        if (minSize > averageSize)
            throw new ArgumentException("Min size must be less than or equal to average size");

        if (averageSize > maxSize)
            throw new ArgumentException("Average size must be less than or equal to max size");

        return this with { FastCdcMinSize = minSize, FastCdcAverageSize = averageSize, FastCdcMaxSize = maxSize };
    }

    public override string ToString()
        => $"FastCdcMinSize {FastCdcMinSize}, FastCdcAverageSize {FastCdcAverageSize}, FastCdcMaxSize {FastCdcMaxSize}";
}

public class FastCdcFsWriter(Options options)
{

    public FastCdcFsWriter(Func<Options, Options>? configure = null)
        : this(configure?.Invoke(Options.Default) ?? Options.Default)
    {
    }

    public const byte Version = 1;

    private record ChunkInfo(uint Id, byte[] StrongHash, byte[] Data, uint Offset)
    {
        public uint NextOffset => Offset + (uint)Data.Length;

        public byte[]? CompressedData { get; set; }

        public ulong XxHash64 { get; set; }
    }

    private readonly Dictionary<string, DirectoryInfo> directories = [];
    private readonly List<FileInfo> files = [];
    private readonly List<ChunkInfo> chunks = [];
    private readonly DirectoryInfo root = new(0, 0, "");

    private uint nextFileId = 0, nextDirectoryId = 1, nextChunkId = 0;

    internal IReadOnlyCollection<uint> ChunkLengths => chunks.Select(c => (uint)c.Data.Length).ToArray();

    public long Length { get; private set; }

    public int CompressionRatePercentage { get; private set; }

    public void AddFile(string sourcePath, string targetPath)
    {
        AddFile(File.ReadAllBytes(sourcePath), targetPath);
    }

    public void AddFile(byte[] data, string targetPath)
    {
        var cdc = new FastCdc(data, options.FastCdcMinSize, options.FastCdcAverageSize, options.FastCdcMaxSize);

        using var ms = new MemoryStream(data);

        var file = CreateFile(targetPath, (uint)ms.Length);

        foreach (var chunk in cdc.GetChunks())
        {
            //Console.WriteLine($"chunk offset {chunk.Offset} length {chunk.Length} (stream position {ms.Position})");
            var chunkInfo = GetOrCreateChunk(chunk, ms);
            file.ChunkIds.Add(chunkInfo.Id);
        }
    }

    public void Build(string targetPath)
    {
        using var fs = new FileStream(targetPath, FileMode.OpenOrCreate, options.NoHash ? FileAccess.Write : FileAccess.ReadWrite);
        fs.SetLength(0);
        Build(fs);
    }

    public void Build(Stream s)
    {
        var pos = s.Position;

        using var bw = new BinaryWriter(s, Encoding.UTF8, true);
        bw.Write("FastCdcFs"); // magic
        bw.Write(Version);
        bw.Write((byte)GetModes(options));

        // write metadata
        using var memoryStream = new MemoryStream();
        using var compressionStream = new ZstdSharp.CompressionStream(memoryStream, new Compressor(22));
        using var bwMeta = new BinaryWriter(compressionStream, Encoding.UTF8, true);
        WriteDirectories(bwMeta);
        WriteFiles(bwMeta);
        compressionStream.Flush();
        var compressedMetaData = memoryStream.ToArray();

        // write length of metadata and metadata
        if (compressedMetaData.LongLength > uint.MaxValue)
            throw new InvalidOperationException("Compressed metadata exceeds 4GB and cannot be written as a 4-byte length.");
        bw.Write((uint)compressedMetaData.LongLength);
        bw.Write(compressedMetaData);

        WriteChunks(bw);

        Length = s.Position - pos;
    }

    private void WriteChunks(BinaryWriter bw)
    {
        HandleChunks(bw);

        bw.Write((uint)chunks.LongCount());

        foreach (var chunk in chunks)
        {
            bw.Write(chunk.Data.Length);

            if (!options.NoZstd)
            {
                bw.Write(chunk.CompressedData!.Length);
            }

            if (!options.NoHash)
            {
                bw.Write(chunk.XxHash64);
            }
        }

        if (!options.NoHash)
        {
            CreateAndWriteMetaDataHash(bw);
        }

        foreach (var chunk in chunks)
        {
            bw.Write(options.NoZstd ? chunk.Data : chunk.CompressedData!);
        }
    }

    private void CreateAndWriteMetaDataHash(BinaryWriter bw)
    {
        var data = new byte[bw.BaseStream.Position];
        bw.BaseStream.Position = 0;

        if (bw.BaseStream.Read(data, 0, data.Length) != data.Length)
            throw new FastCdcFsException("Unexpected count of bytes read");

        var hasher = new XxHash64();
        hasher.Append(data);
        var hash = hasher.GetCurrentHashAsUInt64();
        bw.Write(hash);
    }

    private void HandleChunks(BinaryWriter bw)
    {
        byte[] dict = [];

        if (!options.NoZstd)
        {
            dict = DictBuilder.TrainFromBuffer(chunks.Select(c => c.Data), 1024 * 1024);
            bw.Write((uint)dict.LongLength);
            bw.Write(dict);
        }
            
        Parallel.ForEach(chunks, c =>
        {
            if (!options.NoZstd)
            {
                using var compressor = new Compressor(22);
                compressor.LoadDictionary(dict);

                using var ms = new MemoryStream();
                using (var cs = new CompressionStream(ms, compressor))
                {
                    cs.Write(c.Data);
                    cs.Flush();
                }

                c.CompressedData = ms.ToArray();
            }
            
            if (!options.NoHash)
            {
                using var ms = new MemoryStream(c.Data);
                var hasher = new XxHash64();
                hasher.Append(ms);
                c.XxHash64 = hasher.GetCurrentHashAsUInt64();
            }
        });

        if (!options.NoZstd)
        {
            CompressionRatePercentage = options.NoZstd ? 0 : (int)(100 - (double)chunks.Sum(c => c.CompressedData!.Length) / chunks.Sum(c => c.Data.Length) * 100);
        }
    }

    private void WriteFiles(BinaryWriter bw)
    {
        bw.Write((uint)files.LongCount());

        foreach (var file in files)
        {
            WriteFile(bw, file);
        }
    }

    private void WriteFile(BinaryWriter bw, FileInfo file)
    {
        bw.Write(file.DirectoryId);
        bw.Write(file.Name);
        bw.Write(file.Length);
        bw.Write((uint)file.ChunkIds.LongCount());

        foreach (var chunkId in file.ChunkIds)
        {
            bw.Write(chunkId);
        }
    }

    private void WriteDirectories(BinaryWriter bw)
    {
        bw.Write((uint)directories.LongCount());

        foreach (var directory in directories.Values)
        {
            WriteDirectory(bw, directory);
        }
    }

    private void WriteDirectory(BinaryWriter bw, DirectoryInfo directory)
    {
        bw.Write(directory.ParentId);
        bw.Write(directory.Name);
    }

    private ChunkInfo GetOrCreateChunk(FastCdc.Chunk chunk, Stream s)
    {
        if (s.Position != chunk.Offset)
        {
            s.Position = chunk.Offset;
        }

        var data = new byte[chunk.Length];        
        if (s.Read(data, 0, data.Length) != chunk.Length)
            throw new FastCdcFsException("Unexpected count of bytes read");

        using var sha = SHA256.Create();
        var strongHash = sha.ComputeHash(data);

        var info = chunks.FirstOrDefault(c => c.StrongHash.SequenceEqual(strongHash));
        if (info is not null)
            return info;

        info = new(nextChunkId++, strongHash, data, chunks.Any() ? chunks.Last().NextOffset : 0);
        chunks.Add(info);
        return info;
    }

    private FileInfo CreateFile(string name, uint length)
    {
        var directory = GetOrCreateDirectory(FastCdcFsHelper.GetDirectoryName(name)!);
        var file = new FileInfo(nextFileId++, directory.Id, Path.GetFileName(name), length);
        files.Add(file);
        return file;
    }

    private DirectoryInfo GetOrCreateDirectory(string path)
    {
        if (path is "" or "/")
            return root;

        if (directories.TryGetValue(path, out var info))
            return info;

        var parent = GetOrCreateDirectory(FastCdcFsHelper.GetDirectoryName(path)!);
        info = new DirectoryInfo(nextDirectoryId++, parent.Id, Path.GetFileName(path));
        directories.Add(path, info);
        return info;
    }

    private static Modes GetModes(Options options)
    {
        var mode = Modes.None;

        if (options.NoZstd)
        {
            mode |= Modes.NoZstd;
        }

        if (options.NoHash)
        {
            mode |= Modes.NoHash;
        }

        return mode;
    }
}
