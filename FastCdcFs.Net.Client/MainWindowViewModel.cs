using FastCdcFs.Net.Reader;
using Humanizer;
using Microsoft.Win32;
using System.Windows.Input;

namespace FastCdcFs.Net.Client;

public record Entry(string Name);

public record FileEntry(string Name, uint Length) : Entry(Name)
{
    public string LengthText => ByteSize.FromBytes(Length).Humanize("0.##");
}

public record DirectoryEntry(string Name, string FullName) : Entry(Name);

internal class MainWindowViewModel : ViewModelBase
{
    private FastCdcFsReader? reader;

    public MainWindowViewModel()
    {
        LoadCommand = new RelayCommand(Load, () => !Loading);
        LoadDirectoryCommand = new RelayCommand<DirectoryEntry>(LoadDirectory, _ => !Loading);
    }

    public ICommand LoadCommand { get; }

    public ICommand LoadDirectoryCommand { get; }

    public bool Loading { get; private set => SetProperty(ref field, value); }

    public string? CurrentPath { get; private set => SetProperty(ref field, value); }

    public IEnumerable<Entry>? Entries { get; private set => SetProperty(ref field, value); }

    public IEnumerable<Entry>? SelectedEntries { get; set; }

    private async void Load()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open CDCFS file",
            Filter = "CDCFS files (*.cdcfs)|*.cdcfs|All files (*.*)|*.*",
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
            return;

        var tmp = reader;
        Loading = true;

        try
        {
            await DispatchAsync(() =>
            {
                if (tmp is not null)
                {
                    tmp.Dispose();
                }

                reader = new FastCdcFsReader(dialog.FileName);
                Entries = Map("", reader.List());
                return Task.CompletedTask;
            });
        }
        finally
        {
            Loading = false;
        }
    }

    private async void LoadDirectory(DirectoryEntry? entry)
    {
        if (entry is null || reader is null)
            return;

        Loading = true;

        try
        {
            await DispatchAsync(() =>
            {
                Entries = Map(entry.FullName, reader.List(entry.FullName));
                return Task.CompletedTask;
            });
        }
        finally
        {
            Loading = false;
        }
    }

    private IEnumerable<Entry> Map(string path, IEnumerable<Reader.Entry> entries)
    {
        CurrentPath = path;

        var list = new List<Entry>();

        if (path is not "")
        {
            list.Add(new DirectoryEntry("..", FastCdcFsHelper.GetDirectoryName(path)!));
        }

        foreach (var entry in entries)
        {
            if (entry.IsDirectory)
            {
                list.Add(new DirectoryEntry(entry.Name, FastCdcFsHelper.PathCombine(path, entry.Name)));
            }
            else
            {
                list.Add(new FileEntry(entry.Name, entry.Length));
            }
        }

        return list;
    }
}
