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

		private const double FLAG_WIDTH_PERCENTAGE = 0.05f;
		private const double FLAG_HEIGHT_PERCENTAGE = 0.1f;
        private const double SNAPPING_ANGLE_BOUNDS = 7.5f;

        private static readonly double[] SNAPPING_ANGLES = { 0.0f, 90.0f, 180.0f, 270.0f };

        private int layer;
        private bool trashHovering;
        private double doubleingExactAngle;
		private List<LinkedList<ColorableEntity>> entities;
        private Simulator context;
        private State currentState;
		private RoadPool roadPool;
		private Cursor cursor;
		private Camera cam;
		private EntityPanel entityPanel;
		private ColorableEntity entitydoubleing;
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
            entitySnappingDist = new Utils.Vector2(0.0f, 0.0f);

            entities = new List<LinkedList<ColorableEntity>>();

            for (uint i = 0; i < UNIQUE_ENTITIES; i++)
                entities.Add(new LinkedList<ColorableEntity>());

	        flag = new Graphic(context);
            flag.visible = false;
	        flag.SetTexture(TextureManager.TextureType.FLAG);
	        flag.SetWidth(FLAG_WIDTH_PERCENTAGE * (double)context.GetWindowWidth());
	        flag.SetHeight(FLAG_HEIGHT_PERCENTAGE * (double)context.GetWindowHeight());

	        cursor = c;
	        cam = camera;
	        entityPanel = panel;
	        entitydoubleing = null;

            doubleingExactAngle = 0.0f;
            dist = new Utils.Vector2(0.0f, 0.0f);

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
	        if (entitydoubleing != null)
	            UpdateEntitydoubleing();

            if (entitydoubleing != null)
            {
                if (entitydoubleing.Intersects(cam.WindowToWorld(entityPanel.GetTrash().aabb.GetBounds())))
                {
                    if (!trashHovering)
                    {
                        //Turn the trash red when hovering.
                        entityPanel.GetTrash().SetColor(1.0f, 0.0f, 0.0f, 1.0f);
                        trashHovering = true;
                    }
                }
                else
                {
                    //Set the trash can back to black.
                    entityPanel.GetTrash().SetColor(0.0f, 0.0f, 0.0f, 1.0f);
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
                    Vector2i mousePosi = Mouse.GetPosition(context.GetWindow());

                    double x = (double)(mousePosi.X) - (cursor.GetWidth() / 2.0f);
                    double y = (double)(context.GetWindowHeight() - mousePosi.Y) - cursor.GetHeight();

                    Utils.Vector2 mousePosf = new Utils.Vector2(x, y);
                    Utils.Vector2 transform = cam.WindowToWorld(mousePosf);

                    flag.worldPos.x = transform.x;
                    flag.worldPos.y = transform.y;
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
                        dist = entityPanel.GetDist(cursor.worldPos.x, cursor.worldPos.y, cam);
                        UpdateEntitydoubleing();
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
                                comparisonRect = cam.WindowToWorld(cursor.aabb.GetBounds());

                                if (curEntity.Intersects(comparisonRect) && Mouse.IsButtonPressed(Mouse.Button.Left))
                                {
                                    entitydoubleing = curEntity;
                                    entitydoubleing.SetColor(0.0f, 0.0f, 1.0f, 0.85f);
                                    currentState.ChangeLayer(entitydoubleing, currentState.GetTopLayerIndex() - 1);

                                    cursor.Update();

                                    dist.x = cursor.worldPos.x - curEntity.worldPos.x;
                                    dist.y = cursor.worldPos.y - curEntity.worldPos.y;

                                    doubleingExactAngle = entitydoubleing.angle;

                                    UpdateEntitydoubleing();
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

            if (entitydoubleing != null)
            {
                if (e.Type == EventType.MouseButtonReleased && e.MouseButton.Button == Mouse.Button.Left)
                {
                    if (trashHovering)
                    {
                        RemoveEntityFromList(entitydoubleing, 0);
                        roadPool.Free((Road)entitydoubleing);
                        entityPanel.GetTrash().SetColor(0.0f, 0.0f, 0.0f, 1.0f);
                        trashHovering = false;
                    }
                    else
                    {
                        //Change the entitiy's color back to its original upon dropping it.
                        entitydoubleing.SetColor(1.0f, 1.0f, 1.0f, 1.0f);
                        currentState.ChangeLayer(entitydoubleing, layer);
                    }
                    entitydoubleing = null;
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
            writer.WriteString(flag.worldPos.x.ToString());
            writer.WriteEndElement();

            writer.WriteStartElement(EntityMap.XML_ELEMENTS[(int)EntityMap.XMLElement.Y]);
            writer.WriteString(flag.worldPos.y.ToString());
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
                    writer.WriteString(entity.worldPos.x.ToString());
                    writer.WriteEndElement();

                    writer.WriteStartElement(EntityMap.XML_ELEMENTS[(int)EntityMap.XMLElement.Y]);
                    writer.WriteString(entity.worldPos.y.ToString());
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

        public bool doubleing()
        {
            if (entitydoubleing != null)
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
                newRoad.SetColor(0.0f, 0.0f, 1.0f, 0.85f);

                AddEntityToList(newRoad, 0);

                entitydoubleing = newRoad;
                entitySnappingDist.x = newRoad.GetWidth() / 4.0f;
                entitySnappingDist.y = newRoad.GetHeight() / 4.0f;
                newRoad.angle = 0.0f;
                doubleingExactAngle = newRoad.angle;
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

        private void UpdateEntitydoubleing()
        {
            //Make sure the mouse is in bounds.
            if (cursor.windowPos.x < 0 || cursor.windowPos.y < 0
            || cursor.windowPos.x > context.GetWindowWidth() || cursor.windowPos.y > context.GetWindowHeight())
            {
                //Change the entitiy's color back to its original upon dropping it.
                entitydoubleing.SetColor(1.0f, 1.0f, 1.0f, 1.0f);
                currentState.ChangeLayer(entitydoubleing, layer);
                entitydoubleing = null;
                return;
            }

            Utils.Vector2 mousePosf = new Utils.Vector2(cursor.worldPos.x - dist.x, cursor.worldPos.y - dist.y);
            UpdatePosition(mousePosf);

            UpdateAngle();
        }

        private void UpdatePosition(Utils.Vector2 mouseWorldPos)
        {
            entitydoubleing.worldPos.x = mouseWorldPos.x;
            entitydoubleing.worldPos.y = mouseWorldPos.y;
            entitydoubleing.Update();

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
                    if (entitydoubleing == curEntity)
                        continue;

                    xDistances.Add(Math.Abs(entitydoubleing.aabb.GetBounds().left - curEntity.aabb.GetBounds().left));
                    xDistances.Add(Math.Abs(entitydoubleing.aabb.GetBounds().left - curEntity.aabb.GetBounds().right));
                    xDistances.Add(Math.Abs(entitydoubleing.aabb.GetBounds().right - curEntity.aabb.GetBounds().left));
                    xDistances.Add(Math.Abs(entitydoubleing.aabb.GetBounds().right - curEntity.aabb.GetBounds().right));

                    yDistances.Add(Math.Abs(entitydoubleing.aabb.GetBounds().bottom - curEntity.aabb.GetBounds().bottom));
                    yDistances.Add(Math.Abs(entitydoubleing.aabb.GetBounds().bottom - curEntity.aabb.GetBounds().top));
                    yDistances.Add(Math.Abs(entitydoubleing.aabb.GetBounds().top - curEntity.aabb.GetBounds().bottom));
                    yDistances.Add(Math.Abs(entitydoubleing.aabb.GetBounds().top - curEntity.aabb.GetBounds().top));

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
                Utils.Rect doubleingBounds = entitydoubleing.aabb.GetBounds();
                Utils.Rect entitySnappingBounds = closestEntityX.aabb.GetBounds();
                double distanceToBound = 0.0f;

                switch (boundsUsageX)
                {
                    //Snap left aabb to left aabb.
                    case 0:
                    {
                        distanceToBound = entitydoubleing.worldPos.x - doubleingBounds.left;
                        entitydoubleing.worldPos.x = entitySnappingBounds.left + distanceToBound;
                        break;
                    }
                    //Snap left aabb to right aabb.
                    case 1:
                    {
                        distanceToBound = entitydoubleing.worldPos.x - doubleingBounds.left;
                        entitydoubleing.worldPos.x = entitySnappingBounds.right + distanceToBound;
                        break;
                    }
                    //Snap right aabb to left aabb.
                    case 2:
                    {
                        distanceToBound = doubleingBounds.right - entitydoubleing.worldPos.x;
                        entitydoubleing.worldPos.x = entitySnappingBounds.left - distanceToBound;
                        break;
                    }
                    //Snap right aabb to right aabb.
                    case 3:
                    {
                        distanceToBound = doubleingBounds.right - entitydoubleing.worldPos.x;
                        entitydoubleing.worldPos.x = entitySnappingBounds.right - distanceToBound;
                        break;
                    }
                }

                entitySnappingBounds = closestEntityY.aabb.GetBounds();

                switch (boundsUsageY)
                {
                    //Snap bottom aabb to left bottom.
                    case 0:
                    {
                        distanceToBound = entitydoubleing.worldPos.y - doubleingBounds.bottom;
                        entitydoubleing.worldPos.y = entitySnappingBounds.bottom + distanceToBound;
                        break;
                    }
                    //Snap bottom aabb to top aabb.
                    case 1:
                    {
                        distanceToBound = entitydoubleing.worldPos.y - doubleingBounds.bottom;
                        entitydoubleing.worldPos.y = entitySnappingBounds.top + distanceToBound;
                        break;
                    }
                    //Snap top aabb to bottom aabb.
                    case 2:
                    {
                        distanceToBound = doubleingBounds.top - entitydoubleing.worldPos.y;
                        entitydoubleing.worldPos.y = entitySnappingBounds.bottom - distanceToBound;
                        break;
                    }
                    //Snap top aabb to top aabb.
                    case 3:
                    {
                        distanceToBound = doubleingBounds.top - entitydoubleing.worldPos.y;
                        entitydoubleing.worldPos.y = entitySnappingBounds.top - distanceToBound;
                        break;
                    }
                }
            }
        }

        private void UpdateAngle()
        {
            if (Keyboard.IsKeyPressed(Keyboard.Key.Up))
                doubleingExactAngle += Entity.ROTATE_SPEED * context.GetDeltaTime();
            if (Keyboard.IsKeyPressed(Keyboard.Key.Down))
                doubleingExactAngle -= Entity.ROTATE_SPEED * context.GetDeltaTime();

            if (doubleingExactAngle >= 360.0f)
                doubleingExactAngle -= 360.0f;
            else if (doubleingExactAngle < 0.0f)
                doubleingExactAngle += 360.0f;

            bool snaps = false;
            for (uint i = 0; i < SNAPPING_ANGLES_COUNT; i++)
            {
                if (doubleingExactAngle > SNAPPING_ANGLES[i] - SNAPPING_ANGLE_BOUNDS
                && doubleingExactAngle < SNAPPING_ANGLES[i] + SNAPPING_ANGLE_BOUNDS)
                {
                    entitydoubleing.angle = SNAPPING_ANGLES[i];
                    snaps = true;
                    break;
                }
            }
            if (!snaps)
                entitydoubleing.angle = doubleingExactAngle;
        }
	}
}
