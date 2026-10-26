using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace PrinterInstall.App.Controls;

/// <summary>
/// Janela base do app. Sem Mica, o WPF UI pinta a janela com o cinza neutro do próprio tema
/// (#202020 / #FAFAFA) em <see cref="OnSourceInitialized"/>; aqui o fundo volta para a paleta.
/// </summary>
public class HermesWindow : FluentWindow
{
    public HermesWindow()
    {
        // Pixels inteiros em todas as janelas: sem isso, a marca da barra de título cai em meio pixel
        // e o nome e o subtítulo ficam 1 px fora da mesma linha de base, dependendo da janela.
        UseLayoutRounding = true;
        Loaded += (_, _) => AlignCaptionButtons();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        UseCanvasBackground();
    }

    /// <summary>Liga o fundo ao recurso da paleta, que acompanha a troca de tema.</summary>
    public void UseCanvasBackground() =>
        SetResourceReference(BackgroundProperty, "AppCanvasBackgroundBrush");

    /// <summary>
    /// A barra de título do WPF UI tem 48 px, mas os botões de minimizar, maximizar e fechar vêm com
    /// 30 px presos no topo, acima da linha da marca e das ferramentas. Só a altura muda; o estilo
    /// deles continua o do WPF UI.
    /// </summary>
    private void AlignCaptionButtons()
    {
        foreach (var titleBar in Descendants<TitleBar>(this))
        {
            var height = double.IsNaN(titleBar.Height) ? titleBar.ActualHeight : titleBar.Height;
            foreach (var button in Descendants<TitleBarButton>(titleBar))
                button.Height = height;
        }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                yield return match;
            foreach (var descendant in Descendants<T>(child))
                yield return descendant;
        }
    }
}
