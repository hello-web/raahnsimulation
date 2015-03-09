using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
	public class MapBuilder
	{
        private const uint UNIQUE_ENTITIES = 1;
        private const int XML_INDENT_SPACE = 4;

		private const double FLAG_WIDTH = 192.0;
		private const double FLAG_HEIGHT = 216.0;

        private uint layer;
		private List<LinkedList<ColorableEntity>> entities;
        private Simulator context;
        private MapState currentState;
        //private WallPool wallPool;
		private Cursor cursor;
		private Graphic flag;

        public MapBuilder(Simulator sim, Cursor c, uint stateLayer)
	    {
            context = sim;
            currentState = (MapState)context.GetState();
            cursor = c;

            layer = stateLayer;

            //wallPool = new WallPool(context);

            entities = new List<LinkedList<ColorableEntity>>();

            for (uint i = 0; i < UNIQUE_ENTITIES; i++)
                entities.Add(new LinkedList<ColorableEntity>());

	        flag = new Graphic(context);
            flag.visible = false;
	        flag.SetTexture(TextureManager.TextureType.FLAG);
	        flag.SetWidth(FLAG_WIDTH);
	        flag.SetHeight(FLAG_HEIGHT);

            currentState.AddEntity(flag, layer + 1);
	    }

	    ~MapBuilder()
	    {
            //Don't bother freeing pool allocated elements 
            //as the pools will be destroyed.
            entities.Clear();
	    }

	    public void Update()
	    {

	    }

        public void UpdateEvent(Event e)
        {
            //Check if the flag's state should be changed.
            if (e.type == Gdk.EventType.KeyPress)
            {
                if (e.key == Gdk.Key.space)
                {
                    if (flag.visible)
                        flag.visible = false;
                    else
                    {
                        flag.transformedWorldPos.x = cursor.transformedWorldPos.x;
                        flag.transformedWorldPos.y = cursor.transformedWorldPos.y;
                        flag.visible = true;
                    }
                }
            }
        }

        public bool SaveMap(string file)
        {
            MapConfig mapConfig = new MapConfig();

            mapConfig.robotConfig = new CarConfig();

            uint entityCount = 0;

            for (int i = 0; i < entities.Count; i++)
                entityCount += (uint)entities[i].Count;

            mapConfig.entites = new EntityConfig[entityCount];

            for (int i = 0; i < entityCount; i++)
                mapConfig.entites[i] = new EntityConfig();

            mapConfig.robotConfig.x = flag.transformedWorldPos.x;
            mapConfig.robotConfig.y = flag.transformedWorldPos.y;
            //No angle saving, for now at least.
            mapConfig.robotConfig.angle = 0.0;

            int index = 0;

            for (int x = 0; x < entities.Count; x++)
            {
                foreach (ColorableEntity entity in entities[x])
                {
                    mapConfig.entites[index].x = entity.transformedWorldPos.x;
                    mapConfig.entites[index].y = entity.transformedWorldPos.y;
                    mapConfig.entites[index].angle = entity.angle;
                    mapConfig.entites[index].type = Entity.GetStringFromType(entity.GetEntityType());

                    index++;
                }
            }

            TextWriter configWriter = new StreamWriter(file);

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(MapConfig));
                serializer.Serialize(configWriter, mapConfig);
            }
            catch (Exception e)
            {
                Console.WriteLine(Utils.XML_WRITE_ERROR);
                Console.WriteLine(e.Message);

                return false;
            }
            finally
            {
                configWriter.Close();
            }

            return true;
        }

        public bool Intersects(double x, double y)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                foreach (Entity curEntity in entities[i])
                {
                    if (x > curEntity.aabb.GetBounds().left && x < curEntity.aabb.GetBounds().right)
                    {
                        if (y > curEntity.aabb.GetBounds().bottom && y < curEntity.aabb.GetBounds().top)
                            return true;
                    }
                }
            }
            return false;
        }

        public bool Intersects(Utils.Rect bounds)
        {
            for (int x = 0; x < entities.Count; x++)
            {
                foreach (Entity curEntity in entities[x])
                {
                    if (!(curEntity.aabb.GetBounds().left > bounds.right || curEntity.aabb.GetBounds().right < bounds.left
                    || curEntity.aabb.GetBounds().bottom > bounds.top || curEntity.aabb.GetBounds().top < bounds.bottom))
                        return true;
                }
            }
            return false;
        }

        private void RemoveEntityFromList(ColorableEntity entity, int listIndex)
        {
            entities[listIndex].Remove(entity);
            currentState.RemoveEntity(entity);
        }
	}
}
