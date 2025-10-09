using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;
using ZstdSharp;

namespace FastCdcFs.Net;

internal record DirectoryInfo(uint Id, uint ParentId, string Name);

internal record FileInfo(uint Id, uint DirectoryId, string Name, uint Length)
{
    public List<uint> ChunkIds { get; } = [];

    public uint? SolidBlockId { get; set; }

    public uint SolidBlockOffset { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append($"[{Id}] {Name} (DirId: {DirectoryId}, Length: {Length}");

        if (SolidBlockId is not null)
        {
            sb.Append($", SolidBlockId: {SolidBlockId}, SolidBlockOffset: {SolidBlockOffset}");
        }
        else
        {
            sb.Append($", Chunks: {string.Join(',', ChunkIds)}");
        }

        return sb.ToString();
    }
}

internal record SolidBlock(uint Id)
{
    public List<uint> ChunkIds { get; } = [];
    public MemoryStream Data { get; } = new();
}

public class FastCdcFsWriter(FastCdcFsOptions options)
{

    public FastCdcFsWriter(Func<FastCdcFsOptions, FastCdcFsOptions>? configure = null)
        : this(configure?.Invoke(FastCdcFsOptions.Default) ?? FastCdcFsOptions.Default)
    {
    }

    public const byte Version = 2;

    private record ChunkInfo(uint Id, byte[] StrongHash, byte[] Data, uint Offset)
    {
        public uint NextOffset => Offset + (uint)Data.Length;

        public byte[]? CompressedData { get; set; }

        public ulong XxHash64 { get; set; }
    }

    private readonly Dictionary<string, DirectoryInfo> directories = [];
    private readonly List<FileInfo> files = [];
    private readonly List<ChunkInfo> chunks = [];
    private readonly List<SolidBlock> solidBlocks = [];
    private readonly DirectoryInfo root = new(0, 0, "");

    private uint nextFileId = 0, nextDirectoryId = 1, nextChunkId = 0, nextSolidBlockId = 0;
    private SolidBlock? currentSolidBlock;

    internal IReadOnlyCollection<uint> ChunkLengths => chunks.Select(c => (uint)c.Data.Length).ToArray();

    public long Length { get; private set; }

    public int CompressionRatePercentage { get; private set; }

