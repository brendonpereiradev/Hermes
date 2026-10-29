namespace PrinterInstall.App.Controls;

/// <summary>
/// Frações da barra de status (0 a 1): sucesso, falha, neutro (terminou sem sucesso nem falha,
/// como "Já existe" ou "Cancelado") e o que ainda está em andamento.
/// Contagens fora do total são limitadas nessa ordem, e a soma nunca passa de 1.
/// </summary>
public readonly record struct StatusBarSegments(double Success, double Failure, double Pending, double Neutral = 0)
{
    public static StatusBarSegments Compute(int succeeded, int failed, int total, int neutral = 0)
    {
        if (total <= 0)
            return default;

        var success = Math.Clamp(succeeded, 0, total);
        var failure = Math.Clamp(failed, 0, total - success);
        var finished = Math.Clamp(neutral, 0, total - success - failure);
        return new StatusBarSegments(
            (double)success / total,
            (double)failure / total,
            (double)(total - success - failure - finished) / total,
            (double)finished / total);
    }
}
