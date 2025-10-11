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

[Verb("train")]
public class TrainArgs : BaseArgs
{
    public enum TrainModes
    {
        FastCdc,
        CompressionDict
    }

    [Option("min", Required = false, Default = FastCdc.MinimumMin, HelpText = "Min size to begin with")]
    public uint Min { get; set; }

    [Option("max", Required = false, Default = FastCdc.MaximumMax, HelpText = "Max size to end with")]
    public uint Max { get; set; }

    [Option("concurrency", Required = false, HelpText = "The number of concurrent tasks (0 is Environment.ProcessorCount)")]
    public int Concurrency
    {
        get => field > 0 ? field : Environment.ProcessorCount;
        set
        {
            field = value < 0
                ? throw new ArgumentOutOfRangeException("Concurrency cannot be negative")
                : value;
        }
    }

    [Option("fastcdc-min", Required = false, Default = FastCdcFsOptions.DefaultFastCdcMinSize, HelpText = "FastCdc Min (only when mode is compression-dict)")]
    public uint FastCdcMin { get; set; }

    [Option("fastcdc-avg", Required = false, Default = FastCdcFsOptions.DefaultFastCdcAverageSize, HelpText = "FastCdc Average (only when mode is compression-dict)")]
    public uint FastCdcAvg { get; set; }

    [Option("fastcdc-max", Required = false, Default = FastCdcFsOptions.DefaultFastCdcMaxSize, HelpText = "FastCdc Max (only when mode is compression-dict)")]
    public uint FastCdcMax { get; set; }

    [Value(0, Required = true, HelpText = "Train mode: fastcdc parameter or compressiondict")]
    public TrainModes Mode { get; set; }

    public FastCdcFsOptions GetCompressionDictOptions() => FastCdcFsOptions.Default.WithChunkSizes(FastCdcMin, FastCdcAvg, FastCdcMax);

    internal void Validate()
    {
        if (string.IsNullOrEmpty(Directory) || !System.IO.Directory.Exists(Directory))
            throw new DirectoryNotFoundException(Directory);

        if (Min < FastCdc.MinimumMin)
            throw new Exception($"Min < {FastCdc.MinimumMin}");

        if (Max > FastCdc.MaximumMax)
            throw new Exception($"Max < {FastCdc.MaximumMax}");

        if (Mode is TrainModes.CompressionDict)
        {
            GetCompressionDictOptions();
        }
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

    [Option("dict-size", Required = false, HelpText = "Zstd compression dict size (if 0, 100 * average of sample data is used")]
    public uint CompressionDictSize { get; set; }

    [Option("fastcdc-min", Required = false, Default = FastCdcFsOptions.DefaultFastCdcMinSize, HelpText = "Minimum chunk size for FastCDC algorithm")]
    public uint FastCdcMin { get; set; }

    [Option("fastcdc-avg", Required = false, Default = FastCdcFsOptions.DefaultFastCdcAverageSize, HelpText = "Average chunk size for FastCDC algorithm")]
    public uint FastCdcAvg { get; set; }

    [Option("fastcdc-max", Required = false, Default = FastCdcFsOptions.DefaultFastCdcMaxSize, HelpText = "Maximum chunk size for FastCDC algorithm")]
    public uint FastCdcMax { get; set; }
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