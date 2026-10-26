using Moq;
using PrinterInstall.App.Services;
using PrinterInstall.App.ViewModels;
using PrinterInstall.Core.Models;
using PrinterInstall.Core.Orchestration;
using PrinterInstall.Core.Remote;
using Xunit;

namespace PrinterInstall.App.Tests.ViewModels;

public class MainViewModelStatusSummaryTests
{
    private static MainViewModel CreateViewModel()
    {
        var remote = new Mock<IRemotePrinterOperations>().Object;
        return new MainViewModel(
            new SessionContext(),
            TestOrchestratorFactory.Create(remote),
            new DeploymentRollbackRunner(remote, new PrinterControlOrchestrator(remote)),
            null!,
            new LocalMachineIdentity());
    }

    [Fact]
    public void StatusSummary_FollowsRowStateChanges()
    {
        var vm = CreateViewModel();
        var first = new TargetRowViewModel { ComputerName = "PC1", PrinterQueueName = "A" };
        var second = new TargetRowViewModel { ComputerName = "PC2", PrinterQueueName = "A" };
        vm.Targets.Add(first);
        vm.Targets.Add(second);

        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        first.State = TargetMachineState.CompletedSuccess;
        second.State = TargetMachineState.Error;

        Assert.Contains(nameof(MainViewModel.StatusSucceededCount), changed);
        Assert.Equal(2, vm.StatusTargetCount);
        Assert.Equal(1, vm.StatusSucceededCount);
        Assert.Equal(1, vm.StatusFailedCount);
        Assert.Equal(0, vm.StatusOtherCount);
    }

    [Fact]
    public void StatusSummary_StopsListeningToRemovedRows()
    {
        var vm = CreateViewModel();
        var row = new TargetRowViewModel { ComputerName = "PC1", PrinterQueueName = "A" };
        vm.Targets.Add(row);
        vm.Targets.Remove(row);

        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        row.State = TargetMachineState.CompletedSuccess;

        Assert.Empty(changed);
        Assert.Equal(0, vm.StatusTargetCount);
    }
}
