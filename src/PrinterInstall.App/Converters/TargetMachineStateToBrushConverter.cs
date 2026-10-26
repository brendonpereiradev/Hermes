using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using PrinterInstall.Core.Models;

namespace PrinterInstall.App.Converters;

/// <summary>
/// Cor de cada estado da implantação. O parâmetro escolhe o papel: "Dot" (ponto de status),
/// "Foreground" (texto), "Border" ou fundo (padrão), nos tons do Talos (escuro) e do Hefesto (claro).
/// </summary>
public sealed class TargetMachineStateToBrushConverter : IValueConverter
{
    public static bool IsDarkTheme { get; set; }

    private enum Tone
    {
        Pending,
        Active,
        Success,
        Warning,
        Error,
        RolledBack
    }

    private sealed record Palette(SolidColorBrush Background, SolidColorBrush Border, SolidColorBrush Foreground, SolidColorBrush Dot);

    private static readonly Dictionary<Tone, Palette> LightPalettes = new()
    {
        [Tone.Pending] = CreatePalette("#FFF1F4F8", "#FFC9D2DE", "#FF465569", "#FF9AA7B8"),
        [Tone.Active] = CreatePalette("#FFEBF2FD", "#FFA9C3F2", "#FF2155B3", "#FF2F6FE0"),
        [Tone.Success] = CreatePalette("#FFEAF6EF", "#FFA6D4B6", "#FF1F8A50", "#FF22A35E"),
        [Tone.Warning] = CreatePalette("#FFFCF5E8", "#FFE3C584", "#FF8F5410", "#FFD08A1E"),
        [Tone.Error] = CreatePalette("#FFFCEEEE", "#FFEBB2B2", "#FFC23636", "#FFD94848"),
        [Tone.RolledBack] = CreatePalette("#FFE8F4F2", "#FF9ACFC7", "#FF16736B", "#FF1A9A8E")
    };

    private static readonly Dictionary<Tone, Palette> DarkPalettes = new()
    {
        [Tone.Pending] = CreatePalette("#FF181C23", "#FF333B47", "#FF98A2B0", "#FF7A8494"),
        [Tone.Active] = CreatePalette("#FF15213A", "#FF2E4A73", "#FF8DB5FA", "#FF4C8DF6"),
        [Tone.Success] = CreatePalette("#FF132620", "#FF225C44", "#FF3DD68C", "#FF3DD68C"),
        [Tone.Warning] = CreatePalette("#FF282418", "#FF6E5A26", "#FFF2C14E", "#FFF2C14E"),
        [Tone.Error] = CreatePalette("#FF2A181C", "#FF7A3337", "#FFFF9A97", "#FFF0605D"),
        [Tone.RolledBack] = CreatePalette("#FF0E2524", "#FF115E59", "#FF2DD4BF", "#FF2DD4BF")
    };

    private static bool ResolveIsDarkTheme()
    {
        try
        {
            return IsDarkTheme || Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme() == Wpf.Ui.Appearance.ApplicationTheme.Dark;
        }
        catch
        {
            return IsDarkTheme;
        }
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var state = value is TargetMachineState s ? s : TargetMachineState.Pending;
        var palette = (ResolveIsDarkTheme() ? DarkPalettes : LightPalettes)[ToneOf(state)];

        return parameter?.ToString() switch
        {
            "Border" => palette.Border,
            "Foreground" => palette.Foreground,
            "Dot" => palette.Dot,
            _ => palette.Background
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Tone ToneOf(TargetMachineState state) => state switch
    {
        TargetMachineState.CompletedSuccess => Tone.Success,
        TargetMachineState.SkippedAlreadyExists => Tone.Warning,
        TargetMachineState.DeployCancelled => Tone.Warning,
        TargetMachineState.AbortedDriverMissing => Tone.Error,
        TargetMachineState.Error => Tone.Error,
        TargetMachineState.PrinterIdentityMismatch => Tone.Error,
        TargetMachineState.PrinterIdentityUnknown => Tone.Error,
        TargetMachineState.IdentifyingPrinter => Tone.Active,
        TargetMachineState.ValidatingPrinter => Tone.Active,
        TargetMachineState.ContactingRemote => Tone.Active,
        TargetMachineState.ValidatingDriver => Tone.Active,
        TargetMachineState.InstallingDriver => Tone.Active,
        TargetMachineState.DriverInstalledReconfirming => Tone.Active,
        TargetMachineState.Configuring => Tone.Active,
        TargetMachineState.RollbackRemovingQueue => Tone.Active,
        TargetMachineState.RollbackRemovingPort => Tone.Active,
        TargetMachineState.RolledBack => Tone.RolledBack,
        _ => Tone.Pending
    };

    private static Palette CreatePalette(string background, string border, string foreground, string dot) =>
        new(CreateBrush(background), CreateBrush(border), CreateBrush(foreground), CreateBrush(dot));

    private static SolidColorBrush CreateBrush(string colorHex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
        brush.Freeze();
        return brush;
    }
}
