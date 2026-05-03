using System;

namespace Storylines.Helpers
{
    public enum CanvasConnectorOrientation
    {
        Horizontal,
        Vertical
    }

    public struct CanvasConnectionPoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        public CanvasConnectionPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public class CanvasConnectionRect
    {
        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }

        public CanvasConnectionRect(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public CanvasConnectionPoint Center => new CanvasConnectionPoint(X + Width / 2.0, Y + Height / 2.0);
    public double CenterX => X + (Width / 2.0);
    public double CenterY => Y + (Height / 2.0);
        public double Left => X;
        public double Top => Y;
        public double Right => X + Width;
        public double Bottom => Y + Height;
    }

    public class CanvasBezierConnection
    {
        public CanvasConnectionPoint Start { get; set; }
        public CanvasConnectionPoint Control1 { get; set; }
        public CanvasConnectionPoint Control2 { get; set; }
        public CanvasConnectionPoint End { get; set; }
        public CanvasConnectionPoint Label { get; set; }
        public double EndTangentAngleRadians { get; set; }

        public CanvasBezierConnection(CanvasConnectionPoint start, CanvasConnectionPoint control1, CanvasConnectionPoint control2, CanvasConnectionPoint end)
            : this(
                start,
                control1,
                control2,
                end,
                EvaluateAtT(start, control1, control2, end, 0.5),
                Math.Atan2(end.Y - control2.Y, end.X - control2.X))
        {
        }

        public CanvasBezierConnection(
            CanvasConnectionPoint start,
            CanvasConnectionPoint control1,
            CanvasConnectionPoint control2,
            CanvasConnectionPoint end,
            CanvasConnectionPoint label,
            double endTangentAngleRadians)
        {
            Start = start;
            Control1 = control1;
            Control2 = control2;
            End = end;
            Label = label;
            EndTangentAngleRadians = endTangentAngleRadians;
        }

        private static CanvasConnectionPoint EvaluateAtT(
            CanvasConnectionPoint start,
            CanvasConnectionPoint control1,
            CanvasConnectionPoint control2,
            CanvasConnectionPoint end,
            double t)
        {
            double u = 1 - t;
            double b0 = u * u * u;
            double b1 = 3 * u * u * t;
            double b2 = 3 * u * t * t;
            double b3 = t * t * t;

            double x = b0 * start.X + b1 * control1.X + b2 * control2.X + b3 * end.X;
            double y = b0 * start.Y + b1 * control1.Y + b2 * control2.Y + b3 * end.Y;
            return new CanvasConnectionPoint(x, y);
        }
    }

    public static class CanvasConnectionGeometry
    {
        public static CanvasBezierConnection CreateBranchingConnection(
            CanvasConnectionRect fromRect,
            CanvasConnectionRect toRect,
            double minControlDistance = 72,
            double backwardLoopOffset = 64)
        {
            var start = new CanvasConnectionPoint(fromRect.Right, fromRect.CenterY);
            var end = new CanvasConnectionPoint(toRect.Left, toRect.CenterY);

            var deltaX = end.X - start.X;
            var deltaY = end.Y - start.Y;

            CanvasConnectionPoint control1;
            CanvasConnectionPoint control2;

            if (deltaX >= 0)
            {
                var controlDistance = Math.Max(minControlDistance, Math.Abs(deltaX) * 0.45);
                control1 = new CanvasConnectionPoint(start.X + controlDistance, start.Y);
                control2 = new CanvasConnectionPoint(end.X - controlDistance, end.Y);
            }
            else
            {
                var verticalDirection = deltaY >= 0 ? 1 : -1;
                var loopHeight = Math.Max(backwardLoopOffset, (Math.Abs(deltaY) * 0.5) + 24);
                var loopWidth = Math.Max(48, minControlDistance * 0.75);

                control1 = new CanvasConnectionPoint(start.X + loopWidth, start.Y + (verticalDirection * loopHeight));
                control2 = new CanvasConnectionPoint(end.X - loopWidth, end.Y - (verticalDirection * loopHeight));
            }

            return CreateConnection(start, control1, control2, end);
        }

