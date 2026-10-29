using PrinterInstall.Core.Models;

namespace PrinterInstall.App.ViewModels;

/// <summary>
/// O que cada estado de uma linha significa para o resumo, a barra de status e o resultado da operação.
/// </summary>
internal enum TargetStateCategory
{
    InProgress,
    Succeeded,
    Skipped,
    Failed,
    Cancelled
}

internal static class TargetStateCategories
{
    /// <summary>Lança para um estado sem categoria, para que um estado novo não passe despercebido.</summary>
    public static TargetStateCategory Of(TargetMachineState state) => state switch
    {
        TargetMachineState.Pending
            or TargetMachineState.IdentifyingPrinter
            or TargetMachineState.ValidatingPrinter
            or TargetMachineState.ContactingRemote
            or TargetMachineState.ValidatingDriver
            or TargetMachineState.InstallingDriver
            or TargetMachineState.DriverInstalledReconfirming
            or TargetMachineState.Configuring
            or TargetMachineState.RollbackRemovingQueue
            or TargetMachineState.RollbackRemovingPort => TargetStateCategory.InProgress,
        // A impressora ficou instalada; o aviso aparece na cor da linha e no resumo, não como falha.
        TargetMachineState.CompletedSuccess
            or TargetMachineState.CompletedWithWarning => TargetStateCategory.Succeeded,
        TargetMachineState.SkippedAlreadyExists => TargetStateCategory.Skipped,
        TargetMachineState.Error
            or TargetMachineState.AbortedDriverMissing
            or TargetMachineState.PrinterIdentityMismatch
            or TargetMachineState.PrinterIdentityUnknown => TargetStateCategory.Failed,
        TargetMachineState.DeployCancelled
            or TargetMachineState.RolledBack => TargetStateCategory.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Estado sem categoria.")
    };
}
