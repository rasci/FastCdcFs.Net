using FastCdcFs.Net;

namespace Tests;

public class EntryTests : TestBase
{

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void OpenFile(bool noZstd, bool noHash)
    {
        var options = FastCdcFsOptions.Default
            .WithNoZstd(noZstd)
            .WithNoHash(noHash);

        using var reader = CreateReaderWith(options, DefaultFiles);

        foreach (var path in DefaultFiles)
        {
            using var stream = reader.Get(path).Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var expected = randFileData[path];
            var actual = ms.ToArray();
            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ReadAllBytes(bool noZstd, bool noHash)
    {
        var options = FastCdcFsOptions.Default
            .WithNoZstd(noZstd)
            .WithNoHash(noHash);

        using var reader = CreateReaderWith(options, DefaultFiles);

        foreach (var path in DefaultFiles)
        {
            var actual = reader.Get(path).ReadAllBytes();
            var expected = randFileData[path];
            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ZeroByteFile(bool noZstd, bool noHash)
    {
        var options = FastCdcFsOptions.Default
            .WithNoZstd(noZstd)
            .WithNoHash(noHash);

        var writer = new FastCdcFsWriter(options);
        var emptyData = Array.Empty<byte>();
        writer.AddFile(emptyData, "empty.txt");

        using var ms = new MemoryStream();
        writer.Build(ms);
        ms.Position = 0;

        using var reader = new FastCdcFsReader(ms);
        var entry = reader.Get("empty.txt");
        
        Assert.True(entry.IsFile);
        Assert.Equal(0u, entry.Length);
        
        var actual = entry.ReadAllBytes();
        Assert.Empty(actual);
    }
}
