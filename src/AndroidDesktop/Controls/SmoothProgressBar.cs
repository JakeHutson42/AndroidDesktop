using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace AndroidDesktop.Controls;

/// <summary>Animate stage transitions without making the progress calculation time dependent.</summary>
public sealed class SmoothProgressBar : ProgressBar
{
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(nameof(Progress), typeof(double),
        typeof(SmoothProgressBar), new PropertyMetadata(0d, ProgressChanged));
    public double Progress { get => (double)GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    private static void ProgressChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        var bar = (SmoothProgressBar)target;
        var progress = Math.Clamp((double)args.NewValue, bar.Minimum, bar.Maximum);
        if (progress == bar.Minimum) { bar.BeginAnimation(ValueProperty, null); bar.Value = progress; return; }
        bar.BeginAnimation(ValueProperty, new DoubleAnimation(bar.Value, progress, TimeSpan.FromMilliseconds(250))
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
    }
}
