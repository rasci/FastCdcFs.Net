using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FastCdcFs.Net.Client;

internal abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected Task DispatchAsync(Func<Task> a)
        => Task.Run(async () =>
        {
            try
            {
                await a();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Oops", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });

    protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new(propertyName));

    protected bool SetProperty<T>(ref T t_old, T t_new, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(t_old, t_new))
            return false;

        t_old = t_new;
        PropertyChanged?.Invoke(this, new(propertyName));
        return true;
    }

    protected bool SetProperty(ref bool old, bool @new, [CallerMemberName] string propertyName = "")
    {
        if (old == @new)
            return false;

        old = @new;
        PropertyChanged?.Invoke(this, new(propertyName));
        return true;
    }

    protected bool SetProperty(ref int old, int @new, [CallerMemberName] string propertyName = "")
    {
        if (old == @new)
            return false;

        old = @new;
        PropertyChanged?.Invoke(this, new(propertyName));
        return true;
    }

    protected bool SetProperty(ref string? old, string? @new, [CallerMemberName] string propertyName = "")
    {
        if (old == @new)
            return false;

        old = @new;
        PropertyChanged?.Invoke(this, new(propertyName));
        return true;
    }

    protected bool SetProperty(ref float old, float @new, [CallerMemberName] string propertyName = "")
    {
        if (old == @new)
            return false;

        old = @new;
        PropertyChanged?.Invoke(this, new(propertyName));
        return true;
    }

    protected bool SetProperty(ref double old, double @new, [CallerMemberName] string propertyName = "")
    {
        if (old == @new)
            return false;

        old = @new;
        PropertyChanged?.Invoke(this, new(propertyName));
        return true;
    }

    public static void SetProperty(object sender, PropertyChangedEventHandler? handler, ref bool old, bool @new, [CallerMemberName] string? name = null)
    {
        if (old == @new)
            return;

        old = @new;
        handler?.Invoke(sender, new(name));
    }

    public static void SetProperty(object sender, PropertyChangedEventHandler? handler, ref int old, int @new, [CallerMemberName] string? name = null)
    {
        if (old == @new)
            return;

        old = @new;
        handler?.Invoke(sender, new(name));
    }
}
