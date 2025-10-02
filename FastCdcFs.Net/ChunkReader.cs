using System.IO.Hashing;
using ZstdSharp;

namespace FastCdcFs.Net;

internal class ChunkReader(Stream s, bool compressed, bool hashed, byte[]? compressionDict, ChunkInfo[] chunkInfos, int dataOffset)
{
    private readonly HashSet<int> verifiedChunks = [];

    public byte[] ReadChunk(int chunkIndex)
    {
        var chunkInfo = chunkInfos[chunkIndex];

        s.Position = dataOffset + chunkInfo.Offset;

        var data = new byte[chunkInfo.Length];
        var total = 0;
        var offset = 0;

        if (compressed)
        {
            using var decompressor = new Decompressor();
            decompressor.LoadDictionary(compressionDict);

            using var ds = new DecompressionStream(s, decompressor);

            while (total < chunkInfo.Length)
            {
                var read = ds.Read(data, offset, (int)chunkInfo.Length - total);
                offset += read;
                total += read;
            }
        }
        else
        {
            while (total < chunkInfo.Length)
            {
                var read = s.Read(data, offset, (int)chunkInfo.Length - total);
                offset += read;
                total += read;
            }
        }

        if (hashed && !verifiedChunks.Contains(chunkIndex))
        {
            using var ms = new MemoryStream(data);
            var hasher = new XxHash64();
            hasher.Append(ms);

            var hash = hasher.GetCurrentHashAsUInt64();
            if (hash != chunkInfo.Hash)
                throw new CorruptedDataException();

            verifiedChunks.Add(chunkIndex);
        }

        return data;
    }
}
