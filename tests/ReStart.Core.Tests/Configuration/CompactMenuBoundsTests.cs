using System.Drawing;
using ReStart.App.Services;

namespace ReStart.Core.Tests;

public class CompactMenuBoundsTests
{
    private static readonly Rectangle Area = new(-1440, -50, 1440, 900);

    [Fact]
    public void UsesWorkAreaReservationsWhenTheTaskbarWindowCannotBeIdentified()
    {
        Assert.Equal(TaskbarEdge.Left, CompactMenuBounds.InferDock(Area, Rectangle.FromLTRB(-1392, -50, 0, 850)));
        Assert.Equal(TaskbarEdge.Top, CompactMenuBounds.InferDock(Area, Rectangle.FromLTRB(-1440, -2, 0, 850)));
        Assert.Equal(TaskbarEdge.Right, CompactMenuBounds.InferDock(Area, Rectangle.FromLTRB(-1440, -50, -48, 850)));
        Assert.Equal(TaskbarEdge.Bottom, CompactMenuBounds.InferDock(Area, Rectangle.FromLTRB(-1440, -50, 0, 802)));
        Assert.Equal(TaskbarEdge.Bottom, CompactMenuBounds.InferDock(Area, Area));
    }

    [Fact]
    public void PlacesTheSameLogicalSizeAtEachTaskbarEdgeOnAnOffsetMonitor()
    {
        var preferred = new Size(800, 500);
        Assert.Equal(new(-1440, -50, 1000, 625), CompactMenuBounds.Place(Area, TaskbarEdge.Top, preferred, 120));
        Assert.Equal(new(-1440, -50, 1000, 625), CompactMenuBounds.Place(Area, TaskbarEdge.Left, preferred, 120));
        Assert.Equal(new(-1000, -50, 1000, 625), CompactMenuBounds.Place(Area, TaskbarEdge.Right, preferred, 120));
        Assert.Equal(new(-1440, 225, 1000, 625), CompactMenuBounds.Place(Area, TaskbarEdge.Bottom, preferred, 120));
        Assert.Equal(800, CompactMenuBounds.ToDips(1000, 120));
    }

    [Fact]
    public void CapsBothPreferredAndMinimumSizesOnASmallHighDpiMonitor()
    {
        var small = new Rectangle(1600, -450, 800, 420);
        Assert.Equal(small, CompactMenuBounds.Place(small, TaskbarEdge.Bottom, new(int.MaxValue, int.MaxValue), 192));
        Assert.Equal(small, CompactMenuBounds.Place(small, TaskbarEdge.Right, new(1, 1), 192));
        Assert.Equal(small, CompactMenuBounds.Resize(small, TaskbarEdge.Bottom, small, new(1600, -450, 50, 50), SizingEdge.BottomRight, 192));
    }

    [Fact]
    public void AllEdgesAndCornersResizeWithTheBottomAnchored()
    {
        var initial = new Rectangle(-1340, 250, 800, 600);
        var proposed = Rectangle.FromLTRB(-1390, 200, -490, 900);
        var expected = new Dictionary<SizingEdge, Rectangle>
        {
            [SizingEdge.Left] = new(-1390, 250, 850, 600),
            [SizingEdge.Right] = new(-1340, 250, 850, 600),
            [SizingEdge.Top] = new(-1340, 200, 800, 650),
            [SizingEdge.Bottom] = new(-1340, 200, 800, 650),
            [SizingEdge.TopLeft] = new(-1390, 200, 850, 650),
            [SizingEdge.TopRight] = new(-1340, 200, 850, 650),
            [SizingEdge.BottomLeft] = new(-1390, 200, 850, 650),
            [SizingEdge.BottomRight] = new(-1340, 200, 850, 650)
        };
        foreach (var (edge, bounds) in expected)
            Assert.Equal(bounds, CompactMenuBounds.Resize(Area, TaskbarEdge.Bottom, initial, proposed, edge, 96));
    }

    [Fact]
    public void ClampsAtTheFixedEdgesAndPreservesOtherTaskbarDocks()
    {
        var preferred = new Size(800, 600);
        var beyondMonitor = Rectangle.Inflate(Area, Area.Width, Area.Height);
        var bottom = CompactMenuBounds.Place(Area, TaskbarEdge.Bottom, preferred, 96);
        Assert.Equal(Area, CompactMenuBounds.Resize(Area, TaskbarEdge.Bottom, bottom, beyondMonitor, SizingEdge.TopRight, 96));
        var narrow = new Rectangle(bottom.Left, bottom.Top, 10, 10);
        var minimum = CompactMenuBounds.Resize(Area, TaskbarEdge.Bottom, bottom, narrow, SizingEdge.BottomRight, 96);
        Assert.Equal(new Rectangle(Area.Left, Area.Bottom - 480, 640, 480), minimum);
        var top = CompactMenuBounds.Place(Area, TaskbarEdge.Top, preferred, 96);
        Assert.Equal(Area, CompactMenuBounds.Resize(Area, TaskbarEdge.Top, top, beyondMonitor, SizingEdge.TopRight, 96));
        var left = CompactMenuBounds.Place(Area, TaskbarEdge.Left, preferred, 96);
        Assert.Equal(Area, CompactMenuBounds.Resize(Area, TaskbarEdge.Left, left, beyondMonitor, SizingEdge.BottomLeft, 96));
        var right = CompactMenuBounds.Place(Area, TaskbarEdge.Right, preferred, 96);
        Assert.Equal(Area, CompactMenuBounds.Resize(Area, TaskbarEdge.Right, right, beyondMonitor, SizingEdge.BottomRight, 96));
    }
}
