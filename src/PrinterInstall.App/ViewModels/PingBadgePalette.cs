namespace PrinterInstall.App.ViewModels;

/// <summary>
/// Cores do indicador de conectividade do assistente de controle (ponto + texto), por estado e tema.
/// Os tons são os mesmos dos estados da tabela de status (TargetMachineStateToBrushConverter).
/// </summary>
internal static class PingBadgePalette
{
    public static string Foreground(ComputerPingStatus status, bool dark) => (status, dark) switch
    {
        (ComputerPingStatus.Checking, true) => "#FFF2C14E",
        (ComputerPingStatus.Checking, false) => "#FF8F5410",
        (ComputerPingStatus.Online, true) => "#FF3DD68C",
        (ComputerPingStatus.Online, false) => "#FF1F8A50",
        (ComputerPingStatus.Offline, true) => "#FFFF9A97",
        (ComputerPingStatus.Offline, false) => "#FFC23636",
        _ => "#FF98A2B0"
    };

    public static string Dot(ComputerPingStatus status, bool dark) => (status, dark) switch
    {
        (ComputerPingStatus.Checking, true) => "#FFF2C14E",
        (ComputerPingStatus.Checking, false) => "#FFD08A1E",
        (ComputerPingStatus.Online, true) => "#FF3DD68C",
        (ComputerPingStatus.Online, false) => "#FF22A35E",
        (ComputerPingStatus.Offline, true) => "#FFF0605D",
        (ComputerPingStatus.Offline, false) => "#FFD94848",
        _ => "Transparent"
    };
}
