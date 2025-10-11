using Humanizer;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace FastCdcFs.Net.Shell;

internal class TrainHandler(TrainArgs a)
{
    private abstract record WorkItem
    {
        public uint Length { get; set; }

        public TimeSpan? Duration { get; set; }

        public abstract FastCdcFsWriter CreateWriter(TrainArgs a);

        public override string ToString()
            => $"{Length.Bytes().Humanize()} ({Duration.GetValueOrDefault().Humanize()})";
    }

    private record FastCdcWorkItem(uint Min, uint Avg, uint Max) : WorkItem()
    {
        public override FastCdcFsWriter CreateWriter(TrainArgs a)
            => new(FastCdcFsOptions.Default.WithChunkSizes(Min, Avg, Max));

        public override string ToString()
            => $"{Min} - {Avg} - {Max}{(Length is 0 ? "" : $" {base.ToString()}")}";
    }

    private record CompressionDictWorkItem(uint DictSize) : WorkItem()
    {

        public override FastCdcFsWriter CreateWriter(TrainArgs a)
            => new(a.GetCompressionDictOptions().WithCompressionDictSize(DictSize));

        public override string ToString()
            => $"{DictSize}{(Length is 0 ? "" : $" {base.ToString()}")}";
    }

    private static readonly object sync = new();
    private readonly ConcurrentQueue<WorkItem> workItems = [];
    private readonly Dictionary<string, byte[]> fileData = [];

    public static async Task HandleAsync(TrainArgs a)
    {
        a.Validate();

        var tune = new TrainHandler(a);
        await tune.HandleAsync();
    }

    private async Task HandleAsync()
    {
        CreateWorkItems();

        var sw = new Stopwatch();
        sw.Start();

        Console.Write("Loading files in memory ...");
        await LoadFilesAsync();

        sw.Stop();
        Console.WriteLine($" {sw.Elapsed.Humanize()}");

        Console.WriteLine($"Running ... ({a.Concurrency} tasks)");
        await Task.WhenAll(Enumerable.Range(0, a.Concurrency).Select(_ => Task.Run(Work)));
    }

    private void Work()
    {
        var sw = new Stopwatch();

        while (workItems.TryDequeue(out var item))
        {
            using var ms = new MemoryStream();
            var writer = item.CreateWriter(a);

            sw.Restart();

            foreach (var (path, data) in fileData)
            {
                writer.AddFile(data, path);
            }

            writer.Build(ms);

            item.Duration = sw.Elapsed;
            item.Length = (uint)ms.Length;

            PrintCurrentWorkItemRanking();
        }
    }

    private void PrintCurrentWorkItemRanking()
    {
        var finished = workItems.Where(w => w.Length > 0).OrderBy(w => w.Length).ToArray();
        ConsoleGrid grid;

        if (a.Mode is TrainArgs.TrainModes.FastCdc)
        {
            grid = new ConsoleGrid(6);
            grid.Add("Rank", "Min", "Avg", "Max", "Length", "Duration");

            var i = 0;
            foreach (var w in finished.Cast<FastCdcWorkItem>())
            {
                grid.Add(
                    i++,
                    w.Min,
                    w.Avg,
                    w.Max,
                    w.Length.Bytes(),
                    w.Duration.GetValueOrDefault().Humanize());
            }
        }
        else if (a.Mode is TrainArgs.TrainModes.CompressionDict)
        {
            grid = new ConsoleGrid(4);
            grid.Add("Rank", "Dict Size", "Length", "Duration");

            var i = 0;
            foreach (var w in finished.Cast<CompressionDictWorkItem>())
            {
                grid.Add(
                    i++,
                    w.DictSize,
                    w.Length.Bytes(),
                    w.Duration.GetValueOrDefault().Humanize());
            }
        }
        else
            throw new NotImplementedException(a.Mode.ToString());


        var str = grid.ToString();
        lock (sync)
        {
            Console.WriteLine(str);
        }
    }

    private void CreateWorkItems()
    {
        if (a.Mode is TrainArgs.TrainModes.FastCdc)
        {
            for (var min = a.Min; min < a.Max; min <<= 1)
            {
                for (var avg = (min < FastCdc.AverageMin ? FastCdc.AverageMin : min) << 1;
                    avg < (a.Max > FastCdc.AverageMax ? FastCdc.AverageMax : a.Max);
                    avg <<= 1)
                {
                    for (var max = avg << 1; max < a.Max; max <<= 1)
                    {
                        var item = new FastCdcWorkItem(min, avg, max);
                        Console.WriteLine($"[{workItems.Count}] {item}");
                        workItems.Enqueue(item);
                    }
                }
            }
        }
        else if (a.Mode is TrainArgs.TrainModes.CompressionDict)
        {
            for (var current = a.Min; current < Math.Min(a.Max, FastCdcFsOptions.CompressionDictMaxSize); current <<= 1)
            {
                var item = new CompressionDictWorkItem(current);
                Console.WriteLine($"[{workItems.Count}] {item}");
                workItems.Enqueue(item);
            }
        }
        else
        {
            throw new NotImplementedException(a.Mode.ToString());
        }
    }

    private async Task LoadFilesAsync()
    {
        foreach (var file in Directory.GetFiles(a.Directory!, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetRelativePath(a.Directory!, file);
            fileData.Add(name, await File.ReadAllBytesAsync(file));
        }
    }
}
