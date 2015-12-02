using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
	public class MapBuilder
	{
        public enum Mode
        {
            WALL = 0,
            POINT = 1,
            SELECT = 2
        }

        private const uint UNIQUE_ENTITIES = 2;
        private const int WALL_INDEX = 0;
        private const int POINT_INDEX = 1;
        private const int XML_INDENT_SPACE = 4;

		private const double FLAG_WIDTH = 200.0;
		private const double FLAG_HEIGHT = 220.0;
        private const double HOLLOW_CIRCLE_WIDTH = 80.0;
        private const double HOLLOW_CIRCLE_HEIGHT = 60.0;
        private const double SNAP_COLOR_R = 1.0;
        private const double SNAP_COLOR_G = 0.0;
        private const double SNAP_COLOR_B = 0.0;
        private const double SNAP_COLOR_A = 1.0;
        private const double SELECTED_R = 0.0;
        private const double SELECTED_G = 1.0;
        private const double SELECTED_B = 0.0;
        private const double SELECTED_A = 1.0;

        private uint layer;
        private bool hasSnappingPoint;
		private List<LinkedList<Entity>> entities;
        private Utils.Point2 snappingPoint;
        private Simulator context;
        private MapState currentState;
        private TextureManager texMan;
        //The entity being modified by the user.
        private Entity entityInUse;
        private Entity entitySelected;
        private WallPool wallPool;
        private PointPool pointPool;
		private Cursor cursor;
        private Camera camera;
		private Graphic flag;
        private Mesh quad;
        private Mode itemMode;

        public MapBuilder(Simulator sim, Cursor c, uint stateLayer)
	    {
            context = sim;

            currentState = (MapState)context.GetState();

            texMan = currentState.GetTexMan();
            quad = State.GetQuad();

            cursor = c;
            camera = currentState.GetCamera();
            entityInUse = null;
            entitySelected = null;

            layer = stateLayer;

            wallPool = new WallPool(context);
            pointPool = new PointPool(context);

            entities = new List<LinkedList<Entity>>();

            for (uint i = 0; i < UNIQUE_ENTITIES; i++)
                entities.Add(new LinkedList<Entity>());

            hasSnappingPoint = false;
            snappingPoint = new Utils.Point2();
            itemMode = Mode.WALL;

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

        public void SetMode(Mode mode)
        {
            //Do not switch modes while an entity is being added.
            if (entityInUse == null)
                itemMode = mode;
        }

        public void UpdateEvent(Event e)
        {
            if (e.type == Gdk.EventType.KeyPress)
            {
                //Check if the flag's state should be changed.
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
                else if (e.key == Gdk.Key.Delete)
                {
                    //Check if a wall should be deleted
                    if (entitySelected != null)
                    {
                        currentState.RemoveEntity(entitySelected);

                        switch (entitySelected.GetEntityType())
                        {
                            case Entity.EntityType.POINT:
                            {
                                entities[POINT_INDEX].Remove(entitySelected);
                                pointPool.Free((Point)entitySelected);
                                break;
                            }
                            case Entity.EntityType.WALL:
                            {
                                entities[WALL_INDEX].Remove(entitySelected);
                                wallPool.Free((Wall)entitySelected);
                                break;
                            }
                        }

                        entitySelected.ResetColor();
                        entitySelected = null;
                    }
                }
            }
            else if (e.type == Gdk.EventType.ButtonPress)
            {
                if (e.button == Utils.GTK_BUTTON_RIGHT)
                    ModeInitial();
            }
            else if (e.type == Gdk.EventType.MotionNotify)
                UpdateSnappingPoint();

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
                entities[WALL_INDEX].AddLast(wall);
                entityInUse = wall;
            }
            else
                Console.WriteLine(Utils.ENTITY_POOL_USED_UP, Entity.ENTITY_TYPE_STRINGS[(int)Entity.EntityType.WALL]);
        }

        private void AddPoint()
        {
            if (!pointPool.Empty())
            {
                Point point = pointPool.Alloc();

                point.SetPosition(cursor.GetTransformedX(), cursor.GetTransformedY() + cursor.GetHeight());

                currentState.AddEntity(point, layer);
                entities[POINT_INDEX].AddLast(point);
            }
            else
                Console.WriteLine(Utils.ENTITY_POOL_USED_UP, Entity.ENTITY_TYPE_STRINGS[(int)Entity.EntityType.POINT]);
        }

        private void RemoveEntityFromList(Entity entity, int listIndex)
        {
            entities[listIndex].Remove(entity);
            currentState.RemoveEntity(entity);
        }

        //Performs the initial functions of a mode.
        private void ModeInitial()
        {
            switch (itemMode)
            {
                case Mode.WALL:
                {
                    if (entityInUse == null)
                        AddWall();
                    else
                    {
                        if (hasSnappingPoint)
                        {
                            double xDiff = snappingPoint.x - entityInUse.GetTransformedX();
                            double yDiff = snappingPoint.y - entityInUse.GetTransformedY();

                            ((Wall)entityInUse).SetRelativeEndPoint(xDiff, yDiff);
                        }

                        entityInUse = null;
                    }

                    break;
                }
                case Mode.POINT:
                {
                    if (entityInUse == null)
                        AddPoint();

                    break;
                }
                case Mode.SELECT:
                {
                    //If there are no entities that can be selected do not continue.
                    if (entities[POINT_INDEX].Count == 0 && entities[WALL_INDEX].Count == 0)
                        break;

                    Utils.Rect compareRect = camera.TransformWorld(cursor.aabb.GetBounds());

                    //Check points first for a simple intersection.
                    foreach (Entity curPoint in entities[POINT_INDEX])
                    {
                        if (compareRect.Intersects(curPoint.GetTransformedX(), curPoint.GetTransformedY()))
                        {
                            if (entitySelected != null)
                                entitySelected.ResetColor();

                            entitySelected = curPoint;
                            entitySelected.SetColor(SELECTED_R, SELECTED_G, SELECTED_B, SELECTED_A);

                            //An entity was selected, do not continue.
                            return;
                        }
                    }

                    Utils.LineSegment compareLine = new Utils.LineSegment();

                    //Check the two lines diagonal to the cursor rather than every side for simplicity.
                    //First line.
                    compareLine.SetUp(new Utils.Point2(compareRect.left, compareRect.top),
                                      new Utils.Point2(compareRect.right, compareRect.bottom));

                    //Check walls for line segment collisions.
                    foreach (Wall curWall in entities[WALL_INDEX])
                    {
                        //If there are any intersection points, set the wall to be selected.
                        if (compareLine.Intersects(curWall.GetLineSegment()).Count > 0)
                        {
                            if (entitySelected != null)
                                entitySelected.ResetColor();

                            entitySelected = curWall;
                            entitySelected.SetColor(SELECTED_R, SELECTED_G, SELECTED_B, SELECTED_A);

                            return;
                        }
                    }

                    //Second line
                    compareLine.SetUp(new Utils.Point2(compareRect.left, compareRect.bottom),
                                      new Utils.Point2(compareRect.right, compareRect.top));

                    //Check walls for line segment collisions.
                    foreach (Wall curWall in entities[WALL_INDEX])
                    {
                        //If there are any intersection points, set the wall to be selected.
                        if (compareLine.Intersects(curWall.GetLineSegment()).Count > 0)
                        {
                            if (entitySelected != null)
                                entitySelected.ResetColor();

                            entitySelected = curWall;
                            entitySelected.SetColor(SELECTED_R, SELECTED_G, SELECTED_B, SELECTED_A);

                            return;
                        }
                    }

                    //No entity intersected. Unselect any entity selected.
                    if (entitySelected != null)
                    {
                        entitySelected.ResetColor();
                        entitySelected = null;
                    }

                    break;
                }
            }
        }

        private void UpdateSnappingPoint()
        {
            if (itemMode == Mode.WALL)
            {
                hasSnappingPoint = false;

                foreach (Entity curWall in entities[WALL_INDEX])
                {
                    //Do not use the points of the current wall for snapping.
                    //Only use walls.
                    if (curWall == entityInUse)
                        continue;

                    Utils.Rect compareRect = camera.TransformWorld(cursor.aabb.GetBounds());

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
        }

        private void UpdateEntityInUse(Event e)
        {
            switch (itemMode)
            {
                case Mode.WALL:
                {
                    if (e.type == Gdk.EventType.MotionNotify)
                    {
                        Utils.Rect compareRect = camera.TransformWorld(cursor.aabb.GetBounds());

                        double xDiff = compareRect.left - entityInUse.GetTransformedX();
                        double yDiff = compareRect.top - entityInUse.GetTransformedY();

                        ((Wall)entityInUse).SetRelativeEndPoint(xDiff, yDiff);
                    }

                    break;
                }
                case Mode.POINT:
                    break;
            }
        }
	}
}
