using System.Net;
using System.Runtime.InteropServices;
using Moq;
using PrinterInstall.Core.Remote;
using PrinterInstall.Core.Tests.TestSupport;

namespace PrinterInstall.Core.Tests.Remote;

public class ElevatedRemoteProcessRunnerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelAfterTaskStarts_EndsTaskBeforeDeletingIt(bool useRpc)
    {
        using var cts = new CancellationTokenSource();
        var wmi = new Mock<IRemoteWmiProcessRunner>();
        var rpc = new RecordingRemoteTaskScheduler { OnRun = cts.Cancel };
        var stager = new Mock<IRemoteDriverFileStager>();
        var actions = new List<string>();
        var messages = new List<string>();
        wmi.Setup(x => x.RunAsync(Host, Cred, It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns<string, NetworkCredential, string, TimeSpan, CancellationToken>((_, _, cmd, _, _) =>
            {
                actions.Add(cmd);
                if (!useRpc && cmd.Contains("/Run")) cts.Cancel();
                return Task.FromResult(new RemoteProcessResult(useRpc ? 5u : 0u, 123, false));
            });
        stager.Setup(x => x.WriteTextFileAsync(Host, Cred, It.IsAny<RemoteDriverStagingPaths>(), "task.ps1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        stager.Setup(x => x.ReadLogAsync(Host, Cred, It.IsAny<RemoteDriverStagingPaths>(), "task.log", It.Is<CancellationToken>(t => !t.IsCancellationRequested)))
            .Callback(() => actions.Add("read-transcript")).ReturnsAsync("Driver step in progress");
        stager.Setup(x => x.CleanupAsync(Host, Cred, It.IsAny<RemoteDriverStagingPaths>(), It.IsAny<CancellationToken>()))
            .Callback(() => actions.Add("cleanup")).Returns(Task.CompletedTask);
        var runner = new ElevatedRemoteProcessRunner(wmi.Object, stager.Object, rpc);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runner.RunElevatedScriptAsync(
            Host, Cred, RemoteElevatedScriptBuilder.WrapWithResultHandling("Write-Output 'test'"), TimeSpan.FromSeconds(20),
            new InlineProgress<string>(messages.Add), cts.Token));

        if (useRpc)
            Assert.Equal(new[] { "Create", "Run", "Stop", "Delete" }, rpc.Calls.Select(c => c.Split(' ')[0]));
        else
        {
            var end = actions.FindIndex(a => a.Contains("/End"));
            var delete = actions.FindIndex(a => a.Contains("/Delete"));
            Assert.True(end >= 0 && delete > end, string.Join("\n", actions));
            Assert.True(actions.IndexOf("read-transcript") > end && actions.IndexOf("read-transcript") < delete);
        }
        Assert.Contains("Driver step in progress", messages);
        Assert.Equal("cleanup", actions[^1]);
    }

    private static readonly NetworkCredential Cred = new("user", "pass\\word\"", "DOMAIN");
    private const string Host = "remote-pc";

    [Fact]
    public async Task RunElevatedScriptAsync_CreatesRunsAndDeletesScheduledTask()
    {
        var wmi = new Mock<IRemoteWmiProcessRunner>();
        var stager = new Mock<IRemoteDriverFileStager>();
        var commands = new List<string>();

        wmi.Setup(x => x.RunAsync(Host, Cred, It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<string, NetworkCredential, string, TimeSpan, CancellationToken>((_, _, cmd, _, _) => commands.Add(cmd))
            .ReturnsAsync(new RemoteProcessResult(0, 123, TimedOut: false));

        stager.Setup(x => x.WriteTextFileAsync(Host, Cred, It.IsAny<RemoteDriverStagingPaths>(), "task.ps1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        stager.Setup(x => x.ReadLogAsync(Host, Cred, It.IsAny<RemoteDriverStagingPaths>(), "task.result", It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        stager.Setup(x => x.ReadLogAsync(Host, Cred, It.IsAny<RemoteDriverStagingPaths>(), "task.log", It.IsAny<CancellationToken>()))
            .ReturnsAsync("RESULT>> OK");

        stager.Setup(x => x.CleanupAsync(Host, Cred, It.IsAny<RemoteDriverStagingPaths>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new ElevatedRemoteProcessRunner(wmi.Object, stager.Object, new RecordingRemoteTaskScheduler());
        await sut.RunElevatedScriptAsync(
            Host,
            Cred,
            RemoteElevatedScriptBuilder.WrapWithResultHandling("Write-Output 'test'"),
            TimeSpan.FromMinutes(1),
            log: null,
            CancellationToken.None);

        Assert.Contains(commands, c => c.Contains("schtasks /Create", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(commands, c => c.Contains("/RU SYSTEM", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(commands, c => c.Contains("-ExecutionPolicy Bypass -File \\\"C:\\", StringComparison.Ordinal));
        Assert.Contains(commands, c => c.Contains("schtasks /Run", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(commands, c => c.Contains("schtasks /Delete", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(commands, c => c.Contains(Cred.Password, StringComparison.Ordinal) || c.Contains("/RP", StringComparison.OrdinalIgnoreCase));
        stager.Verify(x => x.CleanupAsync(Host, Cred, It.IsAny<RemoteDriverStagingPaths>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunElevatedScriptAsync_ResultFail_Throws()
    {
        var wmi = new Mock<IRemoteWmiProcessRunner>();
        var stager = new Mock<IRemoteDriverFileStager>();

        wmi.Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<NetworkCredential>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoteProcessResult(0, 1, TimedOut: false));
        stager.Setup(x => x.WriteTextFileAsync(It.IsAny<string>(), It.IsAny<NetworkCredential>(), It.IsAny<RemoteDriverStagingPaths>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        stager.Setup(x => x.ReadLogAsync(It.IsAny<string>(), It.IsAny<NetworkCredential>(), It.IsAny<RemoteDriverStagingPaths>(), "task.result", It.IsAny<CancellationToken>()))
            .ReturnsAsync("RESULT>> FAIL Acesso negado");
        stager.Setup(x => x.ReadLogAsync(It.IsAny<string>(), It.IsAny<NetworkCredential>(), It.IsAny<RemoteDriverStagingPaths>(), "task.log", It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        stager.Setup(x => x.CleanupAsync(It.IsAny<string>(), It.IsAny<NetworkCredential>(), It.IsAny<RemoteDriverStagingPaths>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new ElevatedRemoteProcessRunner(wmi.Object, stager.Object, new RecordingRemoteTaskScheduler());
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RunElevatedScriptAsync(Host, Cred, "Write-Output x", TimeSpan.FromMinutes(1), null, CancellationToken.None));
        Assert.Contains("Execução elevada", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Acesso negado", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunElevatedScriptAsync_WhenWmiReturnsError_UsesRpcSchedulerWithoutCommandLineCredentials()
    {
        var wmi = new Mock<IRemoteWmiProcessRunner>();
        var stager = new Mock<IRemoteDriverFileStager>();
        var fallback = new RecordingRemoteTaskScheduler();

        // WMI retorna erro 8 (Unknown failure) na criação
        wmi.Setup(x => x.RunAsync(Host, Cred, It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoteProcessResult(8, null, TimedOut: false));

        stager.Setup(x => x.WriteTextFileAsync(Host, Cred, It.IsAny<RemoteDriverStagingPaths>(), "task.ps1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        stager.Setup(x => x.ReadLogAsync(Host, Cred, It.IsAny<RemoteDriverStagingPaths>(), "task.result", It.IsAny<CancellationToken>()))
            .ReturnsAsync("RESULT>> OK");
        stager.Setup(x => x.ReadLogAsync(Host, Cred, It.IsAny<RemoteDriverStagingPaths>(), "task.log", It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        stager.Setup(x => x.CleanupAsync(Host, Cred, It.IsAny<RemoteDriverStagingPaths>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new ElevatedRemoteProcessRunner(wmi.Object, stager.Object, fallback);
        await sut.RunElevatedScriptAsync(
            Host,
            Cred,
            "Write-Output 'fallback-test'",
            TimeSpan.FromMinutes(1),
            log: null,
            CancellationToken.None);

        Assert.Equal(new[] { "Create", "Run", "Delete" }, fallback.Calls.Select(c => c.Split(' ')[0]));
        Assert.All(fallback.Calls, c => Assert.StartsWith(c.Split(' ')[0] + " remote-pc PrinterInstall_", c));
        Assert.All(fallback.Credentials, c => Assert.Same(Cred, c));
        Assert.Equal("powershell.exe", fallback.Executable);
        Assert.StartsWith("-NoProfile -ExecutionPolicy Bypass -File \"C:\\", fallback.Arguments);
        Assert.EndsWith("task.ps1\"", fallback.Arguments);
        stager.Verify(x => x.CleanupAsync(Host, Cred, It.IsAny<RemoteDriverStagingPaths>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunElevatedScriptAsync_WhenWmiAndFallbackBothFail_ThrowsDetailedException()
    {
        var wmi = new Mock<IRemoteWmiProcessRunner>();
        var stager = new Mock<IRemoteDriverFileStager>();
        var fallback = new RecordingRemoteTaskScheduler
        {
            CreateException = new COMException("RPC server unavailable", unchecked((int)0x800706BA))
        };

        // WMI retorna erro 8
        wmi.Setup(x => x.RunAsync(Host, Cred, It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoteProcessResult(8, null, TimedOut: false));

        stager.Setup(x => x.WriteTextFileAsync(Host, Cred, It.IsAny<RemoteDriverStagingPaths>(), "task.ps1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        stager.Setup(x => x.CleanupAsync(Host, Cred, It.IsAny<RemoteDriverStagingPaths>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new ElevatedRemoteProcessRunner(wmi.Object, stager.Object, fallback);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RunElevatedScriptAsync(Host, Cred, "Write-Output 'fail-both'", TimeSpan.FromMinutes(1), null, CancellationToken.None));

        Assert.Contains("WMI return 8", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RPC fallback: RPC server unavailable", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<COMException>(ex.InnerException);
    }
}
