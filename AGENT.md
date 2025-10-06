# Agent Development Guide

## Project Overview

FastCdcFS.Net is a .NET library for creating read-only file systems backed by fast content-defined chunking. It provides efficient storage, deduplication, and retrieval of files and directories.

## Project Structure

- `FastCdcFs.Net/` - Core library implementing the file system
- `FastCdcFs.Net.Shell/` - Command-line tool for working with file systems
- `FastCdcFs.Net.Client/` - WPF GUI client (Windows-only)
- `Tests/` - Unit tests using xUnit
- `Test/` - Additional test project

## Build Requirements

- .NET SDK 8.0 or higher (projects target net8.0 and net10.0)
- Install the .NET 10.0 rc1 or newer SDK to work with the project
- Linux, macOS, or Windows (cross-platform support)

**Note**: The `FastCdcFs.Net.Client` project is a Windows-only WPF GUI client. Agents typically do not need to build or work on this project, and it can be safely ignored on non-Windows platforms.

## Building the Project

```bash
# Build core projects (cross-platform)
dotnet build FastCdcFs.Net
dotnet build FastCdcFs.Net.Shell

# Build all projects including tests
dotnet build --no-incremental
```

**Note**: Building the entire solution with `dotnet build` will fail on non-Windows platforms due to the WPF client project. This is expected and can be ignored. Focus on the core library and shell tool which are the primary development targets for agents.

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests for specific project
dotnet test Tests/Tests.csproj
```

## Packaging

The `build.cmd` script handles testing and packaging:
- Runs all tests first
- Packages `FastCdcFs.Net` and `FastCdcFs.Net.Shell` as NuGet packages

## Key Technologies

- **Content-Defined Chunking**: FastCDC algorithm for deduplication
- **Compression**: Zstandard (ZstdSharp.Port) for chunk compression
- **Hashing**: xxHash64 (System.IO.Hashing) for chunk identification
- **CLI Parsing**: CommandLineParser for shell tool

## Development Notes

- The project uses modern C# with nullable reference types enabled
- File system format is documented in README.md
- Core library has multi-targeting for .NET 8.0 and .NET 10.0
- Tests use xUnit framework with representative test data generation
