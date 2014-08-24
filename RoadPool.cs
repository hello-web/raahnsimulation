namespace RaahnSimulation
{
	class RoadPool : EntityPool<Road>
	{
	    public RoadPool(Simulator sim, int size = DEFAULT_SIZE) : base(sim, size)
	    {
	        Road road;
	        for (int i = 0; i < size; i++)
	        {
	            road = new Road(sim);
	            elements.Add(road);
	        }
	    }
	}
}
