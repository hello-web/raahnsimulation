using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class EntityMap : Updateable
	{
        public enum XMLElement
        {
            MAP = 0,
            ROBOT = 1,
            ENTITY = 2,
            X = 3,
            Y = 4,
            ANGLE = 5,
            TEXTURE = 6,
            TYPE = 7
        }

        //EntityInfo is a class so it can be passed by reference.
        private class EntityInfo
        {
            public int textureVariation;
            public double x;
            public double y;
            public double angle;
            public Entity.EntityType type;
        }

		public const int UNIQUE_ROAD_COUNT = 2;
        public const string NULL_ELEMENT = "";
        public static readonly string[] XML_ELEMENTS = 
        {
            "Map", "Robot", "Entity", "X", "Y", "Angle", "Texture", "Type"
        };
        //The indicies correspond to their respective EntityType.
        public static readonly string[] ENTITY_TYPES = 
        {
            "Road"
        };

        //Textures
        private const int STRAIGHT = 0;
        private const int TURN = 1;
        private const int ENTITY_DEFAULT_TEXTURE_VARIATION = 0;
        private const double ENTITY_DEFAULT_X = 0.0;
        private const double ENTITY_DEFAULT_Y = 0.0;
        private const double ENTITY_DEFAULT_ANGLE = 0.0;
        private const Entity.EntityType ENTITY_DEFAULT_TYPE = Entity.EntityType.ROAD;

        private int layer;
        private bool loaded;
		private List<ColorableEntity> entities;
		//private StreamReader fileStr;
		private XmlReader xmlReader;
        private Simulator context;
        private State currentState;
        private QuadTree quadTree;
        private Car raahnCar;

	    public EntityMap(Simulator sim, int layerIndex, Car car, QuadTree tree)
	    {
            Construct(sim, layerIndex, car, tree);
	    }

	    public EntityMap(Simulator sim, int layerIndex, Car car, QuadTree tree, string fileName)
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

			xmlReader = XmlReader.Create(new StreamReader(fileName));

            bool mapHeaderFound = false;
            bool robotHeaderFound = false;
            bool entityHeaderFound = false;

            string currentElement = "";
            List<EntityInfo> newEntities = new List<EntityInfo>();

            try
            {
                while (xmlReader.Read())
                {
                    switch (xmlReader.NodeType)
                    {
                        case XmlNodeType.Element:
                        {
                            currentElement = xmlReader.Name;

                            if (currentElement.Equals(XML_ELEMENTS[(int)XMLElement.MAP]) && !mapHeaderFound)
                                mapHeaderFound = true;

                            if (!mapHeaderFound)
                                break;

                            if (currentElement.Equals(XML_ELEMENTS[(int)XMLElement.ROBOT]) && !robotHeaderFound && !entityHeaderFound)
                                robotHeaderFound = true;
                            else if (currentElement.Equals(XML_ELEMENTS[(int)XMLElement.ENTITY]) && !robotHeaderFound && !entityHeaderFound)
                            {
                                entityHeaderFound = true;
                                EntityInfo newEntityInfo = new EntityInfo();

                                //Initialize newEntityInfo with default info.
                                newEntityInfo.x = ENTITY_DEFAULT_X;
                                newEntityInfo.y = ENTITY_DEFAULT_Y;
                                newEntityInfo.angle = ENTITY_DEFAULT_ANGLE;
                                newEntityInfo.textureVariation = ENTITY_DEFAULT_TEXTURE_VARIATION;
                                newEntityInfo.type = ENTITY_DEFAULT_TYPE;

                                newEntities.Add(newEntityInfo);
                            }

                            break;
                        }
                        case XmlNodeType.Text:
                        {
                            if (!mapHeaderFound)
                                break;

                            if (!currentElement.Equals(NULL_ELEMENT) && xmlReader.Value.Length > 0)
                            {
                                if (robotHeaderFound)
                                    HandleCarAttribute(currentElement, xmlReader.Value);
                                else if (entityHeaderFound)
                                    HandleEntityAttribute(currentElement, xmlReader.Value, newEntities[newEntities.Count - 1]);
                            }

                            break;
                        }
                        case XmlNodeType.EndElement:
                        {
                            currentElement = NULL_ELEMENT;

                            if (xmlReader.Name.Equals(XML_ELEMENTS[(int)XMLElement.MAP]))
                                mapHeaderFound = false;

                            if (!mapHeaderFound)
                                break;

                            if (xmlReader.Name.Equals(XML_ELEMENTS[(int)XMLElement.ROBOT]))
                                robotHeaderFound = false;
                            else if (xmlReader.Name.Equals(XML_ELEMENTS[(int)XMLElement.ENTITY]))
                                entityHeaderFound = false;

                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(Utils.XML_READ_ERROR);
                Console.WriteLine(e.Message);
            }
            finally
            {
                xmlReader.Close();
            }

            for (int i = 0; i < newEntities.Count; i++)
            {
                switch (newEntities[i].type)
                {
                    case Entity.EntityType.ROAD:
                    {
                        Road road = new Road(context);

                        road.transformedWorldPos.x = newEntities[i].x;
                        road.transformedWorldPos.y = newEntities[i].y;
                        road.angle = newEntities[i].angle;
                        if (newEntities[i].textureVariation < UNIQUE_ROAD_COUNT)
                            road.SetTexture((TextureManager.TextureType)(newEntities[i].textureVariation + TextureManager.ROAD_INDEX_OFFSET));

                        entities.Add(road);

                        currentState.AddEntity(road, layer);

                        if (quadTree != null)
                            quadTree.AddEntity(road);

                        break;
                    }
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

        private void Construct(Simulator sim, int layerIndex, Car car, QuadTree tree)
        {
            loaded = false;

            entities = new List<ColorableEntity>();

            context = sim;

            currentState = context.GetState();
            layer = layerIndex;

            raahnCar = car;

            quadTree = tree;
        }

        private void HandleCarAttribute(string element, string attribute)
        {
            XMLElement elementIndex = 0;
            for (int i = 0; i < XML_ELEMENTS.Length; i++)
            {
                if (element.Equals(XML_ELEMENTS[i]))
                    elementIndex = (XMLElement)i;
            }

            //If an attribute is not set here, it just uses the default already set.
            switch (elementIndex)
            {
                case XMLElement.X:
                {
                    double.TryParse(attribute, out raahnCar.transformedWorldPos.x);
                    break;
                }
                case XMLElement.Y:
                {
                    double.TryParse(attribute, out raahnCar.transformedWorldPos.y);
                    break;
                }
                case XMLElement.ANGLE:
                {
                    double.TryParse(attribute, out raahnCar.angle);
                    break;
                }
            }
        }

        private void HandleEntityAttribute(string element, string attribute, EntityInfo info)
        {
            XMLElement elementIndex = 0;
            for (int i = 0; i < XML_ELEMENTS.Length; i++)
            {
                if (element.Equals(XML_ELEMENTS[i]))
                    elementIndex = (XMLElement)i;
            }

            //If an attribute is not set here, it just uses the default already set.
            switch (elementIndex)
            {
                case XMLElement.X:
                {
                    double.TryParse(attribute, out info.x);
                    break;
                }
                case XMLElement.Y:
                {
                    double.TryParse(attribute, out info.y);
                    break;
                }
                case XMLElement.ANGLE:
                {
                    double.TryParse(attribute, out info.angle);
                    break;
                }
                case XMLElement.TEXTURE:
                {
                    int.TryParse(attribute, out info.textureVariation);
                    //The index should never be less than zero for any entity.
                    if (info.textureVariation < 0)
                        info.textureVariation = 0;
                    break;
                }
                case XMLElement.TYPE:
                {
                    for (int i = 0; i < ENTITY_TYPES.Length; i++)
                    {
                        if (attribute.Equals(ENTITY_TYPES[i]))
                            info.type = (Entity.EntityType)i;
                    }
                    break;
                }
            }
       }
	}
}
