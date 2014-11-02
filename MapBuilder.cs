using System;
using System.Collections.Generic;
using System.Xml;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class MapBuilder : Updateable
	{
        private const uint SNAPPING_ANGLES_COUNT = 4;
        private const uint UNIQUE_ENTITIES = 1;
        private const int XML_INDENT_SPACE = 4;

		private const double FLAG_WIDTH = 192.0;
		private const double FLAG_HEIGHT = 216.0;
        private const double SNAPPING_ANGLE_BOUNDS = 7.5;

        private static readonly double[] SNAPPING_ANGLES = { 0.0, 90.0, 180.0, 270.0 };

        private int layer;
        private bool trashHovering;
        private double floatingExactAngle;
		private List<LinkedList<ColorableEntity>> entities;
        private Simulator context;
        private State currentState;
		private RoadPool roadPool;
		private Cursor cursor;
		private Camera cam;
		private EntityPanel entityPanel;
		private ColorableEntity entityFloating;
		private Graphic flag;
		private Utils.Vector2 dist;
        private Utils.Vector2 entitySnappingDist;

        public MapBuilder(Simulator sim, Cursor c, Camera camera, EntityPanel panel, int stateLayer)
	    {
            context = sim;
            currentState = context.GetState();
            trashHovering = false;

            layer = stateLayer;

	        roadPool = new RoadPool(context);
            entitySnappingDist = new Utils.Vector2(0.0, 0.0);

            entities = new List<LinkedList<ColorableEntity>>();

            for (uint i = 0; i < UNIQUE_ENTITIES; i++)
                entities.Add(new LinkedList<ColorableEntity>());

	        flag = new Graphic(context);
            flag.visible = false;
	        flag.SetTexture(TextureManager.TextureType.FLAG);
	        flag.SetWidth(FLAG_WIDTH);
	        flag.SetHeight(FLAG_HEIGHT);

	        cursor = c;
	        cam = camera;
	        entityPanel = panel;
	        entityFloating = null;

            floatingExactAngle = 0.0;
            dist = new Utils.Vector2(0.0, 0.0);

            currentState.AddEntity(flag, stateLayer + 1);
	    }

	    ~MapBuilder()
	    {
	        while (entities[0].Count > 0)
	        {
                int lastListIndex = entities.Count - 1;
                roadPool.Free((Road)entities[lastListIndex].Last.Value);
                entities[0].RemoveLast();
	        }
	    }

	    public void Update()
	    {
	        if (entityFloating != null)
	            UpdateEntityFloating();

            if (entityFloating != null)
            {
                if (entityFloating.Intersects(cam.TransformWorld(entityPanel.GetTrash().aabb.GetBounds())))
                {
                    if (!trashHovering)
                    {
                        //Turn the trash red when hovering.
                        entityPanel.GetTrash().SetColor(1.0, 0.0, 0.0, 1.0);
                        trashHovering = true;
                    }
                }
                else
                {
                    //Set the trash can back to black.
                    entityPanel.GetTrash().SetColor(0.0, 0.0, 0.0, 1.0);
                    trashHovering = false;
                }
            }
	    }

        public void UpdateEvent(Event e)
        {
            if (e.Type == EventType.KeyPressed && e.Key.Code == Keyboard.Key.Space)
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

            if (!((MapState)currentState).GetPanning())
            {
                if (e.Type == EventType.MouseButtonPressed && e.MouseButton.Button == Mouse.Button.Left)
                {
                    //Check for new intersections.
                    entityPanel.Update();

                    int selectedEntity = entityPanel.GetSelectedEntity();
                    if (selectedEntity != -1)
                    {
                        AddEntity(selectedEntity);
                        dist = entityPanel.GetDist(cursor.transformedWorldPos.x, cursor.transformedWorldPos.y, cam);
                        UpdateEntityFloating();
                    }
                    else
                    {
                        bool shouldBreak = false;

                        for (int x = entities.Count - 1; x >= 0; x--)
                        {
                            foreach (ColorableEntity curEntity in entities[x])
                            {
                                //The cursor's bounds are in window coordinates, we need world coordinates.
                                Utils.Rect comparisonRect;
                                comparisonRect = cam.TransformWorld(cursor.aabb.GetBounds());

                                if (curEntity.Intersects(comparisonRect) && Mouse.IsButtonPressed(Mouse.Button.Left))
                                {
                                    entityFloating = curEntity;
                                    entityFloating.SetColor(0.0, 0.0, 1.0, 0.85);
                                    currentState.ChangeLayer(entityFloating, currentState.GetTopLayerIndex() - 1);

                                    cursor.Update();

                                    dist.x = cursor.transformedWorldPos.x - curEntity.transformedWorldPos.x;
                                    dist.y = cursor.transformedWorldPos.y - curEntity.transformedWorldPos.y;

                                    floatingExactAngle = entityFloating.angle;

                                    UpdateEntityFloating();
                                    shouldBreak = true;
                                }
                                if (shouldBreak)
                                    break;
                            }
                            if (shouldBreak)
                                break;
                        }
                    }
                }
            }

            if (entityFloating != null)
            {
                if (e.Type == EventType.MouseButtonReleased && e.MouseButton.Button == Mouse.Button.Left)
                {
                    if (trashHovering)
                    {
                        RemoveEntityFromList(entityFloating, 0);
                        roadPool.Free((Road)entityFloating);
                        entityPanel.GetTrash().SetColor(0.0, 0.0, 0.0, 1.0);
                        trashHovering = false;
                    }
                    else
                    {
                        //Change the entitiy's color back to its original upon dropping it.
                        entityFloating.SetColor(1.0, 1.0, 1.0, 1.0);
                        currentState.ChangeLayer(entityFloating, layer);
                    }
                    entityFloating = null;
                }
            }
        }

        public void SaveMap()
        {
            XmlTextWriter writer = new XmlTextWriter(Utils.DEFAULT_SAVE_FILE, System.Text.Encoding.UTF8);

            writer.WriteStartDocument(true);
            writer.Indentation = XML_INDENT_SPACE;
            writer.Formatting = Formatting.Indented;

            //Write <Map>
            writer.WriteStartElement(EntityMap.XML_ELEMENTS[(int)EntityMap.XMLElement.MAP]);

            //Write <Robot> and its attributes
            writer.WriteStartElement(EntityMap.XML_ELEMENTS[(int)EntityMap.XMLElement.ROBOT]);

            writer.WriteStartElement(EntityMap.XML_ELEMENTS[(int)EntityMap.XMLElement.X]);
            writer.WriteString(flag.transformedWorldPos.x.ToString());
            writer.WriteEndElement();

            writer.WriteStartElement(EntityMap.XML_ELEMENTS[(int)EntityMap.XMLElement.Y]);
            writer.WriteString(flag.transformedWorldPos.y.ToString());
            writer.WriteEndElement();

            //Do not write angle because map builder currently does not have an angle value for the robot.

            //End </Robot>
            writer.WriteEndElement();

            for (int i = 0; i < entities.Count; i++)
            {
                foreach (ColorableEntity entity in entities[i])
                {
                    //Write <Entity>
                    writer.WriteStartElement(EntityMap.XML_ELEMENTS[(int)EntityMap.XMLElement.ENTITY]);

                    writer.WriteStartElement(EntityMap.XML_ELEMENTS[(int)EntityMap.XMLElement.X]);
                    writer.WriteString(entity.transformedWorldPos.x.ToString());
                    writer.WriteEndElement();

                    writer.WriteStartElement(EntityMap.XML_ELEMENTS[(int)EntityMap.XMLElement.Y]);
                    writer.WriteString(entity.transformedWorldPos.y.ToString());
                    writer.WriteEndElement();

                    writer.WriteStartElement(EntityMap.XML_ELEMENTS[(int)EntityMap.XMLElement.ANGLE]);
                    writer.WriteString(entity.angle.ToString());
                    writer.WriteEndElement();

                    writer.WriteStartElement(EntityMap.XML_ELEMENTS[(int)EntityMap.XMLElement.TYPE]);
                    writer.WriteString(EntityMap.ENTITY_TYPES[(int)entity.GetEntityType()]);
                    writer.WriteEndElement();

                    if (entity.GetEntityType() == Entity.EntityType.ROAD)
                    {
                        writer.WriteStartElement(EntityMap.XML_ELEMENTS[(int)EntityMap.XMLElement.TEXTURE]);
                        int saveTexture = (int)(entity.GetTexture() - TextureManager.ROAD_INDEX_OFFSET);
                        writer.WriteString(saveTexture.ToString());
                        writer.WriteEndElement();
                    }

                    //End </Entity>
                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();

            writer.WriteEndDocument();

            writer.Close();
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

        public bool Floating()
        {
            if (entityFloating != null)
                return true;
            else
                return false;
        }

        private void AddEntity(int itemIndex)
        {
            if (!roadPool.Empty())
            {
                Road newRoad = roadPool.Alloc();
                newRoad.SetTexture((TextureManager.TextureType)(itemIndex + TextureManager.ROAD_INDEX_OFFSET));
                newRoad.SetColor(0.0, 0.0, 1.0, 0.85);

                AddEntityToList(newRoad, 0);

                entityFloating = newRoad;
                entitySnappingDist.x = newRoad.GetWidth() / 4.0;
                entitySnappingDist.y = newRoad.GetHeight() / 4.0;
                newRoad.angle = 0.0;
                floatingExactAngle = newRoad.angle;
            }
        }

        private void AddEntityToList(ColorableEntity entity, int listIndex)
        {
            entities[listIndex].AddLast(entity);
            currentState.AddEntity(entity, currentState.GetTopLayerIndex() - 1);
        }

        private void RemoveEntityFromList(ColorableEntity entity, int listIndex)
        {
            entities[listIndex].Remove(entity);
            currentState.RemoveEntity(entity);
        }

        private void UpdateEntityFloating()
        {
            //Make sure the mouse is in bounds.
            if (cursor.worldPos.x < 0 || cursor.worldPos.y < 0
            || cursor.worldPos.x > Simulator.WORLD_WINDOW_WIDTH || cursor.worldPos.y > Simulator.WORLD_WINDOW_HEIGHT)
            {
                //Change the entitiy's color back to its original upon dropping it.
                entityFloating.SetColor(1.0, 1.0, 1.0, 1.0);
                currentState.ChangeLayer(entityFloating, layer);
                entityFloating = null;
                return;
            }

            Utils.Vector2 mousePosf = new Utils.Vector2(cursor.transformedWorldPos.x - dist.x, cursor.transformedWorldPos.y - dist.y);
            UpdatePosition(mousePosf);

            UpdateAngle();
        }

        private void UpdatePosition(Utils.Vector2 mousetransformedWorldPos)
        {
            entityFloating.transformedWorldPos.x = mousetransformedWorldPos.x;
            entityFloating.transformedWorldPos.y = mousetransformedWorldPos.y;
            entityFloating.Update();

            int boundsUsageX = 0;
            int boundsUsageY = 0;
            //Iniitialized to the first entity to make sure they are never null.
            Entity closestEntityX = entities[0].First.Value;
            Entity closestEntityY = entities[0].First.Value;

            Utils.Vector2 shortestXYDist = new Utils.Vector2(entitySnappingDist.x, entitySnappingDist.y);
            List<double> xDistances = new List<double>();
            List<double> yDistances = new List<double>();

            //Look for the closest entity horizontally and vertically.
            for (int x = 0; x < entities.Count; x++)
            {
                foreach (Entity curEntity in entities[x])
                {
                    if (entityFloating == curEntity)
                        continue;

                    xDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().left - curEntity.aabb.GetBounds().left));
                    xDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().left - curEntity.aabb.GetBounds().right));
                    xDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().right - curEntity.aabb.GetBounds().left));
                    xDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().right - curEntity.aabb.GetBounds().right));

                    yDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().bottom - curEntity.aabb.GetBounds().bottom));
                    yDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().bottom - curEntity.aabb.GetBounds().top));
                    yDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().top - curEntity.aabb.GetBounds().bottom));
                    yDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().top - curEntity.aabb.GetBounds().top));

                    //Find the shortest distance to the current entity and use it if it is shorter than shortest.
                    for (int i = 0; i < xDistances.Count; i++)
                    {
                        if (xDistances[i] < shortestXYDist.x)
                        {
                            shortestXYDist.x = xDistances[i];
                            closestEntityX = curEntity;
                            boundsUsageX = i;
                        }
                    }

                    for (int i = 0; i < yDistances.Count; i++)
                    {
                        if (yDistances[i] < shortestXYDist.y)
                        {
                            shortestXYDist.y = yDistances[i];
                            closestEntityY = curEntity;
                            boundsUsageY = i;
                        }
                    }

                    xDistances.Clear();
                    yDistances.Clear();
                }
            }

            if (shortestXYDist.x < entitySnappingDist.x && shortestXYDist.y < entitySnappingDist.y)
            {
                Utils.Rect floatingBounds = entityFloating.aabb.GetBounds();
                Utils.Rect entitySnappingBounds = closestEntityX.aabb.GetBounds();
                double distanceToBound = 0.0;

                switch (boundsUsageX)
                {
                    //Snap left aabb to left aabb.
                    case 0:
                    {
                        distanceToBound = entityFloating.transformedWorldPos.x - floatingBounds.left;
                        entityFloating.transformedWorldPos.x = entitySnappingBounds.left + distanceToBound;
                        break;
                    }
                    //Snap left aabb to right aabb.
                    case 1:
                    {
                        distanceToBound = entityFloating.transformedWorldPos.x - floatingBounds.left;
                        entityFloating.transformedWorldPos.x = entitySnappingBounds.right + distanceToBound;
                        break;
                    }
                    //Snap right aabb to left aabb.
                    case 2:
                    {
                        distanceToBound = floatingBounds.right - entityFloating.transformedWorldPos.x;
                        entityFloating.transformedWorldPos.x = entitySnappingBounds.left - distanceToBound;
                        break;
                    }
                    //Snap right aabb to right aabb.
                    case 3:
                    {
                        distanceToBound = floatingBounds.right - entityFloating.transformedWorldPos.x;
                        entityFloating.transformedWorldPos.x = entitySnappingBounds.right - distanceToBound;
                        break;
                    }
                }

                entitySnappingBounds = closestEntityY.aabb.GetBounds();

                switch (boundsUsageY)
                {
                    //Snap bottom aabb to left bottom.
                    case 0:
                    {
                        distanceToBound = entityFloating.transformedWorldPos.y - floatingBounds.bottom;
                        entityFloating.transformedWorldPos.y = entitySnappingBounds.bottom + distanceToBound;
                        break;
                    }
                    //Snap bottom aabb to top aabb.
                    case 1:
                    {
                        distanceToBound = entityFloating.transformedWorldPos.y - floatingBounds.bottom;
                        entityFloating.transformedWorldPos.y = entitySnappingBounds.top + distanceToBound;
                        break;
                    }
                    //Snap top aabb to bottom aabb.
                    case 2:
                    {
                        distanceToBound = floatingBounds.top - entityFloating.transformedWorldPos.y;
                        entityFloating.transformedWorldPos.y = entitySnappingBounds.bottom - distanceToBound;
                        break;
                    }
                    //Snap top aabb to top aabb.
                    case 3:
                    {
                        distanceToBound = floatingBounds.top - entityFloating.transformedWorldPos.y;
                        entityFloating.transformedWorldPos.y = entitySnappingBounds.top - distanceToBound;
                        break;
                    }
                }
            }
        }

        private void UpdateAngle()
        {
            if (Keyboard.IsKeyPressed(Keyboard.Key.Up))
                floatingExactAngle += Entity.ROTATE_SPEED * context.GetDeltaTime();
            if (Keyboard.IsKeyPressed(Keyboard.Key.Down))
                floatingExactAngle -= Entity.ROTATE_SPEED * context.GetDeltaTime();

            if (floatingExactAngle >= 360.0)
                floatingExactAngle -= 360.0;
            else if (floatingExactAngle < 0.0)
                floatingExactAngle += 360.0;

            bool snaps = false;
            for (uint i = 0; i < SNAPPING_ANGLES_COUNT; i++)
            {
                if (floatingExactAngle > SNAPPING_ANGLES[i] - SNAPPING_ANGLE_BOUNDS
                && floatingExactAngle < SNAPPING_ANGLES[i] + SNAPPING_ANGLE_BOUNDS)
                {
                    entityFloating.angle = SNAPPING_ANGLES[i];
                    snaps = true;
                    break;
                }
            }
            if (!snaps)
                entityFloating.angle = floatingExactAngle;
        }
	}
}
