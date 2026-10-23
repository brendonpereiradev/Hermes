using System.Net;
using System.Runtime.InteropServices;
using PrinterInstall.Core.Auth;

namespace PrinterInstall.Core.Remote;

/// <summary>
/// Acessa o Agendador de Tarefas remoto pela API COM (ITaskService) sobre RPC, o mesmo canal do
/// schtasks /S, mas sem expor usuário e senha na linha de comando de um processo local.
/// </summary>
public sealed class RpcRemoteTaskScheduler : IRemoteTaskScheduler
{
    private const int TaskCreateOrUpdate = 6;
    private const int TaskLogonServiceAccount = 5;
    private const int TaskRunLevelHighest = 1;
    private const int TaskActionExec = 0;
    private const string SystemAccount = "SYSTEM";

    public Task CreateSystemTaskAsync(string host, NetworkCredential credential, string taskName, string executable,
        string arguments, TimeSpan timeout, CancellationToken cancellationToken) =>
        ExecuteAsync(host, credential, timeout, cancellationToken, service =>
        {
            var definition = service.NewTask(0);
            definition.Principal.UserId = SystemAccount;
            definition.Principal.LogonType = TaskLogonServiceAccount;
            definition.Principal.RunLevel = TaskRunLevelHighest;
            var action = definition.Actions.Create(TaskActionExec);
            action.Path = executable;
            action.Arguments = arguments;
            service.GetFolder(@"\").RegisterTaskDefinition(
                taskName, definition, TaskCreateOrUpdate, SystemAccount, null, TaskLogonServiceAccount);
        });

    public Task RunTaskAsync(string host, NetworkCredential credential, string taskName, TimeSpan timeout,
        CancellationToken cancellationToken) =>
        ExecuteAsync(host, credential, timeout, cancellationToken,
            service => service.GetFolder(@"\").GetTask(taskName).Run(null));

    public Task StopTaskAsync(string host, NetworkCredential credential, string taskName, TimeSpan timeout,
        CancellationToken cancellationToken) =>
        ExecuteAsync(host, credential, timeout, cancellationToken,
            service => service.GetFolder(@"\").GetTask(taskName).Stop(0));

    public Task DeleteTaskAsync(string host, NetworkCredential credential, string taskName, TimeSpan timeout,
        CancellationToken cancellationToken) =>
        ExecuteAsync(host, credential, timeout, cancellationToken,
            service => service.GetFolder(@"\").DeleteTask(taskName, 0));

    private static Task ExecuteAsync(string host, NetworkCredential credential, TimeSpan timeout,
        CancellationToken cancellationToken, Action<dynamic> operation)
    {
        ArgumentNullException.ThrowIfNull(credential);
        cancellationToken.ThrowIfCancellationRequested();

        // A chamada COM bloqueia sem suporte a cancelamento; o tempo limite libera o fluxo de implantação.
        return Task.Run(() =>
        {
            var serviceType = Type.GetTypeFromProgID("Schedule.Service", throwOnError: true)!;
            dynamic service = Activator.CreateInstance(serviceType)!;
            try
            {
                var (userName, domain) = CredentialHelper.SplitDomainAndUser(credential.UserName, credential.Domain);
                service.Connect(host.Trim(), userName, string.IsNullOrEmpty(domain) ? null : domain, credential.Password);
                operation(service);
            }
            finally
            {
                Marshal.FinalReleaseComObject(service);
            }
        }, cancellationToken).WaitAsync(timeout, cancellationToken);
    }
}
