using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PrinterInstall.App.Controls;

/// <summary>
/// Emblema do Hermes que "voa" enquanto há trabalho em andamento: a asa bate, a folha flutua e
/// as linhas CMY se imprimem uma depois da outra. Parado, mostra o emblema estático.
/// </summary>
public partial class HermesEmblem : UserControl
{
    public static readonly DependencyProperty IsAnimatingProperty = DependencyProperty.Register(
        nameof(IsAnimating), typeof(bool), typeof(HermesEmblem),
        new PropertyMetadata(false, (d, _) => ((HermesEmblem)d).UpdateAnimation()));

    private static readonly Duration InkCycle = new(TimeSpan.FromSeconds(1.8));

    private Storyboard? _flight;

    public HermesEmblem()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateAnimation();
        Unloaded += (_, _) => StopFlight();
    }

    public bool IsAnimating
    {
        get => (bool)GetValue(IsAnimatingProperty);
        set => SetValue(IsAnimatingProperty, value);
    }

    private void UpdateAnimation()
    {
        // Respeita "Mostrar animações no Windows" desligado.
        if (IsAnimating && IsLoaded && SystemParameters.ClientAreaAnimation)
            StartFlight();
        else
            StopFlight();
    }

    private void StartFlight()
    {
        if (_flight is not null)
            return;

        _flight = new Storyboard();
        AddWingBeat(_flight);
        AddHover(_flight);
        AddInk(_flight, nameof(CyanInk), TimeSpan.Zero);
        AddInk(_flight, nameof(MagentaInk), TimeSpan.FromSeconds(0.3));
        AddInk(_flight, nameof(YellowInk), TimeSpan.FromSeconds(0.6));
        _flight.Begin(this, isControllable: true);
    }

    private void StopFlight()
    {
        if (_flight is null)
            return;

        _flight.Stop(this);
        _flight.Remove(this);
        _flight = null;
    }

    private static void AddWingBeat(Storyboard storyboard)
    {
        var beat = new DoubleAnimation(0, -16, TimeSpan.FromSeconds(0.32))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTargetName(beat, nameof(WingBeat));
        Storyboard.SetTargetProperty(beat, new PropertyPath(RotateTransform.AngleProperty));
        storyboard.Children.Add(beat);
    }

    private static void AddHover(Storyboard storyboard)
    {
        // A folha sobe quando a asa desce, no mesmo compasso.
        var hover = new DoubleAnimation(0, -5, TimeSpan.FromSeconds(0.32))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTargetName(hover, nameof(FlightOffset));
        Storyboard.SetTargetProperty(hover, new PropertyPath(TranslateTransform.YProperty));
        storyboard.Children.Add(hover);
    }

    private static void AddInk(Storyboard storyboard, string lineName, TimeSpan start)
    {
        var printed = start + TimeSpan.FromSeconds(0.3);
        var ink = new DoubleAnimationUsingKeyFrames
        {
            Duration = InkCycle,
            RepeatBehavior = RepeatBehavior.Forever
        };
        ink.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        ink.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(start)));
        ink.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(printed), new CubicEase { EasingMode = EasingMode.EaseOut }));
        ink.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(InkCycle.TimeSpan)));
        Storyboard.SetTargetName(ink, lineName);
        Storyboard.SetTargetProperty(ink, new PropertyPath(ScaleTransform.ScaleXProperty));
        storyboard.Children.Add(ink);
    }
}
