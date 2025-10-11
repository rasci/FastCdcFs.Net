# FastCdcFS.Net

A .NET library for creating read-only virtual file systems backed by fast content-defined chunking. It allows efficient storage, deduplication, and retrieval of files and directories while keeping access fast and lightweight. Ideal for scenarios where immutable file systems are needed, such as archival storage, distribution, or data integrity�focused applications.

## Usage

### Create a file system

#### Create the file system with shell / terminal

Use FastCdcFs.Shell to a create virtual file-system:

```bash
fastcdcfs build -d "/directory/to/add" -o myfs.fastcdcfs
```

```bash
fastcdcfs build -f "/file/to/add"
fastcdcfs build -f "/another/file/to/add" --target "non/root/destination"
fastcdcfs build -d "/directory/to/add"
fastcdcfs build -d "/another/directory/to/add" --target "another/non/root/destination" --recursive
fastcdcfs build -o myfs.fastcdcfs
```

#### Create the file system programmatically

```csharp
var writer = new FastCdcFsWriter(Options.Default);
writer.AddFile("/file/to/add");
writer.AddFile("/another/file/to/add", "non/root/destination");
writer.Build("/target/location");
```

#### Small file handling

For collections with many small files (e.g., HTML files, configuration files), you can enable solid block optimization:

```csharp
var options = FastCdcFsOptions.Default
    .WithSmallFileHandling(threshold: 1024 * 1024, blockSize: 16 * 1024 * 1024);

var writer = new FastCdcFsWriter(options);
// Add your files...
writer.Build("/target/location");
```

This combines files smaller than the threshold (1 MB by default) into larger blocks (16 MB by default) before chunking, significantly improving storage efficiency and deduplication for many small files.

### Read a file system

#### Read the file system with shell / terminal

```bash
# list files (optional pass --directory to limit outputs to a specific directory)
fastcdcfs list myfs.fastcdcfs
fastcdcfs list myfs.fastcdcfs --directory "/non/root/destination"

# extract everything
fastcdcfs extract myfs.fastcdcfs --target "/extract/to"

# extract file
fastcdcfs extract --file "non/root/destination/file" myfs.fastcdcfs --target "/extract/to"

# extract directory (optional recursively)
fastcdcfs extract --directory "non/root/destination/file" myfs.fastcdcfs --target "/extract/to"
```
#### Read the file system programmatically

```csharp
using var reader = new CdcFsReader("myfs.fastcdcfs");

foreach (var file in reader.List().Where(e => e.IsFile))
{
    // read all bytes
    var data = file.ReadAllBytes();

    // get stream
    var stream = file.OpenRead();
}
```

```csharp
using var reader = new CdcFsReader("myfs.fastcdcfs");

foreach (var file in reader.List("a/known/directory"))
{
    // ...
}
```

```csharp
using var reader = new CdcFsReader("myfs.fastcdcfs");

// read known entries immediately
var entry = reader.Get("some/known/file/path");
var stream = entry!.Open();
```

## File System Format

### Version 2 (Current)

Version 2 adds support for small file handling through solid blocks, which combine multiple small files into larger blocks before chunking. This significantly improves storage efficiency for many small files.

```
+--------------------------------------------------------------------------------------+
| [0x00..0x08]      magic: utf8 "FASTCDCFS"         | identifies file
| [0x09..0x09]      version: byte (2)               | identifies the file system version
| [0x0A..0x0A]      mode: byte                      | identifies the file system modes
| [0x0B..0x0C]      meta data length: u32           | length of metadata
| [0x0D..0x0E]  -   directory count: u32            | number of directories
| [0x0F..]     |C|  directory table: <repeated>
| [..]         |O|    parent id: u32                | parent id of directory
| [..]         |M|    name: utf8                    | name of directory
| [..]         |P|  files count: u32                | number of files
| [..]         |R|  files table: <repeated>
| [..]         |E|    directory id: u32             | id of directory
| [..]         |S|    name: utf8                    | name of file
| [..]         |S|    length: u32                   | length of file
| [..]         |E|    chunk count: u32              | number of chunk ids, when length > 0
| [..]         |D|    file chunk table: <repeated>
| [..]         |*|      chunk id: u32               | chunk id
| [..]         |.|  compression dict length: u32    | length of compression dict *
| [..]         |.|  compression dict: raw           | compression dict *
| [..]         |.|  chunk boundary count: u32       | number of chunk boundaries
| [..]         |.|  chunk boundary table: <repeated>
| [..]         |.|    chunk length: u32             | length of chunk
| [..]         | |    compressed chunk length: u32  | length of compressed chunk *
| [..]          -     chunk hash: u64               | xxHash64 of the chunk **
| [..]              meta data hash: u64             | xxHash64 of the meta data **
| [..]              chunk blobs: raw                | chunk blobs
+--------------------------------------------------------------------------------------+

* only available when the mode is not nozstd
** only available when the mode is not nohash
*** only available in version 2+
```

- the meta data is only compressed when the mode is not nozstd
- the utf8 string encoding uses a 7-bit encoded length prefix
- the id of a directory is the index in the directory table
- the id of a chunk is the index in the chunk boundary table
- the id of a solid block is the index in the solid block table
- files smaller than the threshold are combined into solid blocks
- chunk count of 0 with length > 0 indicates the file is in a solid block
- chunk count of 0 with length = 0 indicates an empty file

### Version 1 (Legacy)

Version 1 is the original format without solid block support. The format is the same as Version 2 but without the solid block table and related file metadata.

### File System Modes

```
None:   0x0
NoZstd: 0x1
NoHash: 0x2
```