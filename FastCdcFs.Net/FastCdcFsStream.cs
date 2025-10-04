using System.Diagnostics.CodeAnalysis;

namespace FastCdcFs.Net;

public class FastCdcFsStream : Stream
{
    private readonly ChunkReader reader;
    private readonly ChunkInfo[] chunkInfos;
    private readonly uint[] chunkIds;

    private int currentChunkIndex;
    private uint currentChunkId;
    private int chunkBytesLeft;
    private long position;
    private ChunkInfo? currentChunkInfo;
    private byte[]? currentChunk;

    internal FastCdcFsStream(ChunkReader reader, ChunkInfo[] chunkInfos, uint[] chunkIds)
    {
        this.reader = reader;
        this.chunkInfos = chunkInfos;
        this.chunkIds = chunkIds;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length { get; }

    public override long Position { get => position; set => throw new NotSupportedException(); }

    public override void Flush() => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (currentChunkIndex is 0 && currentChunkInfo is null)
        {
            NextChunk();
        }

        if (currentChunkInfo is null)
            return 0;

        var totalRead = 0;

        while (count > 0 && currentChunkInfo is not null)
        {
            if (currentChunk is not null || (int)currentChunkInfo!.Length > count)
            {
                // if not hashed, we could optimize this by reading directly to the user buffer

                if (currentChunk is null)
                {
                    ReadNextChunk();
                }

                var read = count > chunkBytesLeft ? chunkBytesLeft : count;
                Buffer.BlockCopy(currentChunk, (int)currentChunkInfo!.Length - chunkBytesLeft, buffer, offset, read);

                chunkBytesLeft -= read;
                offset += read;
                count -= read;
                totalRead += read;
                position += read;

                if (read is 0 || chunkBytesLeft is 0)
                {
                    NextChunk();
                }
            }

            else
            {
                // we can directly read the chunk in the user buffer
                reader.ReadChunk(currentChunkId, currentChunkInfo, buffer, offset);
                offset += (int)currentChunkInfo!.Length;
                count -= (int)currentChunkInfo!.Length;
                totalRead += (int)currentChunkInfo!.Length;
                position += (int)currentChunkInfo!.Length;
                NextChunk();
            }
        }

        return totalRead;
    }

    [MemberNotNull(nameof(currentChunk))]
    private void ReadNextChunk()
    {
        currentChunk = new byte[currentChunkInfo!.Length];
        reader.ReadChunk(currentChunkId, currentChunkInfo, currentChunk, 0);
        chunkBytesLeft = currentChunk.Length;
    }

    private void NextChunk()
    {
        currentChunk = null;

        if (currentChunkIndex >= chunkIds.Length)
        {
            currentChunkInfo = null;
            return;
        }

        currentChunkId = chunkIds[currentChunkIndex];
        currentChunkInfo = chunkInfos[currentChunkId];
        currentChunkIndex++;
    }
}
