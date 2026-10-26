using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace PrinterInstall.App.Controls;

/// <summary>
/// Texto de uma linha com espaçamento entre letras, que o TextBlock do WPF não oferece.
/// Usado no nome da marca, em caixa alta e espaçado como no Talos.
/// </summary>
public sealed class LetterSpacedText : FrameworkElement
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(LetterSpacedText),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Espaço extra depois de cada letra, em fração do tamanho da fonte (0.32 = 0,32 em).</summary>
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
        nameof(Spacing), typeof(double), typeof(LetterSpacedText),
        new FrameworkPropertyMetadata(0.32, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(
        typeof(LetterSpacedText),
        new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(
        typeof(LetterSpacedText),
        new FrameworkPropertyMetadata(SystemFonts.MessageFontSize, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(
        typeof(LetterSpacedText),
        new FrameworkPropertyMetadata(FontWeights.Normal, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(
        typeof(LetterSpacedText),
        new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public FontFamily FontFamily
    {
        get => (FontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var glyphs = CreateGlyphs();
        if (glyphs.Count == 0)
            return default;

        // O espaço depois da última letra não entra na largura, para o texto alinhar pela borda.
        var width = glyphs.Sum(g => g.WidthIncludingTrailingWhitespace) + Spacing * FontSize * (glyphs.Count - 1);
        var height = glyphs.Max(g => g.Height);
        return new Size(width, height);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var x = 0d;
        foreach (var glyph in CreateGlyphs())
        {
            drawingContext.DrawText(glyph, new Point(x, 0));
            x += glyph.WidthIncludingTrailingWhitespace + Spacing * FontSize;
        }
    }

    private List<FormattedText> CreateGlyphs()
    {
        var typeface = new Typeface(FontFamily, FontStyles.Normal, FontWeight, FontStretches.Normal);
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        return (Text ?? string.Empty)
            .Select(c => new FormattedText(
                c.ToString(),
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                FontSize,
                Foreground,
                pixelsPerDip))
            .ToList();
    }
}
