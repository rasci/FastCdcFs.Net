using FastCdcFs.Net.Writer;

var writer = new FastCdcFsWriter(Options.Default);

foreach (var file in Directory.GetFiles(@"d:\work\fastcdsfs\dlbs"))
{
    Console.WriteLine($"adding {file}");

    if (file.Contains("res_"))
    {
        writer.AddFile(file, $"residents/{Path.GetFileName(file)}");
    }
    else if (Path.GetExtension(file) is not ".dlb")
    {
        writer.AddFile(file, $"other/{Path.GetFileName(file)}");
    }
    else if (file.Contains("_C"))
    {
        writer.AddFile(file, $"release/slaves/{Path.GetFileName(file)}");
    }
    else if (file.Contains("_V"))
    {
        writer.AddFile(file, $"test/slaves/{Path.GetFileName(file)}");
    }
    else if (file.Contains("_D"))
    {
        writer.AddFile(file, $"release/fusion/{Path.GetFileName(file)}");
    }
    else if (file.Contains("_W"))
    {
        writer.AddFile(file, $"test/fusion/{Path.GetFileName(file)}");
    }
    else if (file.Contains("_B"))
    {
        writer.AddFile(file, $"release/nucleus/{Path.GetFileName(file)}");
    }
    else if (file.Contains("_U"))
    {
        writer.AddFile(file, $"test/nucleus/{Path.GetFileName(file)}");
    }
    else
    {
        writer.AddFile(file, $"other/{Path.GetFileName(file)}");
    }
}

writer.Build(@"d:\work\chunkfs\structured-current-test.cdcfs");