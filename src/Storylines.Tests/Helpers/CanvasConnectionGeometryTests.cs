using Storylines.Helpers;
using Xunit;

namespace Storylines.Tests.Helpers;

public class CanvasConnectionGeometryTests
{
    [Fact]
    public void CreateBranchingConnection_UsesOutputToInputAnchors()
    {
        var from = new CanvasConnectionRect(20, 40, 200, 100);
        var to = new CanvasConnectionRect(320, 50, 200, 100);

        var connection = CanvasConnectionGeometry.CreateBranchingConnection(from, to);

        Assert.Equal(220, connection.Start.X, 3);
        Assert.Equal(90, connection.Start.Y, 3);
        Assert.Equal(320, connection.End.X, 3);
        Assert.Equal(100, connection.End.Y, 3);
        Assert.True(connection.Control1.X > connection.Start.X);
        Assert.True(connection.Control2.X < connection.End.X);
        Assert.InRange(connection.Label.X, connection.Start.X, connection.End.X);
    }

    [Fact]
    public void CreateBranchingConnection_BackwardLinksCurveAwayFromCards()
    {
        var from = new CanvasConnectionRect(360, 40, 200, 100);
        var to = new CanvasConnectionRect(80, 120, 200, 100);

        var connection = CanvasConnectionGeometry.CreateBranchingConnection(from, to);

        Assert.Equal(from.Right, connection.Start.X, 3);
        Assert.Equal(to.Left, connection.End.X, 3);
        Assert.NotEqual(connection.Start.Y, connection.Control1.Y);
        Assert.NotEqual(connection.End.Y, connection.Control2.Y);
    }

    [Fact]
    public void CreatePinboardConnection_HorizontalLayoutsAttachToSideEdges()
    {
        var from = new CanvasConnectionRect(0, 0, 200, 180);
        var to = new CanvasConnectionRect(320, 40, 200, 180);

        var connection = CanvasConnectionGeometry.CreatePinboardConnection(from, to);

        Assert.Equal(200, connection.Start.X, 3);
        Assert.Equal(90, connection.Start.Y, 3);
        Assert.Equal(320, connection.End.X, 3);
        Assert.Equal(130, connection.End.Y, 3);
    }

    [Fact]
    public void CreatePinboardConnection_VerticalLayoutsAttachToTopAndBottomEdges()
    {
        var from = new CanvasConnectionRect(0, 0, 200, 180);
        var to = new CanvasConnectionRect(30, 260, 200, 180);

        var connection = CanvasConnectionGeometry.CreatePinboardConnection(from, to);

        Assert.Equal(100, connection.Start.X, 3);
        Assert.Equal(180, connection.Start.Y, 3);
        Assert.Equal(130, connection.End.X, 3);
        Assert.Equal(260, connection.End.Y, 3);
    }

    [Fact]
    public void HasMovedBeyondThreshold_IgnoresTinyPointerJitter()
    {
        var start = new CanvasConnectionPoint(10, 10);

        Assert.False(CanvasConnectionGeometry.HasMovedBeyondThreshold(start, new CanvasConnectionPoint(13, 14)));
        Assert.True(CanvasConnectionGeometry.HasMovedBeyondThreshold(start, new CanvasConnectionPoint(16, 10)));
    }
}
