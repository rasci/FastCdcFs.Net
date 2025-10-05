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
        => CreateFile(FastCdcFsOptions.Default, stream, DefaultFiles);

    protected FastCdcFsReader CreateReaderWith(FastCdcFsOptions options, params string[] files)
    {
        var ms = new MemoryStream();
        CreateFile(options, ms, files);
        return new(ms);
    }

    protected void CreateFile(FastCdcFsOptions options, Stream stream, params string[] files)
    {
        var writer = new FastCdcFsWriter(options);
        AddRandFiles(writer, files);
        writer.Build(stream);
        stream.Position = 0;
    }

    protected void AddRandFiles(FastCdcFsWriter writer, params string[] paths)
    {
        foreach (var file in paths)
        {
            var data = GenerateRepresentativeJsonData(1024 * 1000);
            randFileData[file] = data;
            writer.AddFile(data, file);
        }
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
