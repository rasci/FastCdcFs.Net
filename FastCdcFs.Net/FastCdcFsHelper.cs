namespace FastCdcFs.Net;

public class FastCdcFsHelper
{

    public static string Normalize(string? path)
    {
        path = path ?? "";

        if (path.StartsWith('/'))
        {
            path = path[1..];
        }

        return path;
    }

    public static string PathCombine(string a, string b)
        => a is "" ? b : $"{a}/{b}";

    public static string? GetDirectoryName(string path)
    {
        var directory = Path.GetDirectoryName(path);
        return directory is null
            ? null
            : directory.Replace('\\', '/');
    }

    public static int Read(Stream s, byte[] buffer, int offset, int count)
    {
        var total = 0;

        while (count > 0)
        {
            var read = s.Read(buffer, offset, count);
            offset += read;
            total += read;
            count -= read;
        }

        return total;
    }
}
