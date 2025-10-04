using FastCdcFs.Net;

var cdcfsfs = @"d:\work\bcr\firmware-images-smartbox-new-header-cache.fastcdcfs";
var i = 0;

var writer = new FastCdcFsWriter(o => o.WithChunkSizes(32 * 1024, 64 * 1024, 256 * 1024));

//foreach (var file in Directory.GetFiles(@"D:\work\bcr\current-dlbs"))
//{
//    Console.WriteLine($"adding {file}");
//    writer.AddFile(file, Path.GetFileName(file));
//}

//writer.Build(cdcfsfs);

using var reader = new FastCdcFsReader(File.OpenRead(cdcfsfs));

foreach (var entry in reader.List())
{
    Console.WriteLine($"read {entry.Name}");

    using var ms = new MemoryStream();
    using var stream = entry.Open();
    stream.CopyTo(ms);
    var data = ms.ToArray();

    var sourcePath = Path.Combine(@"D:\work\bcr\current-dlbs", entry.Name);
    var sourceData = File.ReadAllBytes(sourcePath);

    for (i = 0; i < sourceData.Length; i++)
    {
        if (data[i] != sourceData[i])
        {
            Console.WriteLine(string.Join("", data.Skip(i)));
            throw new Exception("data mismatch");
        }
    }       
}