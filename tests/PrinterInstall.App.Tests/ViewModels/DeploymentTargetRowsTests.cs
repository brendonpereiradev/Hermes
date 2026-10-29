using PrinterInstall.App.ViewModels;
using PrinterInstall.Core.Models;
using PrinterInstall.Core.Orchestration;

namespace PrinterInstall.App.Tests.ViewModels;

public class DeploymentTargetRowsTests
{
    private static TargetRowViewModel Row(string computer, string queue, TargetMachineState state = TargetMachineState.Pending) =>
        new() { ComputerName = computer, PrinterQueueName = queue, ExpectedPortName = $"IP_{queue}", State = state };

    [Fact]
    public void CountOutcomes_SeparatesSuccessFailuresAndNeutralRows()
    {
        var rows = new[]
        {
            Row("PC1", "A", TargetMachineState.CompletedSuccess),
            Row("PC2", "A", TargetMachineState.Error),
            Row("PC3", "A", TargetMachineState.PrinterIdentityMismatch),
            Row("PC4", "A", TargetMachineState.Configuring),
            Row("PC5", "A", TargetMachineState.SkippedAlreadyExists),
            Row("PC6", "A", TargetMachineState.DeployCancelled),
            Row("PC7", "A", TargetMachineState.RolledBack),
            Row("PC8", "A", TargetMachineState.RollbackRemovingQueue)
        };

        var (succeeded, failed, neutral) = DeploymentTargetRows.CountOutcomes(rows);

        Assert.Equal(1, succeeded);
        Assert.Equal(2, failed);
        Assert.Equal(3, neutral);
    }

    [Fact]
    public void ApplyProgress_WithoutQueue_UpdatesEveryRowOfTheComputer()
    {
        var rows = new[] { Row("PC1", "A"), Row("PC1", "B"), Row("PC2", "A") };

        DeploymentTargetRows.ApplyProgress(rows, new DeploymentProgressEvent("PC1", TargetMachineState.ContactingRemote, "conectando"));

        Assert.All(rows[..2], r => Assert.Equal(TargetMachineState.ContactingRemote, r.State));
        Assert.Equal(TargetMachineState.Pending, rows[2].State);
    }

    [Fact]
    public void ApplyProgress_WithQueue_UpdatesOnlyThatRow()
    {
        var rows = new[] { Row("PC1", "A"), Row("PC1", "B") };

        DeploymentTargetRows.ApplyProgress(rows, new DeploymentProgressEvent("PC1", TargetMachineState.CompletedSuccess, "ok", "B"));

        Assert.Equal(TargetMachineState.Pending, rows[0].State);
        Assert.Equal(TargetMachineState.CompletedSuccess, rows[1].State);
        Assert.Equal("ok", rows[1].Message);
    }

    [Fact]
    public void RollbackProgress_FallsBackToPortWhenQueueIsUnknown()
    {
        var rows = new[] { Row("PC1", "A"), Row("PC1", "B") };

        DeploymentTargetRows.ApplyRollbackProgress(rows,
            new PrinterRemovalProgressEvent("pc1", PrinterRemovalProgressState.RollbackSucceeded, "porta removida", PortName: "IP_B"),
            new DeploymentRollbackJournal());

        Assert.Equal(TargetMachineState.Pending, rows[0].State);
        Assert.Equal(TargetMachineState.RolledBack, rows[1].State);
    }

    [Fact]
    public void FinalizeAfterRollback_ClosesRollbackAndInProgressRows()
    {
        var rows = new[]
        {
            Row("PC1", "A", TargetMachineState.RollbackRemovingPort),
            Row("PC1", "B", TargetMachineState.Configuring),
            Row("PC1", "C", TargetMachineState.CompletedSuccess)
        };

        DeploymentTargetRows.FinalizeAfterRollback(rows);

        Assert.Equal(new[] { TargetMachineState.RolledBack, TargetMachineState.DeployCancelled, TargetMachineState.CompletedSuccess },
            rows.Select(r => r.State));
    }

    [Theory]
    [InlineData(new TargetMachineState[0], OperationOutcome.None)]
    [InlineData(new[] { TargetMachineState.CompletedSuccess, TargetMachineState.SkippedAlreadyExists }, OperationOutcome.Success)]
    [InlineData(new[] { TargetMachineState.Error, TargetMachineState.PrinterIdentityUnknown }, OperationOutcome.Error)]
    [InlineData(new[] { TargetMachineState.CompletedSuccess, TargetMachineState.Error }, OperationOutcome.Warning)]
    [InlineData(new[] { TargetMachineState.CompletedSuccess, TargetMachineState.RolledBack }, OperationOutcome.Warning)]
    internal void GetOutcome_ReflectsRowStates(TargetMachineState[] states, OperationOutcome expected)
    {
        var rows = states.Select((s, i) => Row("PC1", $"Q{i}", s)).ToArray();

        Assert.Equal(expected, DeploymentTargetRows.GetOutcome(rows));
    }

    [Fact]
    public void BuildSummaryText_ListsFailures()
    {
        var rows = new[] { Row("PC1", "A", TargetMachineState.CompletedSuccess), Row("PC2", "B", TargetMachineState.Error) };
        rows[1].Message = "acesso negado";

        var summary = DeploymentTargetRows.BuildSummaryText(rows);

        Assert.Contains("PC2", summary);
        Assert.Contains("acesso negado", summary);
        Assert.Equal(string.Empty, DeploymentTargetRows.BuildSummaryText(Array.Empty<TargetRowViewModel>()));
    }
}
