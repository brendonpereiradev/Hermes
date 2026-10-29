namespace PrinterInstall.Core.Models;

public enum TargetMachineState
{
    Pending,
    IdentifyingPrinter,
    ValidatingPrinter,
    PrinterIdentityMismatch,
    PrinterIdentityUnknown,
    ContactingRemote,
    ValidatingDriver,
    InstallingDriver,
    DriverInstalledReconfirming,
    Configuring,
    CompletedSuccess,
    /// <summary>Impressora instalada, mas uma etapa opcional (a página de teste) falhou.</summary>
    CompletedWithWarning,
    SkippedAlreadyExists,
    AbortedDriverMissing,
    Error,
    DeployCancelled,
    RollbackRemovingQueue,
    RollbackRemovingPort,
    RolledBack
}
