# FastCdcFS.Net

A .NET library for creating read-only file systems backed by fast content-defined chunking. It allows efficient storage, deduplication, and retrieval of files and directories while keeping access fast and lightweight. Ideal for scenarios where immutable file systems are needed, such as archival storage, distribution, or data integrity–focused applications.

## Usage

### Create a file system

#### Create the file system with shell / terminal

Use FastCdcFs.Shell create file systems:

```bash
fastcdcfs build -f "/file/to/add"
fastcdcfs build -f "/another/file/to/add" --target "non/root/destination"
fastcdcfs build -d "/directory/to/add"
fastcdcfs build -d "/another/directory/to/add" --target "another/non/root/destination" --recursive
fastcdcfs build -o myfs.fastcdcfs
```
#### Create the file system programmatically

Use FastCdcFs.Writer

```csharp
var writer = new FastCdcFsWriter(Options.Default);
writer.AddFile("/file/to/add");
writer.AddFile("/another/file/to/add", "non/root/destination");
writer.Build("/target/location");
```

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

Use FastCdcFs.Reader

```csharp
using var reader = new CdcFsReader("myfs.fastcdcfs");

foreach (var file in reader.List())
{
    // read all bytes
    var data = file.ReadAllBytes();

    // get stream
    var stream = file.OpenRead();
}
```

```csharp
using var reader = new CdcFsReader("myfs.fastcdcfs");

// read known entries immediately
var entry = reader.Get("some/known/file/path");
var stream = entry!.Open();
```

## File System Format

The id of a directory is the index in the directory table
The id of a chunk is the index in the chunk boundary table

```
+--------------------------------------------------------------------------------------+
| [0x00..0x08]   magic: utf8 "FASTCDCFS"            | identifies file
| [0x09..0x0A]   mode: byte                         | identifies the file modes
| [0x0B..0x0D]   directory count: u32               | number of directories
| [0x0E..]       directory table: <repeated>
| [..]              parent id: u32                  | parent id of directory
| [..]              name: utf8                      | name of directory
| [..]           files count: u32                   | number of files
| [..]           files table: <repeated>
| [..]              directory id: u32               | id of directory
| [..]              name: utf8                      | name of file
| [..]              length: u32                     | length of file
| [..]              chunk count: u32                | number of chunk ids
| [..]              file chunk table: <repeated>
| [..]                  chunk id: u32               | chunk id
| [..]           compression dict length: u32       | length of compression dict*
| [..]           compression dict                   | compression dict*
| [..]           chunk boundary count: u32          | number of chunk boundaries
| [..]           chunk boundary table: <repeated>
| [..]              chunk length: u32               | length of chunk
| [..]              compressed chunk length: u32    | length of compressed chunk*
| [..]           chunks: raw                        | chunks
+--------------------------------------------------------------------------------------+

* only available when mode is not nozst
```

### File System Modes

```
None = 0x0
NoZstd = 0x1
```