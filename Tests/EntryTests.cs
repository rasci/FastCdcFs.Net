namespace Tests;

public class EntryTests : TestBase
{

    [Fact]
    public void OpenFile()
    {
        using var reader = CreateDefaultReader();

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

    [Fact]
    public void ReadAllBytes()
    {
        using var reader = CreateDefaultReader();

        foreach (var path in DefaultFiles)
        {
            var actual = reader.Get(path).ReadAllBytes();
            var expected = randFileData[path];
            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected, actual);
        }
    }
}
