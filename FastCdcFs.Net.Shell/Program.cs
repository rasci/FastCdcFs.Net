using FastCdcFs.Net.Shell;
using CommandLine;

await Parser.Default
    .ParseArguments<BuildArgs, ListArgs, ExtractArgs, DumpArgs, TuneArgs>(args)
    .WithParsedAsync<BuildArgs>(Handler.HandleBuildAsync)
    .ContinueWithParsedAsync<ListArgs>(Handler.HandleListAsync)
    .ContinueWithParsedAsync<ExtractArgs>(Handler.HandleExtractAsync)
    .ContinueWithParsedAsync<DumpArgs>(DumpHandler.HandleAsync)
    .ContinueWithParsedAsync<TuneArgs>(TuneHandler.HandleAsync);

public static class Extensions
{
    public static async Task<ParserResult<object>> ContinueWithParsedAsync<T>(this Task<ParserResult<object>> parserTask, Func<T, Task> handler)
    {
        var parser = await parserTask;
        await parser.WithParsedAsync(handler);
        return parser;
    }
}
