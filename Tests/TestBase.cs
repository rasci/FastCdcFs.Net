using FastCdcFs.Net;

namespace Tests;

public abstract class TestBase
{
    private static readonly Random Rand = new((int)DateTime.UtcNow.Ticks);

    protected readonly Dictionary<string, byte[]> randFileData = [];

    protected static string[] DefaultFiles = [
        "fileA",
        "fileB",
        "dirA/fileC",
        "dirA/fileD",
        "dirB/fileE",
        "dirB/fileF"];

    protected FastCdcFsReader CreateDefaultReader()
        => CreateReaderWith(FastCdcFsOptions.Default, DefaultFiles);

    protected void CreateDefaultFile(Stream stream)
        => CreateFile(FastCdcFsOptions.Default, stream, CreateRandData, DefaultFiles);

    protected FastCdcFsReader CreateReaderWith(FastCdcFsOptions options, params string[] files)
        => CreateReaderWith(options, CreateRandData, files);

    protected FastCdcFsReader CreateReaderWith(FastCdcFsOptions options, Func<string, byte[]> dataHandler, params string[] files)
    {
        var ms = new MemoryStream();
        CreateFile(options, ms, dataHandler, files);
        return new(ms);
    }

    protected void CreateFile(FastCdcFsOptions options, Stream stream, Func<string, byte[]> dataHandler, params string[] files)
    {
        var writer = new FastCdcFsWriter(options);

        for (var i = 0; i < files.Length; i++)
        {
            writer.AddFile(dataHandler(files[i]), files[i]);
        }

        writer.Build(stream);
        stream.Position = 0;
    }

    protected void AssertFileEntry(Entry? n, byte[] expectedData)
    {
        Assert.NotNull(n);
        Assert.True(n.IsFile);
        Assert.NotNull(n.Name);
        Assert.Equal(n.Name, n.FullName.Split('/').Last());
        Assert.Equal((uint)expectedData.Length, n.Length);

        using var s = n.Open();
        Assert.Equal((uint)expectedData.Length, s.Length);

        var buffer = new Span<byte>(new byte[expectedData.Length + 1]); // need a buffer with length > 0
        Assert.Equal(
            expectedData.Length,
            s.Read(buffer));

        Assert.Equal(expectedData, buffer.Slice(0, expectedData.Length).ToArray());
        Assert.Equal(expectedData, n.ReadAllBytes());
    }

    private byte[] CreateRandData(string file)
    {
        var data = GenerateRepresentativeJsonData(1024 * 1000);
        randFileData[file] = data;
        return data;
    }

    private static byte[] GenerateRepresentativeJsonData(int dataSize)
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
}
