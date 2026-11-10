using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ReStart.App.Controls;

/// <summary>Provides a layout-neutral tile press response; reduced motion never creates animation clocks.</summary>
internal sealed class TileSurfaceButton : Button
{
    internal bool MotionAllowed { get; set; }
    public static readonly DependencyProperty HoverHighlightEnabledProperty = DependencyProperty.Register(nameof(HoverHighlightEnabled), typeof(bool), typeof(TileSurfaceButton), new PropertyMetadata(true));
    public bool HoverHighlightEnabled { get => (bool)GetValue(HoverHighlightEnabledProperty); set => SetValue(HoverHighlightEnabledProperty, value); }
    public static readonly DependencyProperty PlateOpacityProperty = DependencyProperty.Register(nameof(PlateOpacity), typeof(double), typeof(TileSurfaceButton), new PropertyMetadata(1d));
    public double PlateOpacity { get => (double)GetValue(PlateOpacityProperty); set => SetValue(PlateOpacityProperty, value); }
    private readonly ScaleTransform _press = new();
    internal TileSurfaceButton()
    {
        RenderTransform = _press; RenderTransformOrigin = new(.5, .5);
        SetResourceReference(StyleProperty, "TileSurfaceAction");
    }
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property != IsPressedProperty || _press == null) return;
        var scale = IsPressed && MotionAllowed ? .975 : 1;
        foreach (var property in new[] { ScaleTransform.ScaleXProperty, ScaleTransform.ScaleYProperty })
        {
            if (!MotionAllowed) { _press.BeginAnimation(property, null); _press.SetValue(property, 1d); }
            else _press.BeginAnimation(property, new DoubleAnimation(scale, TimeSpan.FromMilliseconds(IsPressed ? 70 : 120))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }
    }
}
