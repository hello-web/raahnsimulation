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
        private const uint WALL_INDEX = 0;
        private const int XML_INDENT_SPACE = 4;

		private const double FLAG_WIDTH = 200.0;
		private const double FLAG_HEIGHT = 220.0;
        private const double HOLLOW_CIRCLE_WIDTH = 80.0;
        private const double HOLLOW_CIRCLE_HEIGHT = 60.0;
        private const double SNAP_COLOR_R = 1.0;
        private const double SNAP_COLOR_G = 0.0;
        private const double SNAP_COLOR_B = 0.0;
        private const double SNAP_COLOR_A = 1.0;

        private uint layer;
        private bool hasSnappingPoint;
		private List<LinkedList<Entity>> entities;
        private Utils.Point2 snappingPoint;
        private Simulator context;
        private MapState currentState;
        private TextureManager texMan;
        //The entity being modified by the user.
        private Entity entityInUse;
        private WallPool wallPool;
		private Cursor cursor;
        private Camera camera;
		private Graphic flag;
        private Mesh quad;

        public MapBuilder(Simulator sim, Cursor c, uint stateLayer)
	    {
            context = sim;

            currentState = (MapState)context.GetState();

            texMan = currentState.GetTexMan();
            quad = State.GetQuad();

            cursor = c;
            camera = currentState.GetCamera();
            entityInUse = null;

            layer = stateLayer;

            wallPool = new WallPool(context);

            entities = new List<LinkedList<Entity>>();

            for (uint i = 0; i < UNIQUE_ENTITIES; i++)
                entities.Add(new LinkedList<Entity>());

            hasSnappingPoint = false;
            snappingPoint = new Utils.Point2();

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
            //as the pools will be destroyed automatically.
            entities.Clear();
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
                        flag.SetPosition(cursor.GetTransformedX(), cursor.GetTransformedY());
                        flag.visible = true;
                    }
                }
            }
            else if (e.type == Gdk.EventType.ButtonPress)
            {
                if (e.button == Utils.GTK_BUTTON_RIGHT)
                {
                    //Add a wall if there is no entity is use.
                    //If there is and it is a wall, stop updating the wall based on the cursor.
                    if (entityInUse == null)
                        AddWall();
                    else if (entityInUse.GetEntityType() == Entity.EntityType.WALL)
                    {
                        if (hasSnappingPoint)
                        {
                            double xDiff = snappingPoint.x - entityInUse.GetTransformedX();
                            double yDiff = snappingPoint.y - entityInUse.GetTransformedY();

                            ((Wall)entityInUse).SetRelativeEndPoint(xDiff, yDiff);
                        }
                        
                        entityInUse = null;
                    }
                }
            }
            else if (e.type == Gdk.EventType.MotionNotify)
            {
                hasSnappingPoint = false;

                foreach (Entity curWall in entities[(int)WALL_INDEX])
                {
                    //Do not use the points of the current wall for snapping.
                    if (curWall == entityInUse)
                        continue;

                    Utils.Rect compareRect = camera.TransformWorld(cursor.aabb.GetBounds());

                    //Center around the top left of the cursor.
                    double horizontalOffset = cursor.GetWidth() / 2.0;
                    double verticalOffset = cursor.GetHeight() / 2.0;

                    compareRect.left -= horizontalOffset;
                    compareRect.right -= horizontalOffset;
                    compareRect.bottom += verticalOffset;
                    compareRect.top += verticalOffset;

                    double startX = curWall.GetTransformedX();
                    double startY = curWall.GetTransformedY();
                    Utils.Point2 endPoint = ((Wall)curWall).GetEndPoint();

                    //After the first snapping point is found, stop.
                    if (compareRect.Intersects(startX, startY))
                    {
                        snappingPoint.x = startX;
                        snappingPoint.y = startY;
                        hasSnappingPoint = true;
                        break;
                    }
                    else if (compareRect.Intersects(endPoint.x, endPoint.y))
                    {
                        snappingPoint.x = endPoint.x;
                        snappingPoint.y = endPoint.y;
                        hasSnappingPoint = true;
                        break;
                    }
                }
            }

            if (entityInUse != null)
                UpdateEntityInUse(e);
        }

        public void Draw()
        {
            if (hasSnappingPoint)
            {
                //Render a textured quad.
                quad.MakeCurrent();

                GL.Color4(SNAP_COLOR_R, SNAP_COLOR_G, SNAP_COLOR_G, SNAP_COLOR_A);

                texMan.SetTexture(TextureManager.TextureType.HOLLOW_CIRCLE);

                GL.PushMatrix();

                double circleCenterX = snappingPoint.x - (HOLLOW_CIRCLE_WIDTH / 2.0);
                double circleCenterY = snappingPoint.y - (HOLLOW_CIRCLE_HEIGHT / 2.0);

                GL.Translate(circleCenterX, circleCenterY, Utils.DISCARD_Z_POS);
                GL.Scale(HOLLOW_CIRCLE_WIDTH, HOLLOW_CIRCLE_HEIGHT, Utils.DISCARD_Z_SCALE);

                GL.DrawElements(quad.GetRenderMode(), quad.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);

                GL.PopMatrix();

                GL.Color4(1.0, 1.0, 1.0, 1.0);
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

            mapConfig.robotConfig.x = flag.GetTransformedX();
            mapConfig.robotConfig.y = flag.GetTransformedY();
            //No angle saving, for now at least.
            mapConfig.robotConfig.angle = 0.0;

            int index = 0;

            for (int x = 0; x < entities.Count; x++)
            {
                foreach (Entity entity in entities[x])
                {
                    mapConfig.entites[index].x = entity.GetTransformedX();
                    mapConfig.entites[index].y = entity.GetTransformedY();
                    mapConfig.entites[index].angle = entity.angle;
                    Entity.EntityType eType = entity.GetEntityType();
                    mapConfig.entites[index].type = Entity.GetStringFromType(eType);

                    if (eType == Entity.EntityType.WALL)
                    {
                        Wall curWall = (Wall)entity;

                        mapConfig.entites[index].relX = curWall.GetRelativeX();
                        mapConfig.entites[index].relY = curWall.GetRelativeY();
                    }

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

        private void AddWall()
        {
            if (!wallPool.Empty())
            {
                Wall wall = wallPool.Alloc();

                if (hasSnappingPoint)
                    wall.SetPosition(snappingPoint.x, snappingPoint.y);
                else
                    wall.SetPosition(cursor.GetTransformedX(), cursor.GetTransformedY() + cursor.GetHeight());

                wall.SetRelativeEndPoint(0.0, 0.0);

                currentState.AddEntity(wall, layer);
                entities[(int)WALL_INDEX].AddLast(wall);
                entityInUse = wall;
            }
            else
                Console.WriteLine(Utils.ENTITY_POOL_USED_UP, Entity.ENTITY_TYPE_STRINGS[(int)Entity.EntityType.WALL]);
        }

        private void RemoveEntityFromList(Entity entity, int listIndex)
        {
            entities[listIndex].Remove(entity);
            currentState.RemoveEntity(entity);
        }

        private void UpdateEntityInUse(Event e)
        {
            switch (entityInUse.GetEntityType())
            {
                case Entity.EntityType.WALL:
                {
                    if (e.type == Gdk.EventType.MotionNotify)
                    {
                        double xDiff = cursor.GetTransformedX() - entityInUse.GetTransformedX();
                        double yDiff = cursor.GetTransformedY() + cursor.GetHeight() - entityInUse.GetTransformedY();

                        ((Wall)entityInUse).SetRelativeEndPoint(xDiff, yDiff);
                    }

                    break;
                }
            }
        }
	}
}
