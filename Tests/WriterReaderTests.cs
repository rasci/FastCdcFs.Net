using FastCdcFs.Net.Reader;
using FastCdcFs.Net.Writer;

namespace Tests;

public class WriterReaderTests : TestBase
{
    

    [Fact]
    public void WriteRead()
    {
        var files = new[]
        {
            "file0",
            "file1",
            "0/file0",
            "0/file1",
            "1/file0",
            "1/file1",
            "0/0/file0",
            "0/0/file1",
            "0/1/file0",
            "0/1/file1"
        };

        var writer = new FastCdcFsWriter(Options.Default);

        AddRandFiles(writer, files);

        using var ms = new MemoryStream();
        writer.Build(ms);

        ms.Position = 0;

        using var reader = new FastCdcFsReader(ms);
        AsserFiles(reader, files);
    }

    private static void AsserFiles(FastCdcFsReader reader, params string[] paths)
    {
        var left = paths.ToList();
        var directories = new List<string>() { "" };

        while (directories.Any())
        {
            var next = directories.First();
            directories.RemoveAt(0);

            var entries = reader.List(next);

            directories.AddRange(entries.Where(e => e.IsDirectory).Select(e => FastCdcFsHelper.PathCombine(next, e.Name)));

            foreach (var file in entries.Where(e => e.IsFile))
            {
                Assert.True(left.Remove(FastCdcFsHelper.PathCombine(next, file.Name)));
            }
        }

        Assert.Empty(left);
    }

    
}