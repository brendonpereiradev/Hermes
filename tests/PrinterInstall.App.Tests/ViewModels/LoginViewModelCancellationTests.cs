using System.DirectoryServices.Protocols;
using System.Net;
using Moq;
using PrinterInstall.App.Models;
using PrinterInstall.App.Services;
using PrinterInstall.App.ViewModels;
using PrinterInstall.Core.Auth;

namespace PrinterInstall.App.Tests.ViewModels;

public class LoginViewModelCancellationTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task CancelLogin_DuringSlowValidation_ReturnsToFormWithoutError()
    {
        var pending = new TaskCompletionSource<LdapValidationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new SessionContext();
        var sut = CreateSut(ValidatorReturning(pending.Task), session);

        var login = sut.TryLoginAsync();
        Assert.True(sut.IsAuthenticating);

        sut.CancelLoginCommand.Execute(null);
        var (success, error) = await login.WaitAsync(TestTimeout);

        Assert.False(success);
        Assert.Null(error);
        Assert.Null(sut.ErrorMessage);
        Assert.False(sut.IsAuthenticating);
        Assert.Null(session.Credential);
    }

    [Fact]
    public async Task CancelLogin_ValidationSucceedsAfterwards_DoesNotSignIn()
    {
        var pending = new TaskCompletionSource<LdapValidationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new SessionContext();
        var remembered = new Mock<IRememberedUserStore>();
        var sut = CreateSut(ValidatorReturning(pending.Task), session, remembered.Object);
        sut.RememberMe = true;

        var login = sut.TryLoginAsync();
        sut.CancelLoginCommand.Execute(null);
        await login.WaitAsync(TestTimeout);

        // A tentativa nativa que já estava em andamento termina depois do cancelamento.
        pending.SetResult(LdapValidationResult.Success());
        await Task.Delay(50);

        Assert.Null(session.Credential);
        remembered.Verify(s => s.Save(It.IsAny<RememberedUser>()), Times.Never);
    }

    [Fact]
    public async Task ClosingTheWindow_DuringValidation_StillThrowsCancellation()
    {
        var pending = new TaskCompletionSource<LdapValidationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sut = CreateSut(ValidatorReturning(pending.Task), new SessionContext());
        using var windowClosed = new CancellationTokenSource();

        var login = sut.TryLoginAsync(windowClosed.Token);
        windowClosed.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => login.WaitAsync(TestTimeout));
        Assert.False(sut.IsAuthenticating);
    }

    [Fact]
    public void CancelLogin_WithoutLoginInProgress_DoesNothing()
    {
        var sut = CreateSut(ValidatorReturning(Task.FromResult(LdapValidationResult.Success())), new SessionContext());

        sut.CancelLoginCommand.Execute(null);

        Assert.False(sut.IsAuthenticating);
        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public async Task Validator_CancelledDuringAnAttempt_DoesNotStartTheNextOne()
    {
        using var inFirstAttempt = new ManualResetEventSlim();
        using var releaseFirstAttempt = new ManualResetEventSlim();
        var attempts = 0;

        LdapValidationResult Attempt(bool blocks)
        {
            Interlocked.Increment(ref attempts);
            if (blocks)
            {
                inFirstAttempt.Set();
                releaseFirstAttempt.Wait(TestTimeout);
            }
            return LdapValidationResult.Failure("Recusado.");
        }

        var validator = new LdapCredentialValidator(
            (_, _, authType) => Attempt(authType == AuthType.Negotiate),
            (_, _) => Attempt(false),
            (_, _) => Attempt(false));
        using var cts = new CancellationTokenSource();

        var validation = validator.ValidateAsync("test.example", new NetworkCredential("operator", "wrong"), cts.Token);
        Assert.True(inFirstAttempt.Wait(TestTimeout));
        cts.Cancel();
        releaseFirstAttempt.Set();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => validation.WaitAsync(TestTimeout));
        Assert.Equal(1, attempts);
    }

    private static ILdapCredentialValidator ValidatorReturning(Task<LdapValidationResult> result)
    {
        var validator = new Mock<ILdapCredentialValidator>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<NetworkCredential>(), It.IsAny<CancellationToken>()))
            .Returns(result);
        return validator.Object;
    }

    private static LoginViewModel CreateSut(
        ILdapCredentialValidator validator, SessionContext session, IRememberedUserStore? remembered = null)
    {
        var settings = new Mock<IAppSettingsStore>();
        settings.Setup(s => s.Load()).Returns(new AppSettings("test.example", "ldap.test.example"));
        return new LoginViewModel(validator, session, settings.Object, remembered ?? new Mock<IRememberedUserStore>().Object)
        {
            UserName = "operator",
            Password = "test-password"
        };
    }
}