    public void AddDirectory(string sourcePath, bool recursive = true, string? targetRootDirectory = null)
    {
        if (!Directory.Exists(sourcePath))
            throw new DirectoryNotFoundException(sourcePath);

        foreach (var file in Directory.GetFiles(sourcePath, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
        {
            var relativePath = Path.GetRelativePath(sourcePath, file).Replace('\\', '/');
            var targetPath = string.IsNullOrEmpty(targetRootDirectory) || targetRootDirectory is "."
                ? relativePath
                : $"{targetRootDirectory.TrimEnd('/')}/{relativePath}";
         
            AddFile(file, targetPath);
        }
    }

    public void AddFile(string sourcePath, string targetPath)
    {
        AddFile(File.ReadAllBytes(sourcePath), targetPath);
    }

    public void AddFile(byte[] data, string targetPath)
    {
        var file = CreateFile(targetPath, (uint)data.Length);

        if (data.Length > 0)
        {
            if (data.Length < options.SmallFileThreshold)
            {
                AddToSolidBlock(file, data);
            }
            else
            {
                var cdc = new FastCdc(data, options.FastCdcMinSize, options.FastCdcAverageSize, options.FastCdcMaxSize);

                using var ms = new MemoryStream(data);

                foreach (var chunk in cdc.GetChunks())
                {
                    var chunkInfo = GetOrCreateChunk(chunk, ms);
                    file.ChunkIds.Add(chunkInfo.Id);
                }
            }
        }

        Console.WriteLine($"Added {file}");
    }

    public void Build(string targetPath)
    {
        using var fs = new FileStream(targetPath, FileMode.OpenOrCreate, FileAccess.Write);
        fs.SetLength(0);
        Build(fs);
    }

    public void Build(Stream s)
    {
        // Finalize any remaining solid block
        if (currentSolidBlock is not null)
        {
            FinalizeSolidBlock(currentSolidBlock);
            currentSolidBlock = null;
        }

        var pos = s.Position;

        using var bw = new BinaryWriter(s, Encoding.UTF8, true);
        bw.Write("FastCdcFs"); // magic
        bw.Write(Version);
        bw.Write((byte)GetModes(options));

        WriteMetaData(bw);
        WriteBlobs(bw);

        Length = s.Position - pos;
    }

    private void WriteMetaData(BinaryWriter bw)
    {
        using var memoryStream = new MemoryStream();
        using var metaStream = options.NoZstd
            ? (Stream)memoryStream
            : new CompressionStream(memoryStream, new Compressor(options.CompressionLevel));
        using var metaBw = new BinaryWriter(metaStream, Encoding.UTF8, true);

        WriteDirectories(metaBw);
        WriteFiles(metaBw);
        WriteSolidBlocks(metaBw);
        WriteChunks(metaBw);

        metaStream.Flush();        
        var metaData = memoryStream.ToArray();
        if (metaData.LongLength > uint.MaxValue)
            throw new FastCdcFsException("Compressed metadata exceeds 4GB and cannot be written as a 4-byte length.");

        bw.Write((uint)metaData.LongLength);
        bw.Write(metaData);

        if (!options.NoHash)
        {
            bw.Write(CreateMetaDataHash(metaData));
        }
    }

    private void WriteChunks(BinaryWriter bw)
    {
        var compressionDict = PrepareChunks(bw);

        if (!options.NoZstd)
        {
            if (compressionDict != null)
            {
                bw.Write((uint)compressionDict.LongLength);
                bw.Write(compressionDict);
            }
            else
            {
                bw.Write(0u);
            }
        }

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
    }

    private void WriteBlobs(BinaryWriter bw)
    {
        foreach (var chunk in chunks)
        {
            bw.Write(options.NoZstd ? chunk.Data : chunk.CompressedData!);
        }
    }

    private static ulong CreateMetaDataHash(byte[] metaData)
    {
        var hasher = new XxHash64();
        hasher.Append(metaData);
        var hash = hasher.GetCurrentHashAsUInt64();
        return hash;
    }

    private byte[]? PrepareChunks(BinaryWriter bw)
    {
        byte[]? compressionDict = null;
        if (!options.NoZstd && chunks.Count > 0)
        {
            try
            {
                compressionDict = DictBuilder.TrainFromBuffer(chunks.Select(c => c.Data), 1024 * 1024);
            }
            catch
            {
                // Dictionary training can fail with insufficient data, fall back to no dictionary
                compressionDict = null;
            }
        }

        Parallel.ForEach(chunks, c =>
        {
            if (!options.NoZstd)
            {
                using var compressor = new Compressor(options.CompressionLevel);
                compressor.LoadDictionary(compressionDict);

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

        if (!options.NoZstd && chunks.Count > 0)
        {
            CompressionRatePercentage = options.NoZstd ? 0 : (int)(100 - (double)chunks.Sum(c => c.CompressedData!.Length) / chunks.Sum(c => c.Data.Length) * 100);
        }

        return compressionDict;
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

        // Write solid block information (only for non-empty files in solid blocks)
        if (file.SolidBlockId.HasValue && file.Length > 0)
        {
            bw.Write((uint)0); // 0 chunk count indicates file is in a solid block
            bw.Write(file.SolidBlockId.Value);
            bw.Write(file.SolidBlockOffset);
        }
        else
        {
            bw.Write((uint)file.ChunkIds.LongCount());

            foreach (var chunkId in file.ChunkIds)
            {
                bw.Write(chunkId);
            }
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

    private void WriteSolidBlocks(BinaryWriter bw)
    {
        bw.Write((uint)solidBlocks.LongCount());

        foreach (var block in solidBlocks)
        {
            WriteSolidBlock(bw, block);
        }
    }

    private void WriteSolidBlock(BinaryWriter bw, SolidBlock block)
    {
        bw.Write((uint)block.ChunkIds.LongCount());

        foreach (var chunkId in block.ChunkIds)
        {
            bw.Write(chunkId);
        }
    }

    private void AddToSolidBlock(FileInfo file, byte[] data)
    {
        // Create a new solid block if needed
        if (currentSolidBlock is null || currentSolidBlock.Data.Length + data.Length > options.SolidBlockSize)
        {
            if (currentSolidBlock is not null)
            {
                FinalizeSolidBlock(currentSolidBlock);
            }

            currentSolidBlock = new(nextSolidBlockId++);
            solidBlocks.Add(currentSolidBlock);
        }

        // Record the file's location in the solid block
        file.SolidBlockId = currentSolidBlock.Id;
        file.SolidBlockOffset = (uint)currentSolidBlock.Data.Length;

        // Add the file data to the solid block
        currentSolidBlock.Data.Write(data);
    }

    private void FinalizeSolidBlock(SolidBlock block)
    {
        // Chunk the solid block data
        var blockData = block.Data.ToArray();
        var cdc = new FastCdc(blockData, options.FastCdcMinSize, options.FastCdcAverageSize, options.FastCdcMaxSize);

        using var ms = new MemoryStream(blockData);

        foreach (var chunk in cdc.GetChunks())
        {
            var chunkInfo = GetOrCreateChunk(chunk, ms);
            block.ChunkIds.Add(chunkInfo.Id);
        }
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

    private FileInfo CreateFile(string path, uint length)
    {
        var directory = GetOrCreateDirectory(FastCdcFsHelper.GetDirectoryName(path)!);
        var name = Path.GetFileName(path);

        if (files.Any(f => f.Name == name && f.DirectoryId == directory.Id))
            throw new FastCdcFsFileAlreadyExistsException(path);

        var file = new FileInfo(nextFileId++, directory.Id, name, length);
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

    private static Modes GetModes(FastCdcFsOptions options)
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
