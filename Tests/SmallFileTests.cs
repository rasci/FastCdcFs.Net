using FastCdcFs.Net;

namespace Tests;

public class SmallFileTests : TestBase
{
    [Fact]
    public void SmallFilesAreCombinedIntoSolidBlocks()
    {
        // Create options with a small threshold
        var options = FastCdcFsOptions.Default
            .WithSmallFileHandling(threshold: 10 * 1024, blockSize: 32 * 1024);

        var writer = new FastCdcFsWriter(options);
        
        // Add several small files (each 1KB)
        for (int i = 0; i < 10; i++)
        {
            var data = new byte[1024];
            Array.Fill(data, (byte)(i % 256));
            writer.AddFile(data, $"file{i}.txt");
        }

        using var ms = new MemoryStream();
        writer.Build(ms);
        ms.Position = 0;

        // Verify we can read all files back
        using var reader = new FastCdcFsReader(ms);
        
        for (int i = 0; i < 10; i++)
        {
            var entry = reader.Get($"file{i}.txt");
            Assert.True(entry.IsFile);
            Assert.Equal(1024u, entry.Length);
            
            var data = entry.ReadAllBytes();
            Assert.Equal(1024, data.Length);
            Assert.All(data, b => Assert.Equal((byte)(i % 256), b));
        }
    }

    [Fact]
    public void LargeFilesAreNotCombinedIntoSolidBlocks()
    {
        var options = FastCdcFsOptions.Default
            .WithSmallFileHandling(threshold: 10 * 1024, blockSize: 32 * 1024);

        var writer = new FastCdcFsWriter(options);
        
        // Add a large file (20KB, above threshold)
        var largeData = new byte[20 * 1024];
        Array.Fill(largeData, (byte)42);
        writer.AddFile(largeData, "large.txt");

        using var ms = new MemoryStream();
        writer.Build(ms);
        ms.Position = 0;

        using var reader = new FastCdcFsReader(ms);
        var entry = reader.Get("large.txt");
        Assert.True(entry.IsFile);
        Assert.Equal((uint)(20 * 1024), entry.Length);
        
        var data = entry.ReadAllBytes();
        Assert.Equal(20 * 1024, data.Length);
        Assert.All(data, b => Assert.Equal((byte)42, b));
    }

    [Fact]
    public void MixedSmallAndLargeFiles()
    {
        var options = FastCdcFsOptions.Default
            .WithSmallFileHandling(threshold: 10 * 1024, blockSize: 32 * 1024);

        var writer = new FastCdcFsWriter(options);
        
        // Add mix of small and large files
        var smallData = new byte[1024];
        Array.Fill(smallData, (byte)1);
        writer.AddFile(smallData, "small1.txt");

        var largeData = new byte[20 * 1024];
        Array.Fill(largeData, (byte)2);
        writer.AddFile(largeData, "large.txt");

        Array.Fill(smallData, (byte)3);
        writer.AddFile(smallData, "small2.txt");

        using var ms = new MemoryStream();
        writer.Build(ms);
        ms.Position = 0;

        using var reader = new FastCdcFsReader(ms);
        
        var small1 = reader.Get("small1.txt");
        Assert.Equal(1024u, small1.Length);
        Assert.All(small1.ReadAllBytes(), b => Assert.Equal((byte)1, b));

        var large = reader.Get("large.txt");
        Assert.Equal((uint)(20 * 1024), large.Length);
        Assert.All(large.ReadAllBytes(), b => Assert.Equal((byte)2, b));

        var small2 = reader.Get("small2.txt");
        Assert.Equal(1024u, small2.Length);
        Assert.All(small2.ReadAllBytes(), b => Assert.Equal((byte)3, b));
    }

