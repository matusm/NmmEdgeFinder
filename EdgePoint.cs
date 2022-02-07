namespace NmmEdgeFinder
{
    public struct EdgePoint
    {
        public double X { get; }
        public double Y { get; }
        public int DatIndex { get; }
        public ScanDirection Direction { get; }

        public EdgePoint(double X, double Y, int DatIndex, ScanDirection Direction)
        {
            this.X = X;
            this.Y = Y;
            this.DatIndex = DatIndex;
            this.Direction = Direction;
        }

        public override string ToString()
        {
            return $"[EdgePoint: X={X} Y={Y} DatIndex={DatIndex} Direction={Direction}]";
        }
    }

    public enum ScanDirection
    {
        Unknown,
        Forward,
        Backward
    }
}
