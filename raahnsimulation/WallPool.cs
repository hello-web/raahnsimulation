namespace RaahnSimulation
{
    public class WallPool : EntityPool<Wall>
    {
        public WallPool(Simulator sim, int size = DEFAULT_SIZE) : base(sim, size)
        {
            Wall wall;

            for (int i = 0; i < size; i++)
            {
                wall = new Wall(sim);
                elements.Add(wall);
            }
        }
    }
}
