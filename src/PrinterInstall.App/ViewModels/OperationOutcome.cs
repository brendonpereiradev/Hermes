using PrinterInstall.App.Services;

namespace PrinterInstall.App.ViewModels;

/// <summary>Resultado geral de uma operação em lote, usado para escolher a notificação ao operador.</summary>
internal enum OperationOutcome
{
    None,
    Success,
    Warning,
    Error
}

internal static class OperationOutcomeNotifications
{
    public static void Notify(this IDeploymentNotificationService service, OperationOutcome outcome)
    {
        switch (outcome)
        {
            case OperationOutcome.Success:
                service.NotifySuccess();
                break;
            case OperationOutcome.Warning:
                service.NotifyWarning();
                break;
            case OperationOutcome.Error:
                service.NotifyError();
                break;
        }
    }
}
