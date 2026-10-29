using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PrinterInstall.App.Controls;

/// <summary>
/// Barra de status da implantação: os trechos de sucesso, falha e neutro crescem suavemente a cada
/// resultado, e um brilho percorre o trecho ainda em andamento enquanto a implantação roda.
/// </summary>
public partial class StatusProgressBar : UserControl
{
    public static readonly DependencyProperty SucceededCountProperty = RegisterCount(nameof(SucceededCount));

    public static readonly DependencyProperty FailedCountProperty = RegisterCount(nameof(FailedCount));

    public static readonly DependencyProperty NeutralCountProperty = RegisterCount(nameof(NeutralCount));

    public static readonly DependencyProperty TotalCountProperty = RegisterCount(nameof(TotalCount));

    public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
        nameof(IsRunning), typeof(bool), typeof(StatusProgressBar),
        new PropertyMetadata(false, (d, _) => ((StatusProgressBar)d).UpdateShimmer()));

    private const double ShimmerWidth = 48;

    private static readonly Duration GrowDuration = new(TimeSpan.FromMilliseconds(250));

    private static readonly Duration SweepDuration = new(TimeSpan.FromSeconds(1.4));

    private bool _isSweeping;

    public StatusProgressBar()
    {
        InitializeComponent();
        Shimmer.Width = ShimmerWidth;
        Loaded += (_, _) =>
        {
            UpdateSegments(animate: false);
            UpdateShimmer();
        };
        Unloaded += (_, _) => StopSweep();
        SizeChanged += (_, _) =>
        {
            // A posição da falha e o recorte do brilho dependem da largura; a varredura também.
            UpdateSegments(animate: false);
            StopSweep();
            UpdateShimmer();
        };
    }

    public int SucceededCount
    {
        get => (int)GetValue(SucceededCountProperty);
        set => SetValue(SucceededCountProperty, value);
    }

    public int FailedCount
    {
        get => (int)GetValue(FailedCountProperty);
        set => SetValue(FailedCountProperty, value);
    }

    /// <summary>Linhas que terminaram sem sucesso nem falha.</summary>
    public int NeutralCount
    {
        get => (int)GetValue(NeutralCountProperty);
        set => SetValue(NeutralCountProperty, value);
    }

    public int TotalCount
    {
        get => (int)GetValue(TotalCountProperty);
        set => SetValue(TotalCountProperty, value);
    }

    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    private static DependencyProperty RegisterCount(string name) => DependencyProperty.Register(
        name, typeof(int), typeof(StatusProgressBar),
        new PropertyMetadata(0, (d, _) => ((StatusProgressBar)d).OnCountsChanged()));

    // Respeita "Mostrar animações no Windows" desligado.
    private bool CanAnimate => IsLoaded && SystemParameters.ClientAreaAnimation;

    private StatusBarSegments Segments => StatusBarSegments.Compute(SucceededCount, FailedCount, TotalCount, NeutralCount);

    private void OnCountsChanged()
    {
        UpdateSegments(CanAnimate);
        UpdateShimmer();
    }

    private void UpdateSegments(bool animate)
    {
        var segments = Segments;
        var width = ActualWidth;
        var done = segments.Success + segments.Failure + segments.Neutral;

        Move(SuccessScale, ScaleTransform.ScaleXProperty, segments.Success, animate);
        Move(FailureScale, ScaleTransform.ScaleXProperty, segments.Failure, animate);
        Move(FailureOffset, TranslateTransform.XProperty, segments.Success * width, animate);
        Move(NeutralScale, ScaleTransform.ScaleXProperty, segments.Neutral, animate);
        Move(NeutralOffset, TranslateTransform.XProperty, (segments.Success + segments.Failure) * width, animate);

        var pending = new Rect(done * width, 0, segments.Pending * width, ActualHeight);
        if (animate)
        {
            PendingClip.BeginAnimation(RectangleGeometry.RectProperty,
                new RectAnimation(pending, GrowDuration) { EasingFunction = GrowEase() },
                HandoffBehavior.SnapshotAndReplace);
        }
        else
        {
            PendingClip.BeginAnimation(RectangleGeometry.RectProperty, null);
            PendingClip.Rect = pending;
        }
    }

    private static void Move(Animatable target, DependencyProperty property, double value, bool animate)
    {
        if (animate)
        {
            // Parte do valor em que a animação anterior está, para resultados seguidos não "voltarem".
            target.BeginAnimation(property,
                new DoubleAnimation(value, GrowDuration) { EasingFunction = GrowEase() },
                HandoffBehavior.SnapshotAndReplace);
        }
        else
        {
            target.BeginAnimation(property, null);
            target.SetValue(property, value);
        }
    }

    private static IEasingFunction GrowEase() => new CubicEase { EasingMode = EasingMode.EaseOut };

    private void UpdateShimmer()
    {
        if (IsRunning && CanAnimate && Segments.Pending > 0 && ActualWidth > 0)
            StartSweep();
        else
            StopSweep();
    }

    private void StartSweep()
    {
        if (_isSweeping)
            return;

        _isSweeping = true;
        ShimmerLayer.Visibility = Visibility.Visible;
        ShimmerOffset.BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation(-ShimmerWidth, ActualWidth, SweepDuration)
            {
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });
    }

    private void StopSweep()
    {
        if (!_isSweeping)
            return;

        _isSweeping = false;
        ShimmerOffset.BeginAnimation(TranslateTransform.XProperty, null);
        ShimmerLayer.Visibility = Visibility.Collapsed;
    }
}
