namespace FastCdcFs.Net.Reader;

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
}
