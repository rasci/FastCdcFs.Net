using FastCdcFs.Net;

namespace Tests;

public class FastCdcFsWriterAddFile : TestBase
{

    [Theory]
    [InlineData("root")]
    [InlineData("non/root")]
    public void SameFileTwiceThrowsException(string targetPath)
    {
        var writer = new FastCdcFsWriter(FastCdcFsOptions.Default);
        writer.AddFile([], targetPath);
        Assert.Throws<FastCdcFsFileAlreadyExistsException>(() => writer.AddFile([], targetPath));
    }

    [Fact]
    public void WriteZeroByteFile()
    {
        using var reader = CreateReaderWith(FastCdcFsOptions.Default, _ => [], "empty.txt");
        AssertFileEntry(reader.Get("empty.txt"), []);
    }
}
