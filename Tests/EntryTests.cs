using FastCdcFs.Net;

namespace Tests;

public class EntryTests : TestBase
{

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void FileRead(bool noZstd, bool noHash)
    {
        var options = FastCdcFsOptions.Default
            .WithNoZstd(noZstd)
            .WithNoHash(noHash);

        using var reader = CreateReaderWith(options, DefaultFiles);

        foreach (var path in DefaultFiles)
        {
            AssertFileEntry(reader.Get(path), randFileData[path]);
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

        using var reader = CreateReaderWith(options, _ => [], "empty.txt");
        AssertFileEntry(reader.Get("empty.txt"), []);
    }
}
