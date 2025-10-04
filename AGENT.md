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
- Windows platform required for building `FastCdcFs.Net.Client` (WPF)

## Building the Project

```bash
# Restore dependencies and build all projects
dotnet build

# Build specific project
dotnet build FastCdcFs.Net
dotnet build FastCdcFs.Net.Shell
```

Note: On non-Windows platforms, the WPF client project will fail to build. This is expected.

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
