using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;
using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
    [XmlRoot("Map")]
    public class MapConfig
    {
        [XmlElement("Robot")]
        public CarConfig robotConfig;

        [XmlElement("Entity")]
        public EntityConfig[] entites;
    }

	public class EntityMap
    {
		public const int UNIQUE_ROAD_COUNT = 2;

        //Textures
        private const int STRAIGHT = 0;
        private const int TURN = 1;
        private const int ENTITY_DEFAULT_TEXTURE_VARIATION = 0;
        private const double ENTITY_DEFAULT_X = 0.0;
        private const double ENTITY_DEFAULT_Y = 0.0;
        private const double ENTITY_DEFAULT_ANGLE = 0.0;

        private uint layer;
        private bool loaded;
        private double defaultCarX;
        private double defaultCarY;
        private double defaultCarAngle;
		private List<Entity> entities;
        private Simulator context;
        private State currentState;
        private QuadTree quadTree;
        private Car raahnCar;

	    public EntityMap(Simulator sim, uint layerIndex, Car car, QuadTree tree)
	    {
            Construct(sim, layerIndex, car, tree);
	    }

	    public EntityMap(Simulator sim, uint layerIndex, Car car, QuadTree tree, string fileName)
	    {
            Construct(sim, layerIndex, car, tree);

	        Load(fileName);
	    }

        ~EntityMap()
        {
            while (entities.Count > 0)
            {
                entities.RemoveAt(entities.Count - 1);
            }
        }

        public void SetQuadTree(QuadTree tree)
        {
            quadTree = tree;
        }

        public bool Load(string fileName)
        {
            if (loaded)
            {
                Console.WriteLine(Utils.MAP_ALREADY_LOADED);
                return false;
            }

            if (!File.Exists(fileName))
            {
                Console.WriteLine(string.Format(Utils.FILE_NOT_FOUND, fileName));
                return false;
            }

            TextReader configReader = new StreamReader(fileName);
            MapConfig mapConfig = null;

            try
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(MapConfig));
                mapConfig = (MapConfig)deserializer.Deserialize(configReader);
            }
            catch (Exception e)
            {
                Console.WriteLine(Utils.XML_READ_ERROR);
                Console.WriteLine(Utils.MAP_LOAD_ERROR);
                Console.WriteLine(e.Message);

                return false;
            }
            finally
            {
                configReader.Close();
            }

            if (mapConfig.robotConfig != null)
            {
                defaultCarX = mapConfig.robotConfig.x;
                defaultCarY = mapConfig.robotConfig.y;
                defaultCarAngle = mapConfig.robotConfig.angle;

                raahnCar.SetPosition(defaultCarX, defaultCarY);
                raahnCar.angle = defaultCarAngle;
            }

            if (mapConfig.entites != null)
            {
                for (int i = 0; i < mapConfig.entites.Length; i++)
                {
                    Entity newEntity = null;
                    Entity.EntityType type = Entity.EntityType.NONE;

                    if (mapConfig.entites[i].type != null)
                        type = Entity.GetTypeFromString(mapConfig.entites[i].type);

                    switch (type)
                    {
                        case Entity.EntityType.WALL:
                        {
                            newEntity = new Wall(context);
                            Wall newWall = (Wall)newEntity;

                            newWall.SetRelativeEndPoint(mapConfig.entites[i].relX, mapConfig.entites[i].relY);

                            break;
                        }
                    }

                    if (newEntity == null)
                        continue;

                    newEntity.SetPosition(mapConfig.entites[i].x, mapConfig.entites[i].y);
                    newEntity.angle = mapConfig.entites[i].angle;

                    entities.Add(newEntity);

                    currentState.AddEntity(newEntity, layer);

                    if (quadTree != null)
                        quadTree.AddEntity(newEntity);
                }
            }

            loaded = true;
            
            return true;
        }

        public void Update()
        {

        }

        public void UpdateEvent(Event e)
        {

        }

        public double GetDefaultCarX()
        {
            return defaultCarX;
        }

        public double GetDefaultAngle()
        {
            return defaultCarAngle;
        }

        public double GetDefaultCarY()
        {
            return defaultCarY;
        }

        private void Construct(Simulator sim, uint layerIndex, Car car, QuadTree tree)
        {
            loaded = false;

            defaultCarX = 0.0;
            defaultCarY = 0.0;
            defaultCarAngle = 0.0;

            entities = new List<Entity>();

            context = sim;

            currentState = context.GetState();
            layer = layerIndex;

            raahnCar = car;

            quadTree = tree;
        }
	}
}