        public static CanvasBezierConnection CreatePinboardConnection(
            CanvasConnectionRect fromRect,
            CanvasConnectionRect toRect,
            double minControlDistance = 60)
        {
            var anchors = GetPinboardAnchors(fromRect, toRect);
            var start = anchors.Start;
            var end = anchors.End;

            CanvasConnectionPoint control1;
            CanvasConnectionPoint control2;

            if (anchors.Orientation == CanvasConnectorOrientation.Horizontal)
            {
                var deltaX = end.X - start.X;
                var horizontalDirection = deltaX >= 0 ? 1 : -1;
                var controlDistance = Math.Max(minControlDistance, Math.Abs(deltaX) * 0.45);

                control1 = new CanvasConnectionPoint(start.X + (horizontalDirection * controlDistance), start.Y);
                control2 = new CanvasConnectionPoint(end.X - (horizontalDirection * controlDistance), end.Y);
            }
            else
            {
                var deltaY = end.Y - start.Y;
                var verticalDirection = deltaY >= 0 ? 1 : -1;
                var controlDistance = Math.Max(minControlDistance, Math.Abs(deltaY) * 0.45);

                control1 = new CanvasConnectionPoint(start.X, start.Y + (verticalDirection * controlDistance));
                control2 = new CanvasConnectionPoint(end.X, end.Y - (verticalDirection * controlDistance));
            }

            return CreateConnection(start, control1, control2, end);
        }

        public static CanvasConnectionPoint[] CreateArrowHead(
            CanvasConnectionPoint tip,
            double angleRadians,
            double size = 8,
            double widthFactor = 0.55)
        {
            var cos = Math.Cos(angleRadians);
            var sin = Math.Sin(angleRadians);
            var baseX = tip.X - (cos * size);
            var baseY = tip.Y - (sin * size);
            var normalX = -sin * size * widthFactor;
            var normalY = cos * size * widthFactor;

            return new[]
            {
                tip,
                new CanvasConnectionPoint(baseX + normalX, baseY + normalY),
                new CanvasConnectionPoint(baseX - normalX, baseY - normalY)
            };
        }

        public static bool HasMovedBeyondThreshold(
            CanvasConnectionPoint a,
            CanvasConnectionPoint b,
            double threshold = 6)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            return (dx * dx) + (dy * dy) >= threshold * threshold;
        }

        private static CanvasBezierConnection CreateConnection(
            CanvasConnectionPoint start,
            CanvasConnectionPoint control1,
            CanvasConnectionPoint control2,
            CanvasConnectionPoint end)
        {
            return new CanvasBezierConnection(
                start,
                control1,
                control2,
                end,
                EvaluatePoint(start, control1, control2, end, 0.5),
                Math.Atan2(end.Y - control2.Y, end.X - control2.X));
        }

        private static CanvasConnectionPoint EvaluatePoint(
            CanvasConnectionPoint start,
            CanvasConnectionPoint control1,
            CanvasConnectionPoint control2,
            CanvasConnectionPoint end,
            double t)
        {
            var inverseT = 1 - t;
            var x = (inverseT * inverseT * inverseT * start.X)
                + (3 * inverseT * inverseT * t * control1.X)
                + (3 * inverseT * t * t * control2.X)
                + (t * t * t * end.X);
            var y = (inverseT * inverseT * inverseT * start.Y)
                + (3 * inverseT * inverseT * t * control1.Y)
                + (3 * inverseT * t * t * control2.Y)
                + (t * t * t * end.Y);

            return new CanvasConnectionPoint(x, y);
        }

        private static (CanvasConnectionPoint Start, CanvasConnectionPoint End, CanvasConnectorOrientation Orientation) GetPinboardAnchors(
            CanvasConnectionRect fromRect,
            CanvasConnectionRect toRect)
        {
            var deltaX = toRect.CenterX - fromRect.CenterX;
            var deltaY = toRect.CenterY - fromRect.CenterY;

            if (Math.Abs(deltaX) >= Math.Abs(deltaY))
            {
                if (deltaX >= 0)
                {
                    return (
                        new CanvasConnectionPoint(fromRect.Right, fromRect.CenterY),
                        new CanvasConnectionPoint(toRect.Left, toRect.CenterY),
                        CanvasConnectorOrientation.Horizontal);
                }

                return (
                    new CanvasConnectionPoint(fromRect.Left, fromRect.CenterY),
                    new CanvasConnectionPoint(toRect.Right, toRect.CenterY),
                    CanvasConnectorOrientation.Horizontal);
            }

            if (deltaY >= 0)
            {
                return (
                    new CanvasConnectionPoint(fromRect.CenterX, fromRect.Bottom),
                    new CanvasConnectionPoint(toRect.CenterX, toRect.Top),
                    CanvasConnectorOrientation.Vertical);
            }

            return (
                new CanvasConnectionPoint(fromRect.CenterX, fromRect.Top),
                new CanvasConnectionPoint(toRect.CenterX, toRect.Bottom),
                CanvasConnectorOrientation.Vertical);
        }
    }
}
