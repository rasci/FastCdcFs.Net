using FastCdcFs.Net;

var source = @"D:\work\bcr\uis";
var cdcfsfs = @"d:\work\bcr\firmware-images-smartbox.fastcdcfs";
//var cdcfsfs = @"d:\work\bcr\uis.fastcdcfs";
var dump = @"d:\work\bcr\dump.txt";
var i = 0;

//var writer = new FastCdcFsWriter(o => o
//    //.WithChunkSizes(1024, 32 * 1024, 128 * 1024)
//    .WithSmallFileHandling(1000 * 1024, 64 * 1000 * 1024));

//writer.AddDirectory(source);

//writer.Build(cdcfsfs);


//2269 chunks

using var reader = new FastCdcFsReader(cdcfsfs);

await File.WriteAllTextAsync(dump, FastCdcFsHelper.Dump(reader));

//foreach (var entry in reader.List().Where(e => e.IsFile))
//{
//    Console.WriteLine($"read {entry.Name}");

//    using var ms = new MemoryStream();
//    using var stream = entry.Open();
//    stream.CopyTo(ms);
//    var data = ms.ToArray();

//    var sourcePath = Path.Combine(source, entry.Name);
//    var sourceData = File.ReadAllBytes(sourcePath);

//    for (i = 0; i < sourceData.Length; i++)
//    {
//        if (data[i] != sourceData[i])
//        {
//            Console.WriteLine(string.Join("", data.Skip(i)));
//            throw new Exception("data mismatch");
//        }
//    }       
//}