namespace RaahnSimulation
{
    public class PointPool : EntityPool<Point>
    {
        public PointPool(Simulator sim, int size = DEFAULT_SIZE) : base(sim, size)
        {
            Point point;

            for (int i = 0; i < size; i++)
            {
                point = new Point(sim);
                elements.Add(point);
            }
        }
    }
}

