using CommandLine;

namespace FastCdcFs.Net.Shell;

[Verb("dump")]
public class DumpArgs
{

    [Value(0, Required = true, HelpText = "Path to the cdcfs file")]
    public string? Source { get; set; }

    internal void Validate()
    {
        if (string.IsNullOrEmpty(Source) || !File.Exists(Source))
            throw new FileNotFoundException(Source);
    }
}

internal class DumpHandler
{

    public static Task HandleAsync(DumpArgs a)
    {
        a.Validate();

        using var reader = new FastCdcFsReader(a.Source!);
        Console.WriteLine(FastCdcFsHelper.Dump(reader));
        return Task.CompletedTask;
    }

}
