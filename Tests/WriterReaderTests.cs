using FastCdcFs.Net.Reader;
using FastCdcFs.Net.Writer;

namespace Tests;

public class WriterReaderTests
{
    private static readonly Random Rand = new((int)DateTime.UtcNow.Ticks);

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

            directories.AddRange(entries.Where(e => e.IsDirectory).Select(e => Helper.PathCombine(next, e.Name)));

            foreach (var file in entries.Where(e => e.IsFile))
            {
                Assert.True(left.Remove(Helper.PathCombine(next, file.Name)));
            }
        }

        Assert.Empty(left);
    }

    private static void AddRandFiles(FastCdcFsWriter writer, params string[] paths)
    {
        foreach (var file in paths)
        {
            writer.AddFile(GenerateRepresentativeJsonData(1024 * 10), file);
        }
    }

    public static byte[] GenerateRepresentativeJsonData(int dataSize)
    {
        // Simulate a JSON object with repeating keys
        var sb = new System.Text.StringBuilder();
        sb.Append("{");
        sb.Append("\"id\":" + Guid.NewGuid() + ",");
        sb.Append("\"timestamp\":\"" + DateTime.UtcNow.ToString("O") + "\",");
        sb.Append("\"status\":\"" + GetRandomStatus() + "\",");
        sb.Append("\"message\":\"" + GenerateRandomString(dataSize - 100) + "\""); // Keep overall size in check
        sb.Append("}");

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string GetRandomStatus()
    {
        string[] statuses = { "pending", "processing", "completed", "failed", "retrying" };
        return statuses[Rand.Next(statuses.Length)];
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Rand.Next(s.Length)]).ToArray());
    }

    private static byte[] CreateData(int size)
    {
        var data = new byte[size];
        Rand.NextBytes(data);
        return data;
    }
}