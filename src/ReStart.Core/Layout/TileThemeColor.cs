namespace ReStart.Core;

/// <summary>Selects a color from the current theme for an individual tile.</summary>
public enum TileThemeColor
{
    /// <summary>Uses the ordinary app, widget, or shared tile appearance.</summary>
    Automatic,
    /// <summary>Uses the background color of the current theme.</summary>
    Background,
    /// <summary>Uses the accent color of the current theme.</summary>
    Accent
}
