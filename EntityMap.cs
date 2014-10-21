using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class EntityMap : Updateable
	{
		public const int UNIQUE_ROAD_COUNT = 2;

		//Textures
		private const int STRAIGHT = 0;
		private const int TURN = 1;

        private int layer;
		private List<ColorableEntity> entities;
		private StreamReader fileStr;
        private Simulator context;
        private State currentState;
        private QuadTree quadTree;

	    public EntityMap(Simulator sim, int layerIndex)
	    {
            Construct(sim, layerIndex);
	    }

	    public EntityMap(Simulator sim, int layerIndex, QuadTree tree, string fileName)
	    {
            Construct(sim, layerIndex);

            quadTree = tree;

	        InitFromFile(fileName);
	    }

        public void SetQuadTree(QuadTree tree)
        {
            quadTree = tree;
        }

        public bool Load(string fileName)
        {
            if (!InitFromFile(fileName))
                return false;
            return true;
        }

        public void Update()
        {

        }

        public void UpdateEvent(Event e)
        {

        }

        private void Construct(Simulator sim, int layerIndex)
        {
            entities = new List<ColorableEntity>();

            context = sim;

            currentState = context.GetState();
            layer = layerIndex;
        }

	    ~EntityMap()
	    {
	        while (entities.Count > 0)
	        {
	            entities.RemoveAt(entities.Count - 1);
	        }
	    }

	    private bool InitFromFile(string fileName)
	    {
	        Road newEntity;
	        string buffer;

			if (!File.Exists(fileName))
				return false;

			fileStr = null;
			try
			{
				fileStr = new StreamReader(fileName);

		        while ((buffer = fileStr.ReadLine()) != null)
		        {
		            if (buffer.Length < 1)
		                return false;

		            int stringPos = 0;
		            int sepsFound = 0;
		            int entityVariation = 0;
		            float x = 0.0f;
		            float y = 0.0f;
		            float angle = 0.0f;

		            for (int i = 0; i < buffer.Length; i++)
		            {
		                if (buffer[i] == Utils.FILE_COMMENT)
		                    break;
		                else if (buffer[i] == Utils.FILE_VALUE_SEPERATOR)
		                {
		                    switch (sepsFound)
		                    {
		                        case 0:
		                        {
		                            float.TryParse(buffer.Substring(stringPos, i - stringPos), NumberStyles.Float, Utils.EN_US, out x);
		                            break;
		                        }
		                        case 1:
		                        {
                                    float.TryParse(buffer.Substring(stringPos, i - stringPos), NumberStyles.Float, Utils.EN_US, out y);
		                            break;
		                        }
		                        case 2:
		                        {
                                    float.TryParse(buffer.Substring(stringPos, i - stringPos), NumberStyles.Float, Utils.EN_US, out angle);
		                            break;
		                        }
		                        case 3:
		                        {
									int.TryParse(buffer.Substring(stringPos, i - stringPos), NumberStyles.Float, Utils.EN_US, out entityVariation);
		                            break;
		                        }
		                    }
		                    stringPos = i + 1;
		                    sepsFound++;
		                }
		            }

					//Default to only using roads for now.
		            //To account for out of range input from causing index out of bounds errors
					if (entityVariation + 1 > UNIQUE_ROAD_COUNT)
		                return false;

		            newEntity = new Road(context);
		            newEntity.worldPos.x = (float)context.GetWindowWidth() * x;
		            newEntity.worldPos.y = (float)context.GetWindowHeight() * y;
					newEntity.SetTexture((TextureManager.TextureType)(entityVariation + TextureManager.ROAD_INDEX_OFFSET));
		            newEntity.angle = angle;
                    newEntity.Update();

		            entities.Add(newEntity);

                    currentState.AddEntity(newEntity, layer);

                    if (quadTree != null)
                        quadTree.AddEntity(newEntity);
		        }
			}
			finally 
			{
                fileStr.Close();
			}
	        return true;
	    }
	}
}
