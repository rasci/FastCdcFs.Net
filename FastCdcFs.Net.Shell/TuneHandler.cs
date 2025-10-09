using System.Collections.Concurrent;

namespace FastCdcFs.Net.Shell;

internal class TuneHandler(TuneArgs a)
{
    private record WorkItem(uint Min, uint Avg, uint Max)
    {
        public uint Length { get; set; }

        public override string ToString()
            => Length is 0
                ? $"{Min} - {Avg} - {Max}"
                : $"{Min} - {Avg} - {Max}: {Length}";
    }

    private readonly ConcurrentQueue<WorkItem> workItems = [];
    private readonly Dictionary<string, byte[]> fileData = [];

    public static async Task HandleAsync(TuneArgs a)
    {
        a.Validate();

        var tune = new TuneHandler(a);
        await tune.HandleAsync();
    }

    private async Task HandleAsync()
    {
        CreateWorkItems(a);

        Console.WriteLine("Loading files in memory");
        await LoadFilesAsync(a);

        Console.WriteLine("Running");
        await Task.WhenAll(Enumerable.Range(0, a.Concurrency).Select(_ => Task.Run(Work)));
    }

    private void Work()
    {
        while (workItems.TryDequeue(out var item))
        {
            var option = new FastCdcFsOptions(item.Min, item.Avg, item.Max, false, false, 22, 0, 0);
            var writer = new FastCdcFsWriter(option);

            foreach (var (path, data) in fileData)
            {
                writer.AddFile(data, path);
            }

            using var ms = new MemoryStream();
            writer.Build(ms);

            item.Length = (uint)ms.Length;

            Console.WriteLine(item);
        }
    }

    private void CreateWorkItems(TuneArgs a)
    {
        for (var min = a.Min; min < a.Max; min <<= 2)
        {
            for (var avg = (min < FastCdc.AverageMin ? FastCdc.AverageMin : min) << 1;
                avg < (a.Max > FastCdc.AverageMax ? FastCdc.AverageMax : a.Max);
                avg <<= 1)
            {
                for (var max = avg << 1; max < a.Max; max <<= 1)
                {
                    var item = new WorkItem(min, avg, max);
                    Console.WriteLine($"[{workItems.Count}] {item}");
                    workItems.Enqueue(item);
                }
            }
        }
    }

    private async Task LoadFilesAsync(TuneArgs a)
    {
        foreach (var file in Directory.GetFiles(a.Directory!, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetRelativePath(a.Directory!, file);
            fileData.Add(name, await File.ReadAllBytesAsync(file));
        }
    }
}
