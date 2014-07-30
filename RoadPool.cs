namespace RaahnSimulation
{
	class RoadPool : EntityPool<Road>
	{
	    public RoadPool(Simulator sim, int size = ENTITY_POOL_DEFAULT_SIZE) : base(sim, size)
	    {
	        Road road;
	        for (int i = 0; i < size; i++)
	        {
	            road = new Road(sim);
	            elements.AddLast(road);
	        }
	    }
	}
}
