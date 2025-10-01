namespace Tests;

public class FastCdcFsReaderGet : TestBase
{

    [Theory]
    [InlineData(null, true, "", "")]
    [InlineData("", true, "", "")]
    [InlineData("/", true, "", "")]
    [InlineData("dirA", true, "dirA", "dirA")]
    [InlineData("dirB", true, "dirB", "dirB")]
    [InlineData("fileA", false, "fileA", "fileA")]
    [InlineData("fileB", false, "fileB", "fileB")]
    [InlineData("dirA/fileC", false, "dirA/fileC", "fileC")]
    [InlineData("dirA/fileD", false, "dirA/fileD", "fileD")]
    [InlineData("dirB/fileE", false, "dirB/fileE", "fileE")]
    [InlineData("dirB/fileF", false, "dirB/fileF", "fileF")]
    public void Exists(string? path, bool isDirectory, string fullName, string name)
    {
        using var reader = CreateDefaultReader();

        var e = reader.Get(path);

        Assert.Equal(isDirectory, e.IsDirectory);
        Assert.Equal(fullName, e.FullName);
        Assert.Equal(name, e.Name);
        Assert.True(isDirectory ? e.Length is 0 : e.Length > 0);
    }

    [Theory]
    [InlineData("fileX")]
    [InlineData("dirA/fileY")]
    [InlineData("dirW/fileZ")]
    public void NotExists(string path)
    {
        using var reader = CreateDefaultReader();
        Assert.Throws<FileNotFoundException>(() => reader.Get(path));
    }
}
