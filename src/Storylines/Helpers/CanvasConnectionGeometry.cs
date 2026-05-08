using System;

namespace Storylines.Helpers
{
    public readonly struct CanvasConnectionPoint
    {
        public CanvasConnectionPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }

        public double Y { get; }
    }

    public readonly struct CanvasConnectionRect
    {
        public CanvasConnectionRect(double left, double top, double width, double height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public double Left { get; }

        public double Top { get; }

        public double Width { get; }

        public double Height { get; }

        public double Right => Left + Width;

        public double Bottom => Top + Height;

        public double CenterX => Left + (Width / 2);

        public double CenterY => Top + (Height / 2);
    }

    public readonly struct CanvasBezierConnection
    {
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

        public CanvasConnectionPoint Start { get; }

        public CanvasConnectionPoint Control1 { get; }

        public CanvasConnectionPoint Control2 { get; }

        public CanvasConnectionPoint End { get; }

        public CanvasConnectionPoint Label { get; }

        public double EndTangentAngleRadians { get; }
    }

    public static class CanvasConnectionGeometry
    {
        private const double MinimumCurveOffset = 48;
        private const double MaximumCurveOffset = 140;
        private const double BackwardLoopHeight = 64;
        private const double ArrowHeadLength = 12;
        private const double ArrowHeadHalfWidth = 5;
        private const double PointerMovementThreshold = 6;

        public static CanvasBezierConnection CreatePinboardConnection(CanvasConnectionRect from, CanvasConnectionRect to)
        {
            var deltaX = to.CenterX - from.CenterX;
            var deltaY = to.CenterY - from.CenterY;
            var useHorizontalAnchors = Math.Abs(deltaX) >= Math.Abs(deltaY);

            if (useHorizontalAnchors)
            {
                var start = new CanvasConnectionPoint(deltaX >= 0 ? from.Right : from.Left, from.CenterY);
                var end = new CanvasConnectionPoint(deltaX >= 0 ? to.Left : to.Right, to.CenterY);
                var controlOffset = Clamp(Math.Abs(end.X - start.X) * 0.35, MinimumCurveOffset, MaximumCurveOffset);
                var control1 = new CanvasConnectionPoint(deltaX >= 0 ? start.X + controlOffset : start.X - controlOffset, start.Y);
                var control2 = new CanvasConnectionPoint(deltaX >= 0 ? end.X - controlOffset : end.X + controlOffset, end.Y);
                return CreateConnection(start, control1, control2, end);
            }

            var verticalStart = new CanvasConnectionPoint(from.CenterX, deltaY >= 0 ? from.Bottom : from.Top);
            var verticalEnd = new CanvasConnectionPoint(to.CenterX, deltaY >= 0 ? to.Top : to.Bottom);
            var verticalOffset = Clamp(Math.Abs(verticalEnd.Y - verticalStart.Y) * 0.35, MinimumCurveOffset, MaximumCurveOffset);
            var verticalControl1 = new CanvasConnectionPoint(verticalStart.X, deltaY >= 0 ? verticalStart.Y + verticalOffset : verticalStart.Y - verticalOffset);
            var verticalControl2 = new CanvasConnectionPoint(verticalEnd.X, deltaY >= 0 ? verticalEnd.Y - verticalOffset : verticalEnd.Y + verticalOffset);
            return CreateConnection(verticalStart, verticalControl1, verticalControl2, verticalEnd);
        }

        public static CanvasBezierConnection CreateBranchingConnection(CanvasConnectionRect from, CanvasConnectionRect to)
        {
            var start = new CanvasConnectionPoint(from.Right, from.CenterY);
            var end = new CanvasConnectionPoint(to.Left, to.CenterY);
            var deltaX = end.X - start.X;
            var deltaY = end.Y - start.Y;

            if (deltaX >= 0)
            {
                var controlOffset = Clamp(deltaX * 0.5, MinimumCurveOffset, MaximumCurveOffset);
                var control1 = new CanvasConnectionPoint(start.X + controlOffset, start.Y);
                var control2 = new CanvasConnectionPoint(end.X - controlOffset, end.Y);
                return CreateConnection(start, control1, control2, end);
            }

            var horizontalOffset = Clamp((Math.Abs(deltaX) * 0.35) + 40, 72, 180);
            var verticalOffset = Math.Max(BackwardLoopHeight, Math.Abs(deltaY) * 0.5);
            var bendDirection = deltaY >= 0 ? -1 : 1;
            var backwardControl1 = new CanvasConnectionPoint(start.X + horizontalOffset, start.Y + (bendDirection * verticalOffset));
            var backwardControl2 = new CanvasConnectionPoint(end.X - horizontalOffset, end.Y - (bendDirection * verticalOffset));
            return CreateConnection(start, backwardControl1, backwardControl2, end);
        }

        public static CanvasConnectionPoint[] CreateArrowHead(CanvasConnectionPoint tip, double angleRadians)
        {
            var baseCenterX = tip.X - (Math.Cos(angleRadians) * ArrowHeadLength);
            var baseCenterY = tip.Y - (Math.Sin(angleRadians) * ArrowHeadLength);
            var perpendicularX = -Math.Sin(angleRadians) * ArrowHeadHalfWidth;
            var perpendicularY = Math.Cos(angleRadians) * ArrowHeadHalfWidth;

            return new[]
            {
                tip,
                new CanvasConnectionPoint(baseCenterX + perpendicularX, baseCenterY + perpendicularY),
                new CanvasConnectionPoint(baseCenterX - perpendicularX, baseCenterY - perpendicularY)
            };
        }

        public static bool HasMovedBeyondThreshold(CanvasConnectionPoint start, CanvasConnectionPoint current)
        {
            var deltaX = current.X - start.X;
            var deltaY = current.Y - start.Y;
            return (deltaX * deltaX) + (deltaY * deltaY) >= PointerMovementThreshold * PointerMovementThreshold;
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
                EvaluateBezier(start, control1, control2, end, 0.5),
                Math.Atan2(end.Y - control2.Y, end.X - control2.X));
        }

        private static CanvasConnectionPoint EvaluateBezier(
            CanvasConnectionPoint start,
            CanvasConnectionPoint control1,
            CanvasConnectionPoint control2,
            CanvasConnectionPoint end,
            double t)
        {
            var oneMinusT = 1 - t;
            var x = (oneMinusT * oneMinusT * oneMinusT * start.X)
                + (3 * oneMinusT * oneMinusT * t * control1.X)
                + (3 * oneMinusT * t * t * control2.X)
                + (t * t * t * end.X);
            var y = (oneMinusT * oneMinusT * oneMinusT * start.Y)
                + (3 * oneMinusT * oneMinusT * t * control1.Y)
                + (3 * oneMinusT * t * t * control2.Y)
                + (t * t * t * end.Y);

            return new CanvasConnectionPoint(x, y);
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}