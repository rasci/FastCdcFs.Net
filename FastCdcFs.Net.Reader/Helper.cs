namespace FastCdcFs.Net.Reader;

public class Helper
{

    public static string PathCombine(string a, string b)
        => a is "" ? b : $"{a}/{b}";

    public static string GetDirectoryName(string path)
        => Path.GetDirectoryName(path)!.Replace('\\', '/');
}
