namespace FastCdcFs.Net.Shell;

internal class Handler
{

    public static void HandleBuild(BuildArgs a)
    {
        if (a.IsFile)
        {
            Cache.AddFile(a.File, a.TargetPath);
        }

        if (a.IsDirectory)
        {
            Cache.AddDirectory(a.Directory, a.TargetPath, a.Recursive);
        }

        if (a.IsOutput)
        {
            var cache = Cache.ReadAndDelete();
            var writer = new FastCdcFsWriter(CreateOptions(a));

            foreach (var file in cache.Files)
            {
                var targetPath = file.TargetPath ?? Path.GetFileName(file.SourcePath);
                Console.WriteLine($"Adding file {file.SourcePath} as {targetPath}");
                writer.AddFile(file.SourcePath, targetPath);
            }

            foreach (var dir in cache.Directories)
            {
                if (dir.Recursive)
                {
                    foreach (var file in Directory.EnumerateFiles(dir.SourcePath, "*", SearchOption.AllDirectories))
                    {
                        var relativePath = Path.GetRelativePath(dir.SourcePath, file);
                        var targetPath = dir.TargetPath is not null
                            ? Path.Combine(dir.TargetPath, relativePath)
                            : relativePath;
                        Console.WriteLine($"Adding file {file} as {targetPath}");
                        writer.AddFile(file, targetPath);
                    }
                }
                else
                {
                    foreach (var file in Directory.EnumerateFiles(dir.SourcePath, "*", SearchOption.TopDirectoryOnly))
                    {
                        var targetPath = dir.TargetPath is not null
                            ? Path.Combine(dir.TargetPath, Path.GetFileName(file))
                            : Path.GetFileName(file);
                        Console.WriteLine($"Adding file {file} as {targetPath}");
                        writer.AddFile(file, targetPath);
                    }
                }
            }

            Console.Write($"Building file system to {a.Output} ");

            using var ms = new MemoryStream();
            writer.Build(a.Output);

            Console.WriteLine($"(length {writer.Length}, compression rate {writer.CompressionRatePercentage}%)");
        }
    }

    public static void HandleList(ListArgs a)
    {
        using var reader = new FastCdcFsReader(a.Source!);
        var entries = reader.List(a.Directory ?? "/");
        var grid = new ConsoleGrid(3);

        foreach (var e in entries)
        {
            if (e.IsFile)
            {
                grid.Add("", e.Name, e.Length.ToString());
            }
            else
            {
                grid.Add("<DIR>", e.Name, "");
            }
        }

        Console.WriteLine(grid);
    }

    public static void HandleExtract(ExtractArgs a)
    {
        using var reader = new FastCdcFsReader(a.Source!);

        if (a.IsFile)
        {
            using var fs = File.OpenWrite(a.TargetPath ?? Path.GetFileName(a.File));
            using var stream = reader.Get(a.File).Open();
            stream.CopyTo(fs);
        }   

        if (a.IsDirectory || !a.IsFile)
        {
            ExtractDirectory(reader, a.Directory ?? "/", a.TargetPath ?? "", a.Recursive);
        }
    }

    private static void ExtractDirectory(FastCdcFsReader reader, string sourceDir, string targetDir, bool recursive)
    {
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        foreach (var entry in reader.List(sourceDir))
        {
            var sourcePath = Path.Combine(sourceDir, entry.Name);
            var targetPath = Path.Combine(targetDir, entry.Name);

            if (entry.IsFile)
            {
                Console.WriteLine($"Extracting file {sourcePath} to {targetPath}");
                using var fs = File.OpenWrite(targetPath);
                using var stream = entry.Open();
                stream.CopyTo(fs);
            }
            else if (recursive)
            {
                ExtractDirectory(reader, sourcePath, targetPath, recursive);
            }
        }
    }

    private static FastCdcFsOptions CreateOptions(BuildArgs a)
        => new(
            a.FastCdcMin ?? FastCdcFsOptions.Default.FastCdcMinSize,
            a.FastCdcAvg ?? FastCdcFsOptions.Default.FastCdcAverageSize,
            a.FastCdcMax ?? FastCdcFsOptions.Default.FastCdcMaxSize,
            a.NoZstd,
            a.NoHash,
            a.CompressionLevel);
}
