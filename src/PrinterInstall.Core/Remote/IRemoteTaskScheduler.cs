using System.Net;

namespace PrinterInstall.Core.Remote;

/// <summary>
/// Contingência para tarefas agendadas no host remoto quando o WMI não consegue iniciar o schtasks.
/// As credenciais seguem pela chamada autenticada, nunca por linha de comando de processo.
/// </summary>
public interface IRemoteTaskScheduler
{
    Task CreateSystemTaskAsync(string host, NetworkCredential credential, string taskName, string executable,
        string arguments, TimeSpan timeout, CancellationToken cancellationToken);

    Task RunTaskAsync(string host, NetworkCredential credential, string taskName, TimeSpan timeout,
        CancellationToken cancellationToken);

    Task StopTaskAsync(string host, NetworkCredential credential, string taskName, TimeSpan timeout,
        CancellationToken cancellationToken);

    Task DeleteTaskAsync(string host, NetworkCredential credential, string taskName, TimeSpan timeout,
        CancellationToken cancellationToken);
}
