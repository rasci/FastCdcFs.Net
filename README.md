# CdcFS

A Fast Content-Defined Chunking readonly File System

## Usage

### Create a file system

#### Create the file system with shell / terminal

Use FastCdcFs.Shell create file systems:

```bash
fastcdcfs build -f "/file/to/add"
fastcdcfs build -f "/another/file/to/add" --target "/non/root/destination"
fastcdcfs build -d "/directory/to/add"
fastcdcfs build -d "/another/directory/to/add" --target "/another/non/root/destination" --recursive
fastcdcfs build -o myfs.fastcdcfs
```
#### Create the file system programmatically

Use FastCdcFs.Writer

```csharp
var writer = new FastCdcFsWriter(Options.Default);
writer.AddFile("/file/to/add");
writer.AddFile("/another/file/to/add", "/non/root/destination");
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
fastcdcfs extract --file "/non/root/destination/file" myfs.fastcdcfs --target "/extract/to"

# extract directory (optional recursively)
fastcdcfs extract --directory "/non/root/destination/file" myfs.fastcdcfs --target "/extract/to"
```
#### Read the file system programmatically

Use FastCdcFs.Reader

```csharp
var reader = new CdcFsReader("myfs.fastcdcfs");
foreach (var file in reader.List())
{
    var data = reader.ReadFile($"/{file.Name}");
}
```