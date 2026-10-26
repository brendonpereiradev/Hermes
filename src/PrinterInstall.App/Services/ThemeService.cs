using System.Windows;
using System.Windows.Media;
using PrinterInstall.App.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace PrinterInstall.App.Services;

/// <summary>
/// Implementação do serviço de controle de temas visuais (Claro / Escuro) da aplicação.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private readonly IAppSettingsStore _appSettingsStore;
    private ApplicationTheme _currentTheme;

    public ThemeService(IAppSettingsStore appSettingsStore)
    {
        _appSettingsStore = appSettingsStore;
        var savedTheme = _appSettingsStore.Load().Theme;
        _currentTheme = string.Equals(savedTheme, "Dark", StringComparison.OrdinalIgnoreCase)
            ? ApplicationTheme.Dark
            : ApplicationTheme.Light;
    }

    public ApplicationTheme CurrentTheme => _currentTheme;

    public bool IsDarkMode => _currentTheme == ApplicationTheme.Dark;

    public event EventHandler<ApplicationTheme>? ThemeChanged;

    /// <summary>
    /// Inicializa e aplica o tema salvo na inicialização do aplicativo.
    /// </summary>
    public void Initialize()
    {
        ApplyThemeToApplication(_currentTheme);
    }

    public void SetTheme(ApplicationTheme theme, bool persist = true)
    {
        _currentTheme = theme;
        ApplyThemeToApplication(theme);

        if (persist)
        {
            try
            {
                var settings = _appSettingsStore.Load();
                var updated = settings with { Theme = theme == ApplicationTheme.Dark ? "Dark" : "Light" };
                _appSettingsStore.Save(updated);
            }
            catch
            {
                // Falha de persistência não interrompe a alteração visual
            }
        }

        ThemeChanged?.Invoke(this, theme);
    }

    public void ToggleTheme()
    {
        var next = IsDarkMode ? ApplicationTheme.Light : ApplicationTheme.Dark;
        SetTheme(next, persist: true);
    }

    private static void ApplyThemeToApplication(ApplicationTheme theme)
    {
        Converters.TargetMachineStateToBrushConverter.IsDarkTheme = (theme == ApplicationTheme.Dark);

        try
        {
            if (Application.Current is null)
                return;

            void Action()
            {
                // Fundo sólido, sem Mica: as superfícies seguem a paleta do Talos/Hefesto.
                ApplicationThemeManager.Apply(theme, WindowBackdropType.None);

                // Acento sem as variações automáticas do WPF UI, que clareiam o azul no tema escuro.
                var (accent, accentText) = theme == ApplicationTheme.Dark
                    ? (Color.FromRgb(0x4C, 0x8D, 0xF6), Color.FromRgb(0x8D, 0xB5, 0xFA))
                    : (Color.FromRgb(0x2F, 0x6F, 0xE0), Color.FromRgb(0x21, 0x55, 0xB3));

                ApplicationAccentColorManager.Apply(accent, accent, accent, accentText);
                UpdateThemeTokens(theme);

                // Janelas já abertas precisam reaplicar o fundo e a barra de título no novo tema
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is FluentWindow)
                    {
                        WindowBackgroundManager.UpdateBackground(window, theme, WindowBackdropType.None);
                    }

                    // O WPF UI volta a pintar o cinza do próprio tema; o fundo retorna à paleta.
                    if (window is HermesWindow hermesWindow)
                    {
                        hermesWindow.UseCanvasBackground();
                    }
                }
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                Action();
            }
            else
            {
                Application.Current.Dispatcher.Invoke(Action);
            }
        }
        catch
        {
            // Em testes unitários sem runtime gráfico completo, ignora falha de aplicação de recurso de UI
        }
    }

    private static void UpdateThemeTokens(ApplicationTheme theme)
    {
        try
        {
            var targetSourceName = theme == ApplicationTheme.Dark
                ? "ThemeTokens.Dark.xaml"
                : "ThemeTokens.Light.xaml";

            var merged = Application.Current.Resources.MergedDictionaries;
            var existing = merged.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("ThemeTokens."));

            var newUri = new Uri($"pack://application:,,,/PrinterInstall.App;component/Themes/{targetSourceName}", UriKind.RelativeOrAbsolute);
            var newDict = new ResourceDictionary { Source = newUri };

            if (existing != null)
            {
                merged.Remove(existing);
            }

            // Garante que os tokens semânticos fiquem sempre no topo da cadeia de precedência
            merged.Add(newDict);
        }
        catch
        {
            // Fallback seguro
        }
    }
}
