using System.Collections.Generic;

namespace RaahnSimulation
{	
	public class EntityPool<T>
	{
		public const int DEFAULT_SIZE = 200;
		protected List<T> elements;

		protected EntityPool() {}

		protected EntityPool(Simulator sim, int size) 
        {
            elements = new List<T>();
        }

		~EntityPool()
		{
			elements.Clear();
		}

		public bool Empty()
		{
			if (elements.Count == 0)
				return true;
			else
				return false;
		}

		public T Alloc()
		{
			T returnElement = default(T);
			if (elements.Count > 0)
			{
				returnElement = elements[elements.Count - 1];
				elements.RemoveAt(elements.Count - 1);
			}
			return returnElement;
		}

		public void Free(T element)
		{
            if (element != null)
			    elements.Add(element);
		}
	}
}