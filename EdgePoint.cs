namespace NmmEdgeFinder
{
    public class EdgePoint
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
    }

    public enum ScanDirection
    {
        Unknown,
        Forward,
        Backward
    }
}
