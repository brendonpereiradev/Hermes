namespace PrinterInstall.App.ViewModels;

/// <summary>Cores e ícone do selo de conectividade do assistente de controle, por estado e tema.</summary>
internal static class PingBadgePalette
{
    public static string Background(ComputerPingStatus status, bool dark) => (status, dark) switch
    {
        (ComputerPingStatus.Checking, true) => "#FF291C0E",
        (ComputerPingStatus.Checking, false) => "#FFFFF4E5",
        (ComputerPingStatus.Online, true) => "#FF0D2818",
        (ComputerPingStatus.Online, false) => "#FFE7F6EC",
        (ComputerPingStatus.Offline, true) => "#FF2D1214",
        (ComputerPingStatus.Offline, false) => "#FFFCE8E8",
        _ => "Transparent"
    };

    public static string Border(ComputerPingStatus status, bool dark) => (status, dark) switch
    {
        (ComputerPingStatus.Checking, true) => "#FF92400E",
        (ComputerPingStatus.Checking, false) => "#FFE2BD7A",
        (ComputerPingStatus.Online, true) => "#FF166534",
        (ComputerPingStatus.Online, false) => "#FF87C79B",
        (ComputerPingStatus.Offline, true) => "#FF991B1B",
        (ComputerPingStatus.Offline, false) => "#FFE59A9A",
        _ => "Transparent"
    };

    public static string Foreground(ComputerPingStatus status, bool dark) => (status, dark) switch
    {
        (ComputerPingStatus.Checking, true) => "#FFFBBF24",
        (ComputerPingStatus.Checking, false) => "#FF8A5A00",
        (ComputerPingStatus.Online, true) => "#FF4ADE80",
        (ComputerPingStatus.Online, false) => "#FF1F6B35",
        (ComputerPingStatus.Offline, true) => "#FFF87171",
        (ComputerPingStatus.Offline, false) => "#FF8C1D1D",
        _ => "#FFA1A1AA"
    };

    public static string Icon(ComputerPingStatus status) => status switch
    {
        ComputerPingStatus.Checking => "",
        ComputerPingStatus.Online => "",
        ComputerPingStatus.Offline => "",
        _ => ""
    };

    public static string IconForeground(ComputerPingStatus status, bool dark) => (status, dark) switch
    {
        (ComputerPingStatus.Checking, true) => "#FFFBBF24",
        (ComputerPingStatus.Checking, false) => "#FF8A5A00",
        (ComputerPingStatus.Online, true) => "#FF4ADE80",
        (ComputerPingStatus.Online, false) => "#FF1F6B35",
        (ComputerPingStatus.Offline, true) => "#FFF87171",
        (ComputerPingStatus.Offline, false) => "#FFDC2626",
        _ => "Transparent"
    };
}
