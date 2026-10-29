using System.Text;
using PrinterInstall.App.Resources;
using PrinterInstall.Core.Models;
using PrinterInstall.Core.Orchestration;

namespace PrinterInstall.App.ViewModels;

/// <summary>
/// Regras de estado das linhas de destino da implantação: progresso, cancelamento, reversão e resumo final.
/// </summary>
internal static class DeploymentTargetRows
{
    /// <summary>Etapa da implantação ainda em andamento. A reversão tem fechamento próprio.</summary>
    public static bool IsIntermediateState(TargetMachineState s) =>
        TargetStateCategories.Of(s) == TargetStateCategory.InProgress
        && s is not (TargetMachineState.RollbackRemovingQueue or TargetMachineState.RollbackRemovingPort);

    private static bool IsFailureState(TargetMachineState s) =>
        TargetStateCategories.Of(s) == TargetStateCategory.Failed;

    /// <summary>
    /// Linhas que terminaram com sucesso, com falha ou sem nenhum dos dois ("Já existe", "Cancelado",
    /// "Revertido"), para o resumo do painel de status. O resto ainda está em andamento.
    /// </summary>
    public static (int Succeeded, int Failed, int Neutral) CountOutcomes(IEnumerable<TargetRowViewModel> targets)
    {
        var succeeded = 0;
        var failed = 0;
        var neutral = 0;
        foreach (var row in targets)
        {
            switch (TargetStateCategories.Of(row.State))
            {
                case TargetStateCategory.Succeeded: succeeded++; break;
                case TargetStateCategory.Failed: failed++; break;
                case TargetStateCategory.Skipped or TargetStateCategory.Cancelled: neutral++; break;
            }
        }
        return (succeeded, failed, neutral);
    }

    /// <summary>Um evento sem fila se aplica a todas as linhas do computador.</summary>
    public static void ApplyProgress(IEnumerable<TargetRowViewModel> targets, DeploymentProgressEvent e)
    {
        var rows = e.PrinterQueueName is null
            ? targets.Where(t => t.ComputerName == e.ComputerName)
            : targets.Where(t => t.ComputerName == e.ComputerName && t.PrinterQueueName == e.PrinterQueueName).Take(1);
        foreach (var row in rows)
        {
            row.State = e.State;
            row.Message = e.Message;
        }
    }

    public static void MarkIntermediateAsCancelled(IEnumerable<TargetRowViewModel> targets)
    {
        foreach (var row in targets.Where(r => IsIntermediateState(r.State)))
        {
            row.State = TargetMachineState.DeployCancelled;
            row.Message = UiStrings.Main_DeployCancelledRowMessage;
        }
    }

    /// <summary>Fecha as linhas que ficaram em reversão ou em andamento depois de um cancelamento.</summary>
    public static void FinalizeAfterRollback(IEnumerable<TargetRowViewModel> targets)
    {
        foreach (var row in targets)
        {
            if (row.State is TargetMachineState.RollbackRemovingQueue or TargetMachineState.RollbackRemovingPort)
            {
                row.State = TargetMachineState.RolledBack;
            }
            else if (IsIntermediateState(row.State))
            {
                row.State = TargetMachineState.DeployCancelled;
                row.Message = UiStrings.Main_DeployCancelledRowMessage;
            }
        }
    }

    public static void ApplyRollbackProgress(
        IReadOnlyCollection<TargetRowViewModel> targets, PrinterRemovalProgressEvent e, DeploymentRollbackJournal journal)
    {
        switch (e.State)
        {
            case PrinterRemovalProgressState.ContactingRemote:
                foreach (var row in targets.Where(t =>
                             string.Equals(t.ComputerName, e.ComputerName, StringComparison.OrdinalIgnoreCase)
                             && JournalTouchesRow(journal, t)))
                {
                    row.State = TargetMachineState.RollbackRemovingQueue;
                    row.Message = UiStrings.Main_RollbackPreparingOnHost;
                }
                break;
            case PrinterRemovalProgressState.RemovingQueue:
                SetState(FindByQueue(targets, e.ComputerName, e.PrinterQueueName), TargetMachineState.RollbackRemovingQueue, e.Message);
                break;
            case PrinterRemovalProgressState.RemovingOrphanPort:
                SetState(ResolveRollbackRow(targets, e), TargetMachineState.RollbackRemovingPort, e.Message);
                break;
            case PrinterRemovalProgressState.RollbackSucceeded:
                SetState(ResolveRollbackRow(targets, e), TargetMachineState.RolledBack, e.Message);
                break;
            case PrinterRemovalProgressState.Warning:
            case PrinterRemovalProgressState.Error:
                SetState(ResolveRollbackRow(targets, e), TargetMachineState.Error, e.Message);
                break;
        }
    }

