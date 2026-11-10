using System.Drawing;

namespace ReStart.App.Services;

// The values correspond to the ABE_* edges returned by SHAppBarMessage.
internal enum TaskbarEdge { Left, Top, Right, Bottom }

// The values correspond to the WMSZ_* directions supplied with WM_SIZING.
internal enum SizingEdge { Left = 1, Right, Top, TopLeft, TopRight, Bottom, BottomLeft, BottomRight }

internal static class CompactMenuBounds
{
    internal static Size Minimum(Rectangle area, uint dpi) => new(Pixels(640, dpi, area.Width), Pixels(480, dpi, area.Height));

    private static int Pixels(int dips, uint dpi, int available) => (int)Math.Clamp(Math.Round(dips * (dpi / 96d)), 1, available);

    internal static TaskbarEdge InferDock(Rectangle monitor, Rectangle working)
    {
        var left = working.Left - monitor.Left;
        var top = working.Top - monitor.Top;
        var right = monitor.Right - working.Right;
        var bottom = monitor.Bottom - working.Bottom;
        var largest = Math.Max(Math.Max(left, right), Math.Max(top, bottom));
        if (largest <= 0 || bottom == largest) return TaskbarEdge.Bottom;
        if (left == largest) return TaskbarEdge.Left;
        return top == largest ? TaskbarEdge.Top : TaskbarEdge.Right;
    }

    internal static Rectangle Place(Rectangle area, TaskbarEdge dock, Size preferred, uint dpi)
    {
        var minimum = Minimum(area, dpi);
        var width = Math.Max(minimum.Width, Pixels(preferred.Width, dpi, area.Width));
        var height = Math.Max(minimum.Height, Pixels(preferred.Height, dpi, area.Height));
        return dock switch
        {
            TaskbarEdge.Left or TaskbarEdge.Top => new(area.Left, area.Top, width, height),
            TaskbarEdge.Right => new(area.Right - width, area.Top, width, height),
            _ => new(area.Left, area.Bottom - height, width, height)
        };
    }

    internal static Rectangle Resize(Rectangle area, TaskbarEdge dock, Rectangle initial, Rectangle proposed, SizingEdge edge, uint dpi)
    {
        var minimum = Minimum(area, dpi);
        var fromLeft = edge is SizingEdge.Left or SizingEdge.TopLeft or SizingEdge.BottomLeft;
        var fromRight = edge is SizingEdge.Right or SizingEdge.TopRight or SizingEdge.BottomRight;
        var fromTop = edge is SizingEdge.Top or SizingEdge.TopLeft or SizingEdge.TopRight;
        var fromBottom = edge is SizingEdge.Bottom or SizingEdge.BottomLeft or SizingEdge.BottomRight;
        var fixRight = dock == TaskbarEdge.Right || dock != TaskbarEdge.Left && fromLeft;
        var fixBottom = dock == TaskbarEdge.Bottom || dock != TaskbarEdge.Top && fromTop;
        var fixedX = fixRight ? initial.Right : initial.Left;
        var fixedY = fixBottom ? initial.Bottom : initial.Top;

        // A docked edge stays against the taskbar even if it is the edge being
        // dragged. Its drag changes the size by moving the opposite edge.
        var width = fromLeft ? initial.Right - proposed.Left : fromRight ? proposed.Right - initial.Left : initial.Width;
        var height = fromTop ? initial.Bottom - proposed.Top : fromBottom ? proposed.Bottom - initial.Top : initial.Height;
        width = Math.Clamp(width, minimum.Width, fixRight ? fixedX - area.Left : area.Right - fixedX);
        height = Math.Clamp(height, minimum.Height, fixBottom ? fixedY - area.Top : area.Bottom - fixedY);
        return new(fixRight ? fixedX - width : fixedX, fixBottom ? fixedY - height : fixedY, width, height);
    }

    internal static int ToDips(int pixels, uint dpi) => Math.Max(1, (int)Math.Round(pixels * 96d / dpi));
}
