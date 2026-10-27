using System.Net;
using Moq;
using PrinterInstall.App.Resources;
using PrinterInstall.App.Services;
using PrinterInstall.App.ViewModels;
using PrinterInstall.Core.Auth;
using PrinterInstall.Core.Models;
using PrinterInstall.Core.Orchestration;
using PrinterInstall.Core.Remote;

namespace PrinterInstall.App.Tests.ViewModels;

/// <summary>Sessão aberta por "Usar localmente", sem domínio, para uso doméstico.</summary>
public class LocalOnlySessionTests
{
    private static LoginViewModel CreateLogin(SessionContext session, Mock<ILdapCredentialValidator> ldap)
    {
        var settings = new Mock<IAppSettingsStore>();
        settings.Setup(s => s.Load()).Returns(new PrinterInstall.App.Models.AppSettings("laboratorio.test"));
        return new LoginViewModel(ldap.Object, session, settings.Object, Mock.Of<IRememberedUserStore>());
    }

    private static SessionContext CreateLocalSession() => new()
    {
        Credential = new NetworkCredential(Environment.UserName, "", Environment.MachineName),
        IsLocalOnly = true
    };

    [Fact]
    public void SignInLocally_OpensLocalOnlySessionWithoutContactingDomain()
    {
        var session = new SessionContext();
        var ldap = new Mock<ILdapCredentialValidator>(MockBehavior.Strict);
        var sut = CreateLogin(session, ldap);

        sut.SignInLocally();

        Assert.True(session.IsLocalOnly);
        Assert.NotNull(session.Credential);
        Assert.Equal(Environment.UserName, session.Credential!.UserName);
        Assert.Equal("", session.Credential.Password);
        Assert.Equal("", session.DomainName);
    }

    [Fact]
    public async Task TryLoginAsync_WithDomain_ClearsLocalOnlyFlag()
    {
        var session = new SessionContext { IsLocalOnly = true };
        var ldap = new Mock<ILdapCredentialValidator>();
        ldap.Setup(l => l.ValidateAsync(It.IsAny<string>(), It.IsAny<NetworkCredential>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LdapValidationResult.Success());
        var sut = CreateLogin(session, ldap);
        sut.UserName = "operador";
        sut.Password = "senha";

        var (success, _) = await sut.TryLoginAsync();

        Assert.True(success);
        Assert.False(session.IsLocalOnly);
    }

    [Fact]
    public void MainViewModel_LocalOnly_PrefillsThisComputer()
    {
        var identity = new LocalMachineIdentity();

        var sut = new MainViewModel(CreateLocalSession(), null!, null!, null!, identity);

        Assert.True(sut.IsLocalOnly);
        Assert.Equal(identity.GetPrimaryLocalName(), sut.ComputersText);
    }

    [Fact]
    public async Task MainViewModel_LocalOnly_RejectsRemoteTargetWithoutCallingIt()
    {
        var identity = new LocalMachineIdentity();
        var remoteMock = new Mock<IRemotePrinterOperations>();
        var sut = new MainViewModel(
            CreateLocalSession(),
            TestOrchestratorFactory.Create(remoteMock.Object),
            new DeploymentRollbackRunner(remoteMock.Object, new PrinterControlOrchestrator(remoteMock.Object)),
            null!,
            identity);
        sut.ComputersText = "pc-remoto-01";
        sut.PrinterRows[0].Brand = PrinterBrand.Epson;
        sut.PrinterRows[0].DisplayName = "Escritório";
        sut.PrinterRows[0].PrinterHostAddress = "192.0.2.10";

        await sut.DeployCommand.ExecuteAsync(null);

        var row = Assert.Single(sut.Targets);
        Assert.Equal(TargetMachineState.Error, row.State);
        Assert.Equal(UiStrings.Main_LocalOnlyRemoteBlocked, row.Message);
        remoteMock.Verify(
            m => m.GetInstalledDriverNamesAsync("pc-remoto-01", It.IsAny<NetworkCredential>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemovalWizard_LocalOnly_BlocksRemoteComputers()
    {
        var identity = new LocalMachineIdentity();
        var remoteMock = new Mock<IRemotePrinterOperations>();
        var sut = new RemovalWizardViewModel(
            CreateLocalSession(),
            remoteMock.Object,
            new PrinterControlOrchestrator(remoteMock.Object),
            localMachineIdentity: identity);

        Assert.Equal(identity.GetPrimaryLocalName(), sut.ComputersText);

        sut.ComputersText = "pc-remoto-01";
        await sut.StartCommand.ExecuteAsync(null);

        Assert.Equal(0, sut.CurrentStepIndex);
        Assert.Contains("pc-remoto-01", sut.LogText);
        remoteMock.Verify(
            m => m.ListPrinterQueuesAsync(It.IsAny<string>(), It.IsAny<NetworkCredential>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
