using System.Text;

namespace FastCdcFs.Net;

public record FastCdcFsOptions(uint FastCdcMinSize, uint FastCdcAverageSize, uint FastCdcMaxSize, bool NoZstd, bool NoHash, int CompressionLevel)
{
    public const int DefaultCompressionLevel = 22;

    public static FastCdcFsOptions Default => new(1024 * 32, 1024 * 64, 1024 * 256, false, false, DefaultCompressionLevel);

    public FastCdcFsOptions WithNoZstd(bool noZstd = true)
        => this with { NoZstd = noZstd };

    public FastCdcFsOptions WithNoHash(bool noHash = true)
        => this with { NoHash = noHash };

    public FastCdcFsOptions WithCompressionLevel(int level = 22)
        => this with { CompressionLevel = level };

    public FastCdcFsOptions WithChunkSizes(uint minSize, uint averageSize, uint maxSize)
    {
        if (minSize is 0 || averageSize is 0 || maxSize is 0)
            throw new ArgumentException("Chunk sizes must be greater than zero");

        if (minSize > averageSize)
            throw new ArgumentException("Min size must be less than or equal to average size");

        if (averageSize > maxSize)
            throw new ArgumentException("Average size must be less than or equal to max size");

        return this with { FastCdcMinSize = minSize, FastCdcAverageSize = averageSize, FastCdcMaxSize = maxSize };
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        var properties = typeof(FastCdcFsOptions).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var p in properties)
        {
            if (sb.Length > 0)
            {
                sb.Append(", ");
            }

            sb.Append(p.Name);
            sb.Append(": ");
            sb.Append(p.GetValue(this));
        }

        return sb.ToString();
    }
}
