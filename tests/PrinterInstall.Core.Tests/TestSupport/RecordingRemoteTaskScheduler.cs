using System.Net;
using PrinterInstall.Core.Remote;

namespace PrinterInstall.Core.Tests.TestSupport;

internal sealed class RecordingRemoteTaskScheduler : IRemoteTaskScheduler
{
    public List<string> Calls { get; } = new();
    public List<NetworkCredential> Credentials { get; } = new();
    public string? Executable { get; private set; }
    public string? Arguments { get; private set; }
    public Exception? CreateException { get; set; }
    public Action? OnRun { get; set; }

    public Task CreateSystemTaskAsync(string host, NetworkCredential credential, string taskName, string executable,
        string arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        Record("Create", host, credential, taskName);
        Executable = executable;
        Arguments = arguments;
        return CreateException is null ? Task.CompletedTask : Task.FromException(CreateException);
    }

    public Task RunTaskAsync(string host, NetworkCredential credential, string taskName, TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        Record("Run", host, credential, taskName);
        OnRun?.Invoke();
        return Task.CompletedTask;
    }

    public Task StopTaskAsync(string host, NetworkCredential credential, string taskName, TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        Record("Stop", host, credential, taskName);
        return Task.CompletedTask;
    }

    public Task DeleteTaskAsync(string host, NetworkCredential credential, string taskName, TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        Record("Delete", host, credential, taskName);
        return Task.CompletedTask;
    }

    private void Record(string operation, string host, NetworkCredential credential, string taskName)
    {
        Calls.Add($"{operation} {host} {taskName}");
        Credentials.Add(credential);
    }
}
