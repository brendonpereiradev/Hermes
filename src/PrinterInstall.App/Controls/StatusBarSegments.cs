namespace PrinterInstall.App.Controls;

/// <summary>
/// Frações da barra de status (0 a 1): sucesso, falha e o que ainda não tem um desses resultados.
/// Contagens fora do total são limitadas, e a soma nunca passa de 1.
/// </summary>
public readonly record struct StatusBarSegments(double Success, double Failure, double Pending)
{
    public static StatusBarSegments Compute(int succeeded, int failed, int total)
    {
        if (total <= 0)
            return default;

        var success = Math.Clamp(succeeded, 0, total);
        var failure = Math.Clamp(failed, 0, total - success);
        return new StatusBarSegments(
            (double)success / total,
            (double)failure / total,
            (double)(total - success - failure) / total);
    }
}
