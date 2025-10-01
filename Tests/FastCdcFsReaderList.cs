using FastCdcFs.Net.Reader;

namespace Tests;

public class FastCdcFsReaderList : TestBase
{
    
    [Fact]
    public void ListAllRecursive()
    {
        using var reader = CreateDefaultReader();
        AsserFiles(reader, DefaultFiles);
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