using System.Windows;
using System.Windows.Controls;

namespace FastCdcFs.Net.Client;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel = new();

    public MainWindow()
    {
        InitializeComponent();

        DataContext = viewModel;
    }

    private void ListBoxItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem lbi && lbi.DataContext is DirectoryEntry entry)
        {
            viewModel.LoadDirectoryCommand.Execute(entry);
        }
    }
}