namespace NmmEdgeFinder
{
    public class EdgePoint
    {
        public double X { get; }
        public double Y { get; }
        public int DatIndex { get; }

        public EdgePoint(double X, double Y, int DatIndex)
        {
            this.X = X;
            this.Y = Y;
            this.DatIndex = DatIndex;
        }
    }
}
