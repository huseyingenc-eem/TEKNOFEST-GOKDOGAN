using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GOKDOGANIHA.Core.Models;

namespace GOKDOGANIHA.UI.Core;

/// <summary>
/// Shared presentation of the four competition kamikaze phases. Both compact
/// sidebar and fullscreen views use this control so phase-to-colour rules have
/// one owner.
/// </summary>
public partial class KamikazePhaseStepper : UserControl
{
    public KamikazePhaseStepper()
    {
        Steps =
        [
            new("INTİKAL"),
            new("DALIŞ"),
            new("QR"),
            new("PAS GEÇ")
        ];

        InitializeComponent();
        UpdateSteps();
    }

    public ObservableCollection<KamikazePhaseStepPresentation> Steps { get; }

    public static readonly DependencyProperty PhaseProperty =
        DependencyProperty.Register(nameof(Phase), typeof(KamikazePhase), typeof(KamikazePhaseStepper),
            new PropertyMetadata(KamikazePhase.Idle, OnPhaseChanged));

    public static readonly DependencyProperty CompactProperty =
        DependencyProperty.Register(nameof(Compact), typeof(bool), typeof(KamikazePhaseStepper),
            new PropertyMetadata(false));

    public KamikazePhase Phase
    {
        get => (KamikazePhase)GetValue(PhaseProperty);
        set => SetValue(PhaseProperty, value);
    }

    public bool Compact
    {
        get => (bool)GetValue(CompactProperty);
        set => SetValue(CompactProperty, value);
    }

    private static void OnPhaseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((KamikazePhaseStepper)d).UpdateSteps();

    private void UpdateSteps()
    {
        if (Steps.Count == 0) return;

        var pendingBackground = ResourceBrush("TacticalSurface");
        var pendingBorder = ResourceBrush("TacticalOverlayBorder");
        var completed = ResourceBrush("TacticalOk");
        var failed = ResourceBrush("TacticalCritical");
        var active = Phase == KamikazePhase.Dalis
            ? ResourceBrush("TacticalWarn")
            : ResourceBrush("TacticalAccent");

        for (var index = 0; index < Steps.Count; index++)
        {
            var state = GetStepState(Phase, index);
            Steps[index].Background = state switch
            {
                KamikazeStepState.Active => active,
                KamikazeStepState.Completed => completed,
                KamikazeStepState.Failed => failed,
                _ => pendingBackground
            };
            Steps[index].BorderBrush = state is KamikazeStepState.Active or KamikazeStepState.Failed
                ? Steps[index].Background
                : pendingBorder;
        }
    }

    private Brush ResourceBrush(string key)
        => (Brush)FindResource(key);

    private static KamikazeStepState GetStepState(KamikazePhase phase, int stepIndex)
    {
        if (phase == KamikazePhase.Hata) return KamikazeStepState.Failed;
        if (phase == KamikazePhase.Tamam) return KamikazeStepState.Completed;
        if (phase == KamikazePhase.QrOkundu)
            return stepIndex <= 2 ? KamikazeStepState.Completed : KamikazeStepState.Pending;

        var activeStep = phase switch
        {
            KamikazePhase.Intikal => 0,
            KamikazePhase.Dalis => 1,
            KamikazePhase.QrAriyor => 2,
            KamikazePhase.PasGec => 3,
            _ => -1
        };

        if (activeStep < 0) return KamikazeStepState.Pending;
        if (stepIndex < activeStep) return KamikazeStepState.Completed;
        if (stepIndex > activeStep) return KamikazeStepState.Pending;

        return KamikazeStepState.Active;
    }

    private enum KamikazeStepState
    {
        Pending,
        Active,
        Completed,
        Failed
    }
}

public sealed class KamikazePhaseStepPresentation : INotifyPropertyChanged
{
    private Brush? _background;
    private Brush? _borderBrush;

    public KamikazePhaseStepPresentation(string label) => Label = label;

    public string Label { get; }

    public Brush? Background
    {
        get => _background;
        set => SetField(ref _background, value);
    }

    public Brush? BorderBrush
    {
        get => _borderBrush;
        set => SetField(ref _borderBrush, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
