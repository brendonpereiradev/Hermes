using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using Moq;
using PrinterInstall.Core.Remote;
using PrinterInstall.Core.Tests.TestSupport;

namespace PrinterInstall.Core.Tests.Remote;

public class RemotePrinterQueryFallbackTests
{
    private const string Host = "new-notebook";
    private static readonly NetworkCredential Credential = new("admin", "test-password", "DOMAIN");
    private readonly Mock<IRemoteDriverFileStager> _stager = new();
    private readonly Mock<IRemoteWmiProcessRunner> _wmi = new();
    private readonly RecordingRemoteTaskScheduler _rpc = new();
    private readonly RemoteHostSessionFactory _sessions;
    private readonly CimRemotePrinterOperations _sut;
    private List<string> _commands => _rpc.Calls;
    private readonly List<string> _events = new();
    private string _json = "[]";
    private string? _script;

    public RemotePrinterQueryFallbackTests()
    {
        _sessions = new RemoteHostSessionFactory(_wmi.Object);
        _wmi.Setup(x => x.RunAsync(Host, Credential, It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new COMException("Access is denied.", unchecked((int)0x80070005)));
        _stager.Setup(x => x.WriteTextFileAsync(Host, Credential, It.IsAny<RemoteDriverStagingPaths>(), "task.ps1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, NetworkCredential, RemoteDriverStagingPaths, string, string, CancellationToken>((_, _, _, _, script, _) => _script = script)
            .Returns(Task.CompletedTask);
        _stager.Setup(x => x.ReadLogAsync(Host, Credential, It.IsAny<RemoteDriverStagingPaths>(), "task.result", It.IsAny<CancellationToken>()))
            .ReturnsAsync("RESULT>> OK");
        _stager.Setup(x => x.ReadLogAsync(Host, Credential, It.IsAny<RemoteDriverStagingPaths>(), "query.json", It.IsAny<CancellationToken>()))
            .Callback(() => _events.Add("read"))
            .ReturnsAsync(() => _json);
        _stager.Setup(x => x.CleanupAsync(Host, Credential, It.IsAny<RemoteDriverStagingPaths>(), It.IsAny<CancellationToken>()))
            .Callback(() => _events.Add("cleanup"))
            .Returns(Task.CompletedTask);
        _sut = new CimRemotePrinterOperations(_stager.Object, _sessions, _wmi.Object,
            new ElevatedRemoteProcessRunner(_wmi.Object, _stager.Object, _rpc));
    }

    [Fact]
    public async Task InitialQuery_DcomDenied_UsesExplicitCredentialsAndSystemWithoutUserProfile()
    {
        _json = "\uFEFF[\"Epson\",\"Brother\"]";
        var drivers = await _sut.ExecuteQueryAsync<IReadOnlyList<string>>(Host, Credential,
            () => throw new UnauthorizedAccessException(), "$queryResult = @('Epson', 'Brother')", CancellationToken.None);

        Assert.Equal(new[] { "Epson", "Brother" }, drivers);
        Assert.True(_sessions.RequiresElevatedExecution(Host));
        Assert.Equal(new[] { "read", "cleanup" }, _events);
        Assert.Equal(3, _commands.Count);
        Assert.StartsWith("Create new-notebook PrinterInstall_", _commands[0]);
        Assert.StartsWith("Run new-notebook", _commands[1]);
        Assert.StartsWith("Delete new-notebook", _commands[2]);
        Assert.All(_rpc.Credentials, c => Assert.Same(Credential, c));
        Assert.Equal("powershell.exe", _rpc.Executable);
        Assert.DoesNotContain(Credential.Password, _rpc.Arguments);
        Assert.Contains("ConvertTo-Json -InputObject $queryResult", _script);
        Assert.DoesNotContain(Credential.Password, _script);

        // As mutações seguintes também precisam evitar o preflight WMI negado.
        var elevated = false;
        await _sut.ExecuteMutationAsync(Host, Credential, null, CancellationToken.None,
            () => throw new Exception("WMI não deve ser repetido"),
            () => { elevated = true; return Task.CompletedTask; });
        Assert.True(elevated);
    }

    [Fact]
    public async Task CachedSession_AllPublicReadOperationsUseScheduledQueries()
    {
        _sessions.RememberElevatedSession(Host);
        Assert.Empty(await _sut.GetInstalledDriverNamesAsync(Host, Credential));
        Assert.Contains("Get-CimInstance Win32_PrinterDriver", _script);
        _json = "[{\"Name\":\"Recepção\",\"PortName\":\"IP_192.0.2.10\",\"DriverName\":\"EPSON Universal Print Driver\"}]";
        var queues = await _sut.ListPrinterQueuesAsync(Host, Credential);
        var queue = Assert.Single(queues);
        Assert.Equal("Recepção", queue.Name);
        Assert.Equal("EPSON Universal Print Driver", queue.DriverName);
        Assert.Contains("Select-Object Name, PortName, DriverName", _script);
        _json = "true";
        Assert.True(await _sut.PrinterQueueExistsAsync(Host, Credential, "Recepção d'água"));
        Assert.Contains("Name=", _script);
        Assert.Contains("d\\''água", _script);
        _json = "2";
        Assert.Equal(2, await _sut.CountPrintersUsingPortAsync(Host, Credential, "IP_192.0.2.10"));
        Assert.Contains("PortName=", _script);
    }

    [Fact]
    public async Task SuccessfulDirectQuery_DoesNotCreateTaskOrElevatedSession()
    {
        var result = await _sut.ExecuteQueryAsync(Host, Credential, () => Task.FromResult(42), "unused", CancellationToken.None);
        Assert.Equal(42, result);
        Assert.Empty(_commands);
        Assert.False(_sessions.RequiresElevatedExecution(Host));
        _stager.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("invalid")]
    [InlineData("{}")]
    public async Task InvalidQueryOutput_FailsWithoutPretendingThereAreNoPrinters(string json)
    {
        _json = json;
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteQueryAsync<IReadOnlyList<string>>(
            Host, Credential, () => throw new UnauthorizedAccessException(), "$queryResult = @()", CancellationToken.None));
        Assert.False(_sessions.RequiresElevatedExecution(Host));
        Assert.Contains("cleanup", _events);
    }

    [Fact]
    public async Task SmbDenied_ReportsDestinationPermissionsAndOriginalWmiFailure()
    {
        _stager.Setup(x => x.WriteTextFileAsync(Host, Credential, It.IsAny<RemoteDriverStagingPaths>(), "task.ps1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Win32Exception(5, "SMB mount failed (Win32 error 5)."));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteQueryAsync<int>(
            Host, Credential, () => throw new UnauthorizedAccessException("Access is denied."), "unused", CancellationToken.None));
        Assert.Contains("WMI/DCOM", error.Message);
        Assert.Contains("Win32Error: 5", error.Message);
        Assert.Contains("Administradores do destino", error.Message);
        Assert.Contains("perfil salvo", error.Message);
        Assert.DoesNotContain(Credential.Password, error.Message);
        Assert.Empty(_commands);
    }

    [Fact]
    public async Task UnrelatedDirectFailure_IsPreservedWithoutFallback()
    {
        var error = new IOException("network failed");
        Assert.Same(error, await Assert.ThrowsAsync<IOException>(() => _sut.ExecuteQueryAsync<int>(
            Host, Credential, () => throw error, "unused", CancellationToken.None)));
        Assert.Empty(_commands);
    }

    [Theory]
    [InlineData(1311, "Nenhum servidor de logon")]
    [InlineData(1355, "domínio informado")]
    public async Task DomainUnavailable_IsDiagnosedSeparatelyFromMissingAdminRights(int code, string detail)
    {
        _stager.Setup(x => x.WriteTextFileAsync(Host, Credential, It.IsAny<RemoteDriverStagingPaths>(), "task.ps1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Win32Exception(code, "SMB failed"));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteQueryAsync<int>(
            Host, Credential, () => throw new UnauthorizedAccessException(), "unused", CancellationToken.None));
        Assert.Contains(detail, error.Message);
        Assert.Contains("DNS", error.Message);
        Assert.Contains("Kerberos", error.Message);
        Assert.DoesNotContain("pertence aos Administradores", error.Message);
        Assert.Empty(_commands);
    }

    [Fact]
    public async Task CancellationDuringWmiBootstrap_DoesNotRunRpcFallback()
    {
        _wmi.Setup(x => x.RunAsync(Host, Credential, It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _sut.ExecuteQueryAsync<int>(
            Host, Credential, () => throw new UnauthorizedAccessException(), "unused", CancellationToken.None));
        Assert.Empty(_commands);
        Assert.Contains("cleanup", _events);
        Assert.False(_sessions.RequiresElevatedExecution(Host));
    }
}
