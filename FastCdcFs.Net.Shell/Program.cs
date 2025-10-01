using FastCdcFs.Net.Shell;
using CommandLine;

Parser.Default
    .ParseArguments<BuildArgs, ListArgs, ExtractArgs>(args)
    .WithParsed<BuildArgs>(Handler.HandleBuild)
    .WithParsed<ListArgs>(Handler.HandleList)
    .WithParsed<ExtractArgs>(Handler.HandleExtract);