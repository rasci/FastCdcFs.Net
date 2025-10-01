namespace Tests;

public class FastCdcFsReaderGet : TestBase
{

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/")]
    public void NullOrEmptyReturnsRoot(string? path)
    {
        using var reader = CreateDefaultReader();

        var e = reader.Get(path);

        Assert.True(e.IsDirectory);
        Assert.True(e.FullName == "");
        Assert.True(e.Name == "");
    }
}
