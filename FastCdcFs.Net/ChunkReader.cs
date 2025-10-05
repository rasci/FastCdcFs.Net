using System.IO.Hashing;
using ZstdSharp;

namespace FastCdcFs.Net;

internal class ChunkReader(Stream s, bool compressed, bool hashed, byte[]? compressionDict, int dataOffset)
{
    private readonly HashSet<uint> verifiedChunks = [];

    public void ReadChunk(uint chunkIndex, ChunkInfo chunkInfo, byte[] buffer, int offset)
    {
        s.Position = dataOffset + chunkInfo.Offset;

        if (compressed)
        {
            using var decompressor = new Decompressor();
            decompressor.LoadDictionary(compressionDict);

            using var ds = new DecompressionStream(s, decompressor);
            FastCdcFsHelper.Read(ds, buffer, offset, (int)chunkInfo.Length);
        }
        else
        {
            FastCdcFsHelper.Read(s, buffer, offset, (int)chunkInfo.Length);
        }

        if (hashed && !verifiedChunks.Contains(chunkIndex))
        {
            AssertChunkHash(buffer, offset, (int)chunkInfo.Length, chunkInfo.Hash);
            verifiedChunks.Add(chunkIndex);
        }
    }

    private static void AssertChunkHash(byte[] buffer, int offset, int count, ulong expectedHash)
    {
        using var ms = new MemoryStream(buffer, offset, count);
        var hasher = new XxHash64();
        hasher.Append(ms);

        var hash = hasher.GetCurrentHashAsUInt64();
        if (hash != expectedHash)
            throw new CorruptedDataException();
    }
}
