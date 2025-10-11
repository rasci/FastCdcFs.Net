using FastCdcFs.Net.Shell;
using CommandLine;

var parser = new Parser(o =>
{
    o.HelpWriter = Console.Error;
    o.CaseInsensitiveEnumValues = true;
});

await parser
    .ParseArguments<BuildArgs, ListArgs, ExtractArgs, DumpArgs, TrainArgs>(args)
    .WithParsedAsync<BuildArgs>(Handler.HandleBuildAsync)
    .ContinueWithParsedAsync<ListArgs>(Handler.HandleListAsync)
    .ContinueWithParsedAsync<ExtractArgs>(Handler.HandleExtractAsync)
    .ContinueWithParsedAsync<DumpArgs>(DumpHandler.HandleAsync)
    .ContinueWithParsedAsync<TrainArgs>(TrainHandler.HandleAsync);

public static class Extensions
{
    public static async Task<ParserResult<object>> ContinueWithParsedAsync<T>(this Task<ParserResult<object>> parserTask, Func<T, Task> handler)
    {
        var parser = await parserTask;
        await parser.WithParsedAsync(handler);
        return parser;
    }
}
