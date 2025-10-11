using CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace FastCdcFs.Net.Shell;

public abstract class BaseArgs
{
    [Option('f', "file", Required = false, HelpText = "Adds or extract given file to or from the file system")]
    public string? File { get; set; }

    [Option('d', "directory", Required = false, HelpText = "Adds or extract given directory to or from the file system")]
    public string? Directory { get; set; }

    [Option('t', "target", Required = false, HelpText = "Target path of the file or directory within the file system or for extraction")]
    public string? TargetPath { get; set; }

    [Option('r', "recursive", Required = false, HelpText = "Adds or extracts the directory recursively to or from the file system")]
    public bool Recursive { get; set; }

    [MemberNotNullWhen(true, nameof(File))]
    public bool IsFile => !string.IsNullOrEmpty(File);

    [MemberNotNullWhen(true, nameof(Directory))]
    public bool IsDirectory => !string.IsNullOrEmpty(Directory);
}

[Verb("tune")]
public class TuneArgs : BaseArgs
{
    [Option("min", Required = false, Default = FastCdc.MinimumMin, HelpText = "FastCdc min size to begin with")]
    public uint Min { get; set; }

    [Option("max", Required = false, Default = FastCdc.MaximumMax, HelpText = "FastCdc max size to end with")]
    public uint Max { get; set; }

    [Option("concurrency", Required = false, Default = 0, HelpText = "The number of concurrent tasks (0 is Environment.ProcessorCount)")]
    public int Concurrency { get; set; }

    internal void Validate()
    {
        if (string.IsNullOrEmpty(Directory) || !System.IO.Directory.Exists(Directory))
            throw new DirectoryNotFoundException(Directory);

        if (Min < FastCdc.MinimumMin)
            throw new Exception($"Min < {FastCdc.MinimumMin}");

        if (Max > FastCdc.MaximumMax)
            throw new Exception($"Max < {FastCdc.MaximumMax}");

        if (Concurrency < 0)
            throw new Exception("Concurrency cannot be negative");
    }
}

[Verb("build")]
public class BuildArgs : BaseArgs
{
    [Option('o', "output", Required = false, HelpText = "Build the file system out of the added files and directories")]
    public string? Output { get; set; }

    [MemberNotNullWhen(true, nameof(Output))]
    public bool IsOutput => !string.IsNullOrEmpty(Output);

    [Option("no-zstd", Required = false, HelpText = "Do not compress chunks with zstd")]
    public bool NoZstd { get; set; }

    [Option("no-hash", Required = false, HelpText = "Do not hash meta data and chunks for read-time verification")]
    public bool NoHash { get; set; }

    [Option("compression-level", Required = false, Default = FastCdcFsOptions.DefaultCompressionLevel, HelpText = "Zstd compression level")]
    public int CompressionLevel { get; set; }

    [Option("fastcdc-min", Required = false, Default = FastCdcFsOptions.DefaultFastCdcMinSize, HelpText = "Minimum chunk size for FastCDC algorithm")]
    public uint FastCdcMin { get; set; }

    [Option("fastcdc-avg", Required = false, Default = FastCdcFsOptions.DefaultFastCdcAverageSize, HelpText = "Average chunk size for FastCDC algorithm")]
    public uint FastCdcAvg { get; set; }

    [Option("fastcdc-max", Required = false, Default = FastCdcFsOptions.DefaultFastCdcMaxSize, HelpText = "Maximum chunk size for FastCDC algorithm")]
    public uint FastCdcMax { get; set; }

    [Option("small-file-threshold", Required = false, Default = FastCdcFsOptions.DefaultSmallFileThreshold, HelpText = "Threshold for small file handling")]
    public uint SmallFileThreshold { get; set; }

    [Option("solid-block-size", Required = false, Default = FastCdcFsOptions.DefaultSolidBlockSize, HelpText = "Size of solid blocks for small files")]
    public uint SolidBlockSize { get; set; }
}

[Verb("list")]
public class ListArgs
{
    [Option('d', "directory", Required = false, HelpText = "List contents od this directory")]
    public string? Directory { get; set; }

    [Value(0, Required = true, HelpText = "Path to the cdcfs file")]
    public string? Source { get; set; }
}

[Verb("extract")]
public class ExtractArgs : BaseArgs
{

    [Value(0, Required = true, HelpText = "Path to the cdcfs file")]
    public string? Source { get; set; }
}