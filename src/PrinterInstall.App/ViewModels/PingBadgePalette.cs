namespace PrinterInstall.App.ViewModels;

/// <summary>Cores e ícone do selo de conectividade do assistente de controle, por estado e tema.</summary>
internal static class PingBadgePalette
{
    public static string Background(ComputerPingStatus status, bool dark) => (status, dark) switch
    {
        (ComputerPingStatus.Checking, true) => "#FF282418",
        (ComputerPingStatus.Checking, false) => "#FFFCF5E8",
        (ComputerPingStatus.Online, true) => "#FF132620",
        (ComputerPingStatus.Online, false) => "#FFEAF6EF",
        (ComputerPingStatus.Offline, true) => "#FF2A181C",
        (ComputerPingStatus.Offline, false) => "#FFFCEEEE",
        _ => "Transparent"
    };

    public static string Border(ComputerPingStatus status, bool dark) => (status, dark) switch
    {
        (ComputerPingStatus.Checking, true) => "#FF6E5A26",
        (ComputerPingStatus.Checking, false) => "#FFE3C584",
        (ComputerPingStatus.Online, true) => "#FF225C44",
        (ComputerPingStatus.Online, false) => "#FFA6D4B6",
        (ComputerPingStatus.Offline, true) => "#FF7A3337",
        (ComputerPingStatus.Offline, false) => "#FFEBB2B2",
        _ => "Transparent"
    };

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

    public static string Icon(ComputerPingStatus status) => status switch
    {
        ComputerPingStatus.Checking => "",
        ComputerPingStatus.Online => "",
        ComputerPingStatus.Offline => "",
        _ => ""
    };

    public static string IconForeground(ComputerPingStatus status, bool dark) => (status, dark) switch
    {
        (ComputerPingStatus.Checking, true) => "#FFF2C14E",
        (ComputerPingStatus.Checking, false) => "#FF8F5410",
        (ComputerPingStatus.Online, true) => "#FF3DD68C",
        (ComputerPingStatus.Online, false) => "#FF1F8A50",
        (ComputerPingStatus.Offline, true) => "#FFF0605D",
        (ComputerPingStatus.Offline, false) => "#FFD94848",
        _ => "Transparent"
    };
}
