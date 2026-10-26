using System.Windows;
using System.Windows.Controls;

namespace PrinterInstall.App.Controls;

/// <summary>Marca na barra de título: emblema, nome HERMES e o nome da tela.</summary>
public partial class BrandTitle : UserControl
{
    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle), typeof(string), typeof(BrandTitle), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsAnimatingProperty = DependencyProperty.Register(
        nameof(IsAnimating), typeof(bool), typeof(BrandTitle), new PropertyMetadata(false));

    public BrandTitle()
    {
        InitializeComponent();
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <summary>Faz o emblema voar enquanto há trabalho em andamento.</summary>
    public bool IsAnimating
    {
        get => (bool)GetValue(IsAnimatingProperty);
        set => SetValue(IsAnimatingProperty, value);
    }
}
