using System.Windows.Input;

namespace FastCdcFs.Net.Client;

internal class RelayCommand(Action action, Func<bool> canExecute) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public RelayCommand(Action action)
        : this(action, () => true)
    {
    }

    public bool CanExecute(object? _)
        => canExecute();

    public void Execute(object? _)
        => action();
}

public class RelayCommand<T>(Action<T?> action, Predicate<T?> canExecute) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public RelayCommand(Action<T?> action)
        : this(action, o => true)
    {
    }
    public bool CanExecute(object? parameter)
        => canExecute((T?)parameter);

    public void Execute(object? parameter)
        => action((T?)parameter);
}