    [Fact]
    public void SolidBlocksAreCreatedWhenSizeExceeded()
    {
        // Create options with a very small block size to force multiple blocks
        var options = FastCdcFsOptions.Default
            .WithSmallFileHandling(threshold: 10 * 1024, blockSize: 5 * 1024);

        var writer = new FastCdcFsWriter(options);
        
        // Add files that will span multiple blocks
        for (int i = 0; i < 10; i++)
        {
            var data = new byte[1024];
            Array.Fill(data, (byte)(i % 256));
            writer.AddFile(data, $"file{i}.txt");
        }

        using var ms = new MemoryStream();
        writer.Build(ms);
        ms.Position = 0;

        // Verify all files can be read correctly
        using var reader = new FastCdcFsReader(ms);
        
        for (int i = 0; i < 10; i++)
        {
            var entry = reader.Get($"file{i}.txt");
            var data = entry.ReadAllBytes();
            Assert.Equal(1024, data.Length);
            Assert.All(data, b => Assert.Equal((byte)(i % 256), b));
        }
    }

    [Fact]
    public void SmallFileStreamReading()
    {
        var options = FastCdcFsOptions.Default
            .WithSmallFileHandling(threshold: 10 * 1024, blockSize: 32 * 1024);

        var writer = new FastCdcFsWriter(options);
        
        var data = new byte[1024];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(i % 256);
        }
        writer.AddFile(data, "small.txt");

        using var ms = new MemoryStream();
        writer.Build(ms);
        ms.Position = 0;

        using var reader = new FastCdcFsReader(ms);
        var entry = reader.Get("small.txt");
        
        using var stream = entry.Open();
        var readData = new byte[1024];
        var bytesRead = stream.Read(readData, 0, readData.Length);
        
        Assert.Equal(1024, bytesRead);
        Assert.Equal(data, readData);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SmallFilesWithDifferentCompressionOptions(bool noZstd, bool noHash)
    {
        var options = FastCdcFsOptions.Default
            .WithSmallFileHandling(threshold: 10 * 1024, blockSize: 32 * 1024)
            .WithNoZstd(noZstd)
            .WithNoHash(noHash);

        var writer = new FastCdcFsWriter(options);
        
        for (int i = 0; i < 5; i++)
        {
            var data = new byte[1024];
            Array.Fill(data, (byte)i);
            writer.AddFile(data, $"file{i}.txt");
        }

        using var ms = new MemoryStream();
        writer.Build(ms);
        ms.Position = 0;

        using var reader = new FastCdcFsReader(ms);
        
        for (int i = 0; i < 5; i++)
        {
            var entry = reader.Get($"file{i}.txt");
            var data = entry.ReadAllBytes();
            Assert.Equal(1024, data.Length);
            Assert.All(data, b => Assert.Equal((byte)i, b));
        }
    }

    [Fact]
    public void ManySmallHtmlLikeFiles()
    {
        // Test the scenario from the issue: 100 HTML files with ~1KB each
        var options = FastCdcFsOptions.Default
            .WithSmallFileHandling(threshold: 1024 * 1024, blockSize: 16 * 1024 * 1024);

        var writer = new FastCdcFsWriter(options);
        
        // Generate HTML-like content with some repetition
        for (int i = 0; i < 100; i++)
        {
            var html = $@"<!DOCTYPE html>
<html>
<head>
    <title>Page {i}</title>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .container {{ max-width: 800px; margin: 0 auto; }}
        h1 {{ color: #333; }}
        p {{ line-height: 1.6; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>Welcome to Page {i}</h1>
        <p>This is page number {i} of our website.</p>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit.</p>
        <p>Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.</p>
        <p>Ut enim ad minim veniam, quis nostrud exercitation ullamco.</p>
    </div>
</body>
</html>";
            var data = System.Text.Encoding.UTF8.GetBytes(html);
            writer.AddFile(data, $"page{i}.html");
        }

        using var ms = new MemoryStream();
        writer.Build(ms);
        var fileSize = ms.Length;
        ms.Position = 0;

        // Verify all files can be read
        using var reader = new FastCdcFsReader(ms);
        
        for (int i = 0; i < 100; i++)
        {
            var entry = reader.Get($"page{i}.html");
            Assert.True(entry.IsFile);
            
            var html = System.Text.Encoding.UTF8.GetString(entry.ReadAllBytes());
            Assert.Contains($"Page {i}", html);
            Assert.Contains($"page number {i}", html);
        }

        // The file size should be significantly smaller than 100 * 1KB due to solid blocks
        // and deduplication of common HTML structure
        Assert.True(fileSize < 100 * 1024, $"File size {fileSize} should be less than 100KB due to deduplication");
    }
}
