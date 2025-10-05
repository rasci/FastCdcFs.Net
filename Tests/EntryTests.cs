using FastCdcFs.Net;

namespace Tests;

public class EntryTests : TestBase
{

    [Theory]
    [InlineData(false, false)]
    //[InlineData(false, true)]
    //[InlineData(true, false)]
    //[InlineData(true, true)]
    public void OpenFile(bool noZstd, bool noHash)
    {
        var options = new Options(
            Options.Default.FastCdcMinSize,
            Options.Default.FastCdcAverageSize,
            Options.Default.FastCdcMaxSize,
            noZstd,
            noHash);

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
        var options = new Options(
            Options.Default.FastCdcMinSize,
            Options.Default.FastCdcAverageSize,
            Options.Default.FastCdcMaxSize,
            noZstd,
            noHash);

        using var reader = CreateReaderWith(options, DefaultFiles);

        foreach (var path in DefaultFiles)
        {
            var actual = reader.Get(path).ReadAllBytes();
            var expected = randFileData[path];
            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected, actual);
        }
    }
}
