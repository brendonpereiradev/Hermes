using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
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
        _captionButtons.Clear();
        foreach (var titleBar in Descendants<TitleBar>(this))
        {
            var height = double.IsNaN(titleBar.Height) ? titleBar.ActualHeight : titleBar.Height;
            foreach (var button in Descendants<TitleBarButton>(titleBar))
            {
                button.Height = height;
                _captionButtons.Add(button);
            }
        }
    }

    protected override void OnContentRendered(EventArgs e)
    {
        // O TitleBar registra o hook dele neste evento; registrado depois, este roda antes do dele.
        base.OnContentRendered(e);
        HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(CaptionButtonEdgeHook);
    }

    /// <summary>
    /// O WPF UI só reconhece o clique dentro do retângulo do botão. A linha de 1 px da moldura acima
    /// dele (e o canto de cima à direita) aparece pintada junto com o botão, mas responde como borda
    /// de redimensionar, então o clique ali some. Aqui esses pixels são levados para dentro do botão
    /// antes de o TitleBar tratar a mensagem. O segundo clique rápido em um botão chega como
    /// duplo clique, que o WPF UI ignora; ele vira um clique comum.
    /// <para>
    /// O Windows entrega a posição do mouse sem extensão de sinal. Num monitor acima do principal
    /// (Y negativo) o valor passa de <see cref="int.MaxValue"/>, o <c>ToInt32</c> do WPF UI estoura e os
    /// botões da barra deixam de acender e de clicar. A posição é reenviada com o sinal estendido.
    /// </para>
    /// </summary>
    private IntPtr CaptionButtonEdgeHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg is not (WM_NCHITTEST or WM_NCLBUTTONDOWN or WM_NCLBUTTONUP or WM_NCLBUTTONDBLCLK))
            return IntPtr.Zero;

        var raw = lParam.ToInt64();
        var x = (int)(short)(raw & 0xFFFF);
        var y = (int)(short)((raw >> 16) & 0xFFFF);
        var moved = TryMoveOntoCaptionButton(hwnd, ref x, ref y);
        var isButtonDoubleClick = msg == WM_NCLBUTTONDBLCLK && wParam.ToInt64() is HTMINBUTTON or HTMAXBUTTON or HTCLOSE;
        var needsSignExtension = raw is > int.MaxValue or < int.MinValue;
        if (!moved && !isButtonDoubleClick && !needsSignExtension)
            return IntPtr.Zero;

        handled = true;
        var signExtendedLParam = (IntPtr)((y << 16) | (x & 0xFFFF));
        return SendMessage(hwnd, isButtonDoubleClick ? WM_NCLBUTTONDOWN : msg, wParam, signExtendedLParam);
    }

    private bool TryMoveOntoCaptionButton(IntPtr hwnd, ref int x, ref int y)
    {
        if (!GetWindowRect(hwnd, out var window))
            return false;

        foreach (var button in _captionButtons)
        {
            if (!button.IsVisible || button.ActualWidth <= 0 || button.ActualHeight <= 0)
                continue;

            var topLeft = button.PointToScreen(new Point(0, 0));
            var bottomRight = button.PointToScreen(new Point(button.ActualWidth, button.ActualHeight));
            var left = (int)Math.Ceiling(topLeft.X);
            var top = (int)Math.Ceiling(topLeft.Y);
            var right = (int)Math.Floor(bottomRight.X);
            var bottom = (int)Math.Floor(bottomRight.Y);

            // A faixa entre o botão e a borda da janela conta como o próprio botão.
            var reachTop = top - window.Top <= CaptionEdgeReach ? window.Top : top;
            var reachRight = window.Right - right <= CaptionEdgeReach ? window.Right : right;
            if (x < left || x >= reachRight || y < reachTop || y >= bottom)
                continue;
            if (x < right && y >= top)
                return false;

            x = Math.Min(x, right - 1);
            y = Math.Max(y, top);
            return true;
        }
        return false;
    }

    private const int WM_NCHITTEST = 0x0084;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int WM_NCLBUTTONUP = 0x00A2;
    private const int WM_NCLBUTTONDBLCLK = 0x00A3;
    private const int HTMINBUTTON = 8;
    private const int HTMAXBUTTON = 9;
    private const int HTCLOSE = 20;

    /// <summary>Distância máxima, em pixels físicos, entre o botão e a borda da janela que ainda conta como botão.</summary>
    private const int CaptionEdgeReach = 12;

    private readonly List<TitleBarButton> _captionButtons = new();

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

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
