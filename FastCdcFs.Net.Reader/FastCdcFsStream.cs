using ZstdSharp;

namespace FastCdcFs.Net.Reader;

public class FastCdcFsStream : Stream
{
    private readonly Stream s;
    private readonly uint dataOffset;
    private readonly Range[] chunks;
    private readonly uint[] chunkIds;
    private readonly bool compressed;
    private Decompressor? decompressor;

    private uint currentChunkIndex;
    private int chunkBytesLeft;
    private long position;
    private Range currentRange;
    private Stream? currentStream;

    internal FastCdcFsStream(Stream s, uint dataOffset, byte[]? compressionDict, Range[] chunks, uint[] chunkIds, uint length, bool compressed)
    {
        this.s = s;
        this.dataOffset = dataOffset;
        this.chunks = chunks;
        this.chunkIds = chunkIds;
        this.compressed = compressed;

        Length = length;

        if (compressed)
        {
            decompressor = new Decompressor();
            decompressor.LoadDictionary(compressionDict);
        }
        else
        {
            currentStream = s;
        }
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length { get; }

    public override long Position { get => position; set => throw new NotSupportedException(); }

    public override void Flush() => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (currentChunkIndex is 0 && currentStream is null)
        {
            OpenNextChunk();
        }

        if (currentStream is null)
            return 0;

        var totalRead = 0;

        while (count > 0 && currentStream is not null)
        {
            var read = currentStream.Read(buffer, offset, count > chunkBytesLeft ? chunkBytesLeft : count);
            chunkBytesLeft -= read;
            offset += read;
            count -= read;
            totalRead += read;
            position += read;

            if (read is 0 || chunkBytesLeft is 0)
            {
                OpenNextChunk();
            }
        }

        return totalRead;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

    public override void SetLength(long value) => throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && compressed)
        {
            decompressor!.Dispose();
            currentStream?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OpenNextChunk()
    {
        if (compressed)
        {
            currentStream?.Dispose();
        }

        if (currentChunkIndex >= chunkIds.Length)
        {
            currentStream = null;
            return;
        }

        currentRange = chunks[chunkIds[currentChunkIndex++]];
        chunkBytesLeft = (int)currentRange.Length;
        s.Position = dataOffset + currentRange.Offset;

        if (compressed)
        {
            currentStream = new DecompressionStream(s, decompressor, leaveOpen: true, preserveDecompressor: true);
        }
    }
}
