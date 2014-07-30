using System.Collections.Generic;

namespace RaahnSimulation
{	
	public class EntityPool<T>
	{
		public const int ENTITY_POOL_DEFAULT_SIZE = 100;
		protected LinkedList<T> elements;

		protected EntityPool() {}

		protected EntityPool(Simulator sim, int size) 
        {
            elements = new LinkedList<T>();
        }

		~EntityPool()
		{
			elements.Clear();
		}

		public bool Empty()
		{
			if (elements.Count < 0)
				return true;
			else
				return false;
		}

		public T Alloc()
		{
			T returnElement = default(T);
			if (elements.Count > 0)
			{
				returnElement = elements.Last.Value;
				elements.RemoveLast();
			}
			return returnElement;
		}

		public void Free(T element)
		{
			elements.Remove(element);
		}
	}
}