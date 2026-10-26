using Wpf.Ui.Controls;

namespace PrinterInstall.App.Controls;

/// <summary>
/// Janela base do app. Sem Mica, o WPF UI pinta a janela com o cinza neutro do próprio tema
/// (#202020 / #FAFAFA) em <see cref="OnSourceInitialized"/>; aqui o fundo volta para a paleta.
/// </summary>
public class HermesWindow : FluentWindow
{
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        UseCanvasBackground();
    }

    /// <summary>Liga o fundo ao recurso da paleta, que acompanha a troca de tema.</summary>
    public void UseCanvasBackground() =>
        SetResourceReference(BackgroundProperty, "AppCanvasBackgroundBrush");
}