    public static string BuildSummaryText(IReadOnlyCollection<TargetRowViewModel> targets)
    {
        if (targets.Count == 0)
            return string.Empty;

        var ok = 0;
        var skipped = 0;
        var err = 0;
        var aborted = 0;
        var rolledBack = 0;
        var deployCancelled = 0;
        var other = 0;
        foreach (var t in targets)
        {
            switch (t.State)
            {
                case TargetMachineState.CompletedSuccess: ok++; break;
                case TargetMachineState.SkippedAlreadyExists: skipped++; break;
                case TargetMachineState.Error: err++; break;
                case TargetMachineState.PrinterIdentityMismatch: err++; break;
                case TargetMachineState.PrinterIdentityUnknown: err++; break;
                case TargetMachineState.AbortedDriverMissing: aborted++; break;
                case TargetMachineState.RolledBack: rolledBack++; break;
                case TargetMachineState.DeployCancelled: deployCancelled++; break;
                default: other++; break;
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine(string.Format(UiStrings.Main_SummaryLineFormat, ok, skipped, err, aborted));
        if (other > 0)
            sb.AppendLine(string.Format(UiStrings.Main_SummaryOtherFormat, other));
        if (rolledBack > 0)
            sb.AppendLine(string.Format(UiStrings.Main_SummaryRolledBackFormat, rolledBack));
        if (deployCancelled > 0)
            sb.AppendLine(string.Format(UiStrings.Main_SummaryDeployCancelledFormat, deployCancelled));

        if (err > 0 || aborted > 0)
        {
            foreach (var t in targets.Where(x => IsFailureState(x.State)))
            {
                sb.AppendLine(
                    string.Format(UiStrings.Main_SummaryFailureLineFormat, t.ComputerName, t.PrinterQueueName, t.Message));
            }
        }

        return sb.ToString();
    }

    public static OperationOutcome GetOutcome(IReadOnlyCollection<TargetRowViewModel> targets)
    {
        if (targets.Count == 0)
            return OperationOutcome.None;

        // "Já existe" conta como sucesso aqui: a impressora está no computador, que é o que o técnico quer saber.
        var categories = targets.Select(t => TargetStateCategories.Of(t.State)).ToList();
        var successCount = categories.Count(c => c is TargetStateCategory.Succeeded or TargetStateCategory.Skipped);
        var errorCount = categories.Count(c => c == TargetStateCategory.Failed);
        var cancelCount = categories.Count(c => c == TargetStateCategory.Cancelled);

        if (cancelCount > 0)
            return OperationOutcome.Warning;
        if (errorCount == 0 && successCount > 0)
            return OperationOutcome.Success;
        if (successCount == 0 && errorCount > 0)
            return OperationOutcome.Error;
        return OperationOutcome.Warning;
    }

    private static void SetState(TargetRowViewModel? row, TargetMachineState state, string message)
    {
        if (row is null)
            return;
        row.State = state;
        row.Message = message;
    }

    private static bool JournalTouchesRow(DeploymentRollbackJournal journal, TargetRowViewModel row) =>
        journal.QueueEntries.Any(q => string.Equals(q.ComputerName, row.ComputerName, StringComparison.OrdinalIgnoreCase)
                                     && string.Equals(q.PrinterName, row.PrinterQueueName, StringComparison.OrdinalIgnoreCase))
        || journal.PortOnlyEntries.Any(p => string.Equals(p.Computer, row.ComputerName, StringComparison.OrdinalIgnoreCase)
                                            && string.Equals(p.PortName, row.ExpectedPortName, StringComparison.OrdinalIgnoreCase));

    private static TargetRowViewModel? FindByQueue(IEnumerable<TargetRowViewModel> targets, string computer, string? printerQueue)
    {
        if (string.IsNullOrEmpty(printerQueue))
            return null;
        return targets.FirstOrDefault(t =>
            string.Equals(t.ComputerName, computer, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.PrinterQueueName, printerQueue, StringComparison.OrdinalIgnoreCase));
    }

    private static TargetRowViewModel? FindByPort(IEnumerable<TargetRowViewModel> targets, string computer, string? portName)
    {
        if (string.IsNullOrWhiteSpace(portName))
            return null;
        return targets.FirstOrDefault(t =>
            string.Equals(t.ComputerName, computer, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.ExpectedPortName, portName, StringComparison.OrdinalIgnoreCase));
    }

    private static TargetRowViewModel? ResolveRollbackRow(IEnumerable<TargetRowViewModel> targets, PrinterRemovalProgressEvent e) =>
        FindByQueue(targets, e.ComputerName, e.PrinterQueueName) ?? FindByPort(targets, e.ComputerName, e.PortName);
}
