using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class RoadMap : Updateable
	{
		public static readonly float[] ROAD_WIDTH_PERCENTAGES = { RWP0, RWP1 };
		public static readonly float[] ROAD_HEIGHT_PERCENTAGES = { RHP0, RHP1 };

		public const int UNIQUE_ROAD_COUNT = 2;

		//Textures
		private const int STRAIGHT = 0;
		private const int TURN = 1;

        private const float RWP0 = 0.266666667f * 0.8f;
        private const float RWP1 = 0.20625f * 0.8f;
        private const float RHP0 = 0.205925926f * 0.8f;
        private const float RHP1 = 0.293333333f * 0.8f;

        private int layer;
		private List<Road> roads;
		private StreamReader fileStr;
        private Simulator context;
        private State currentState;
        private QuadTree quadTree;

	    public RoadMap(Simulator sim, int layerIndex)
	    {
            Construct(sim, layerIndex);
	    }

	    public RoadMap(Simulator sim, int layerIndex, QuadTree tree, string fileName)
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
            roads = new List<Road>();
            context = sim;
            currentState = context.GetState();
            layer = layerIndex;
        }

	    ~RoadMap()
	    {
	        while (roads.Count > 0)
	        {
	            roads.RemoveAt(roads.Count - 1);
	        }
	    }

	    private bool InitFromFile(string fileName)
	    {
	        Road newRoad;
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
		            int road = 0;
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
                                    int.TryParse(buffer.Substring(stringPos, i - stringPos), NumberStyles.Float, Utils.EN_US, out road);
		                            break;
		                        }
		                    }
		                    stringPos = i + 1;
		                    sepsFound++;
		                }
		            }

		            //To account for out of range input from causing index out of bounds errors
		            if (road + 1 > UNIQUE_ROAD_COUNT)
		                return false;

		            newRoad = new Road(context);
		            newRoad.worldPos.x = (float)context.GetWindowWidth() * x;
		            newRoad.worldPos.y = (float)context.GetWindowHeight() * y;
		            newRoad.SetTexture((TextureManager.TextureType)(road + TextureManager.ROAD_INDEX_OFFSET));
		            newRoad.angle = angle;
		            newRoad.SetWidth((float)context.GetWindowWidth() * ROAD_WIDTH_PERCENTAGES[road]);
		            newRoad.SetHeight((float)context.GetWindowHeight() * ROAD_HEIGHT_PERCENTAGES[road]);
                    newRoad.Update();
		            roads.Add(newRoad);

                    currentState.AddEntity(newRoad, layer);
                    if (quadTree != null)
                        quadTree.AddEntity(newRoad);
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
