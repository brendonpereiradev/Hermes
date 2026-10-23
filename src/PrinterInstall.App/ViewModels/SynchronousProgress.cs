namespace PrinterInstall.App.ViewModels;

/// <summary>
/// Repassa cada relatório imediatamente, na thread que o emitiu. Diferente de <see cref="Progress{T}"/>,
/// não depende do contexto de sincronização, e a ordem dos eventos no log é preservada.
/// </summary>
internal sealed class SynchronousProgress<T> : IProgress<T>
{
    private readonly Action<T> _handler;

    public SynchronousProgress(Action<T> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public void Report(T value) => _handler(value);
}
