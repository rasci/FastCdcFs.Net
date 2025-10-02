namespace FastCdcFs.Net;

public class FastCdcFsStream : Stream
{
    private readonly ChunkReader reader;
    private readonly ChunkInfo[] chunkInfos;
    private readonly uint[] chunkIds;

    private int currentChunkIndex;
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

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (currentChunkIndex is 0 && currentChunk is null)
        {
            ReadNextChunk();
        }

        if (currentChunk is null)
            return 0;

        var totalRead = 0;

        while (count > 0 && currentChunk is not null)
        {
            var read = count > chunkBytesLeft ? chunkBytesLeft : count;
            Buffer.BlockCopy(currentChunk, (int)currentChunkInfo!.Length - chunkBytesLeft, buffer, offset, read);

            chunkBytesLeft -= read;
            offset += read;
            count -= read;
            totalRead += read;
            position += read;

            if (read is 0 || chunkBytesLeft is 0)
            {
                ReadNextChunk();
            }
        }

        return totalRead;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

    public override void SetLength(long value) => throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

    private void ReadNextChunk()
    {
        if (currentChunkIndex >= chunkIds.Length)
        {
            currentChunk = null;
            return;
        }

        var chunkId = chunkIds[currentChunkIndex];
        currentChunk = reader.ReadChunk((int)chunkId);
        currentChunkInfo = chunkInfos[chunkId];
        currentChunkIndex++;
        chunkBytesLeft = currentChunk.Length;
    }
}
