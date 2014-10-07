using System;
using System.Collections.Generic;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class MapBuilder : Updateable
	{
        private const uint SNAPPING_ANGLES_COUNT = 4;
        private const uint UNIQUE_ENTITIES = 1;

		private const float FLAG_WIDTH_PERCENTAGE = 0.05f;
		private const float FLAG_HEIGHT_PERCENTAGE = 0.1f;
        private const float SNAPPING_ANGLE_BOUNDS = 7.5f;

        private readonly float[] SNAPPING_ANGLES = { 0.0f, 90.0f, 180.0f, 270.0f };

        private int layer;
        private bool trashHovering;
        private float floatingExactAngle;
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
            entitySnappingDist = new Utils.Vector2(0.0f, 0.0f);

            entities = new List<LinkedList<ColorableEntity>>();

            for (uint i = 0; i < UNIQUE_ENTITIES; i++)
                entities.Add(new LinkedList<ColorableEntity>());

	        flag = new Graphic(context);
            flag.visible = false;
	        flag.SetTexture(TextureManager.TextureType.FLAG);
	        flag.SetWidth(FLAG_WIDTH_PERCENTAGE * (float)context.GetWindowWidth());
	        flag.SetHeight(FLAG_HEIGHT_PERCENTAGE * (float)context.GetWindowHeight());

	        cursor = c;
	        cam = camera;
	        entityPanel = panel;
	        entityFloating = null;

            floatingExactAngle = 0.0f;
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
	        if (entityFloating != null)
	            UpdateEntityFloating();

            if (entityFloating != null)
            {
                if (entityFloating.Intersects(Entity.WindowToWorld(entityPanel.GetTrash().aabb.GetBounds(), cam)))
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

                    float x = (float)(mousePosi.X) - (cursor.GetWidth() / 2.0f);
                    float y = (float)(context.GetWindowHeight() - mousePosi.Y) - cursor.GetHeight();

                    Utils.Vector2 mousePosf = new Utils.Vector2(x, y);
                    Utils.Vector2 transform = Entity.WindowToWorld(mousePosf, cam);

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
                        dist = entityPanel.GetDist(cursor.worldPos.x, cursor.worldPos.y);
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
                                comparisonRect = Entity.WindowToWorld(cursor.aabb.GetBounds(), cam);

                                if (curEntity.Intersects(comparisonRect) && Mouse.IsButtonPressed(Mouse.Button.Left))
                                {
                                    entityFloating = curEntity;
                                    entityFloating.SetColor(0.0f, 0.0f, 1.0f, 0.85f);
                                    currentState.ChangeLayer(entityFloating, currentState.GetTopLayerIndex() - 1);

                                    dist.x = cursor.worldPos.x - curEntity.worldPos.x;
                                    dist.y = cursor.worldPos.y - curEntity.worldPos.y;

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
                        entityPanel.GetTrash().SetColor(0.0f, 0.0f, 0.0f, 1.0f);
                        trashHovering = false;
                    }
                    else
                    {
                        //Change the entitiy's color back to its original upon dropping it.
                        entityFloating.SetColor(1.0f, 1.0f, 1.0f, 1.0f);
                        currentState.ChangeLayer(entityFloating, layer);
                    }
                    entityFloating = null;
                }
            }
        }

        private void AddEntity(int itemIndex)
        {
            if (!roadPool.Empty())
            {
                Road newRoad = roadPool.Alloc();
                newRoad.SetTexture((TextureManager.TextureType)(itemIndex + TextureManager.ROAD_INDEX_OFFSET));
                newRoad.SetWidth(RoadMap.ROAD_WIDTH_PERCENTAGES[itemIndex] * (float)context.GetWindowWidth());
                newRoad.SetHeight(RoadMap.ROAD_HEIGHT_PERCENTAGES[itemIndex] * (float)context.GetWindowHeight());
                newRoad.aabb.SetSize(newRoad.GetWidth(), newRoad.GetHeight());
                newRoad.SetColor(0.0f, 0.0f, 1.0f, 0.85f);

                AddEntityToList(newRoad, 0);

                entityFloating = newRoad;
                entitySnappingDist.x = newRoad.GetWidth() / 4.0f;
                entitySnappingDist.y = newRoad.GetHeight() / 4.0f;
                newRoad.angle = 0.0f;
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
            Vector2i mousePosi = Mouse.GetPosition(context.GetWindow());

            //Make sure the mouse is in bounds.
            if (mousePosi.X < 0 || mousePosi.Y < 0
            || mousePosi.X > context.GetWindowWidth() || mousePosi.Y > context.GetWindowHeight())
            {
                //Change the entitiy's color back to its original upon dropping it.
                entityFloating.SetColor(1.0f, 1.0f, 1.0f, 1.0f);
                currentState.ChangeLayer(entityFloating, layer);
                entityFloating = null;
                return;
            }

            float x = (float)(mousePosi.X) - (cursor.GetWidth() / 2.0f) - dist.x;
            float y = (float)(context.GetWindowHeight() - mousePosi.Y) - cursor.GetHeight() - dist.y;
            Utils.Vector2 mousePosf = new Utils.Vector2(x, y);
            UpdatePosition(Entity.WindowToWorld(mousePosf, cam));

            UpdateAngle();
        }

        private void UpdatePosition(Utils.Vector2 mouseWorldPos)
        {
            entityFloating.worldPos.x = mouseWorldPos.x;
            entityFloating.worldPos.y = mouseWorldPos.y;
            entityFloating.Update();

            int boundsUsageX = 0;
            int boundsUsageY = 0;
            //Iniitialized to the first entity to make sure they are never null.
            Entity closestEntityX = entities[0].First.Value;
            Entity closestEntityY = entities[0].First.Value;

            Utils.Vector2 shortestXYDist = new Utils.Vector2(entitySnappingDist.x, entitySnappingDist.y);
            List<float> xDistances = new List<float>();
            List<float> yDistances = new List<float>();

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
                float distanceToBound = 0.0f;

                switch (boundsUsageX)
                {
                    //Snap left aabb to left aabb.
                    case 0:
                    {
                        distanceToBound = entityFloating.worldPos.x - floatingBounds.left;
                        entityFloating.worldPos.x = entitySnappingBounds.left + distanceToBound;
                        break;
                    }
                    //Snap left aabb to right aabb.
                    case 1:
                    {
                        distanceToBound = entityFloating.worldPos.x - floatingBounds.left;
                        entityFloating.worldPos.x = entitySnappingBounds.right + distanceToBound;
                        break;
                    }
                    //Snap right aabb to left aabb.
                    case 2:
                    {
                        distanceToBound = floatingBounds.right - entityFloating.worldPos.x;
                        entityFloating.worldPos.x = entitySnappingBounds.left - distanceToBound;
                        break;
                    }
                    //Snap right aabb to right aabb.
                    case 3:
                    {
                        distanceToBound = floatingBounds.right - entityFloating.worldPos.x;
                        entityFloating.worldPos.x = entitySnappingBounds.right - distanceToBound;
                        break;
                    }
                }

                entitySnappingBounds = closestEntityY.aabb.GetBounds();

                switch (boundsUsageY)
                {
                    //Snap bottom aabb to left bottom.
                    case 0:
                    {
                        distanceToBound = entityFloating.worldPos.y - floatingBounds.bottom;
                        entityFloating.worldPos.y = entitySnappingBounds.bottom + distanceToBound;
                        break;
                    }
                    //Snap bottom aabb to top aabb.
                    case 1:
                    {
                        distanceToBound = entityFloating.worldPos.y - floatingBounds.bottom;
                        entityFloating.worldPos.y = entitySnappingBounds.top + distanceToBound;
                        break;
                    }
                    //Snap top aabb to bottom aabb.
                    case 2:
                    {
                        distanceToBound = floatingBounds.top - entityFloating.worldPos.y;
                        entityFloating.worldPos.y = entitySnappingBounds.bottom - distanceToBound;
                        break;
                    }
                    //Snap top aabb to top aabb.
                    case 3:
                    {
                        distanceToBound = floatingBounds.top - entityFloating.worldPos.y;
                        entityFloating.worldPos.y = entitySnappingBounds.top - distanceToBound;
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

            if (floatingExactAngle >= 360.0f)
                floatingExactAngle -= 360.0f;
            else if (floatingExactAngle < 0.0f)
                floatingExactAngle += 360.0f;

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

		public bool Intersects(float x, float y)
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
	}
}
