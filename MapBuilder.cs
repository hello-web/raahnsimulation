using System;
using System.Collections.Generic;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class MapBuilder : Entity
	{
		private const uint PANEL_OPTION_COUNT = 2;
        private const uint SNAPPING_ANGLES_COUNT = 4;
        private const uint UNIQUE_ENTITIES = 1;

		private const float FLAG_WIDTH_PERCENTAGE = 0.05f;
		private const float FLAG_HEIGHT_PERCENTAGE = 0.1f;
        private const float TRASH_WIDTH_PERCENTAGE = 0.08f;
        private const float TRASH_HEIGHT_PERCENTAGE = 0.16f;
        private const float SNAPPING_ANGLE_BOUNDS = 7.5f;

        private readonly float[] SNAPPING_ANGLES = { 0.0f, 90.0f, 180.0f, 270.0f };
		
        private const string P0 = "Roads";
		private const string P1 = "Obstacles";

		private static readonly string[] PANEL_OPTIONS = { P0, P1 };

		private bool flagVisible;
        private bool trashHovering;
        private float floatingExactAngle;
		private List<List<Entity>> entities;
		private RoadPool roadPool;
		private Cursor cursor;
		private Camera cam;
		private EntityPanel entityPanel;
		private Entity entityFloating;
		private Graphic flag;
        private Graphic trash;
		private ToggleText panelOption;
		private Utils.Vector2 dist;
        private Utils.Vector2 entitySnappingDist;

        public MapBuilder(Simulator sim, Cursor c, Camera camera, EntityPanel panel) : base(sim)
	    {
	        flagVisible = false;
            trashHovering = false;

	        roadPool = new RoadPool(context);
            entitySnappingDist = new Utils.Vector2(0.0f, 0.0f);
            entities = new List<List<Entity>>();
            for (uint i = 0; i < UNIQUE_ENTITIES; i++)
                entities.Add(new List<Entity>());

	        float charWidth = (float)context.GetWindowWidth() * Utils.CHAR_WIDTH_PERCENTAGE;
	        float charHeight = (float)context.GetWindowHeight() * Utils.CHAR_HEIGHT_PERCENTAGE;

	        panelOption = new ToggleText(context, PANEL_OPTIONS[0]);
	        panelOption.SetWindowAsDrawingVec(true);
	        panelOption.SetCharBounds(0.0f, RoadMap.ROAD_HEIGHT_PERCENTAGES[0] * (float)context.GetWindowHeight(), charWidth, charHeight, false);
            panelOption.aabb.SetSize(panelOption.GetWidth(), panelOption.GetHeight());

            for (uint i = 1; i < PANEL_OPTION_COUNT; i++)
	            panelOption.AddString(PANEL_OPTIONS[i]);
	        panelOption.Update();

	        flag = new Graphic(context);
	        flag.SetTexture(TextureManager.TextureType.FLAG);
	        flag.SetWidth(FLAG_WIDTH_PERCENTAGE * (float)context.GetWindowWidth());
	        flag.SetHeight(FLAG_HEIGHT_PERCENTAGE * (float)context.GetWindowHeight());

            trash = new Graphic(context);
            trash.SetTexture(TextureManager.TextureType.TRASH);
            //Default to black.
            trash.SetColor(0.0f, 0.0f, 0.0f, 1.0f);
            trash.SetWidth(TRASH_WIDTH_PERCENTAGE * (float)context.GetWindowWidth());
            trash.SetHeight(TRASH_HEIGHT_PERCENTAGE * (float)context.GetWindowHeight());
            trash.SetWindowAsDrawingVec(true);

            float xBorderOffset = trash.GetWidth() * 0.2f;
            float yBorderOffset = trash.GetHeight() * 0.1f;

            trash.windowPos.x = (float)context.GetWindowWidth() - trash.GetWidth() - xBorderOffset;
            trash.windowPos.y = yBorderOffset;

	        cursor = c;
	        cam = camera;
	        entityPanel = panel;
	        entityFloating = null;

            floatingExactAngle = 0.0f;
            dist = new Utils.Vector2(0.0f, 0.0f);
	    }

	    ~MapBuilder()
	    {
	        while (entities[0].Count > 0)
	        {
                int lastListIndex = entities.Count - 1;
                roadPool.Free((Road)entities[lastListIndex][entities[lastListIndex].Count - 1]);
                entities[0].RemoveAt(entities.Count - 1);
	        }
	    }

	    public override void Update()
	    {
	        if (entityFloating != null)
	            UpdateEntityFloating();
	        else if (!MapState.Instance().GetPanning())
	        {
	            int selectedEntity = entityPanel.GetSelectedEntity();
	            if (selectedEntity != -1)
	            {
	                AddEntity(selectedEntity);
	                dist = entityPanel.GetDist(cursor.worldPos.x, cursor.worldPos.y);
	                UpdateEntityFloating();
	            }
	            else
	            {
	                /*Iterate backwards to find the element drawn
	                on top, elements are drawn in ascending order.*/
                    bool shouldBreak = false;
	                for (int x = entities.Count - 1; x >= 0; x--)
	                {
                        for (int y = entities[x].Count - 1; y >= 0; y--)
                        {
                            //The cursor's bounds are in window coordinates, we need world coordinates.
                            Utils.Rect comparisonRect;
                            comparisonRect = Entity.WindowToWorld(cursor.aabb.GetBounds(), cam);

                            if (entities[x][y].Intersects(comparisonRect) && Mouse.IsButtonPressed(Mouse.Button.Left))
                            {
                                entityFloating = entities[x][y];
                                dist.x = cursor.worldPos.x - entities[x][y].worldPos.x;
                                dist.y = cursor.worldPos.y - entities[x][y].worldPos.y;
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

            if (entityFloating != null)
            {
                if (entityFloating.Intersects(Entity.WindowToWorld(trash.aabb.GetBounds(), cam)))
                {
                    if (!trashHovering)
                    {
                        //Turn the trash red when hovering.
                        trash.SetColor(1.0f, 0.0f, 0.0f, 1.0f);
                        trashHovering = true;
                    }
                }
                else
                {
                    trash.SetColor(0.0f, 0.0f, 0.0f, 1.0f);
                    trashHovering = false;
                }
            }

            for (int x = 0; x < entities.Count; x++)
            {
                for (int y = 0; y < entities[x].Count; y++)
                    entities[x][y].Update();
            }

	        flag.Update();
	        panelOption.Update();
            trash.Update();
	        base.Update();
	    }

        public override void UpdateEvent(Event e)
        {
            base.UpdateEvent(e);

            if (e.Type == EventType.KeyPressed && e.Key.Code == Keyboard.Key.Space)
            {
                if (flagVisible)
                    flagVisible = false;
                else
                {
                    Vector2i mousePosi = Mouse.GetPosition(context.GetWindow());

                    float x = (float)(mousePosi.X) - (cursor.GetWidth() / 2.0f);
                    float y = (float)(context.GetWindowHeight() - mousePosi.Y) - cursor.GetHeight();

                    Utils.Vector2 mousePosf = new Utils.Vector2(x, y);
                    Utils.Vector2 transform = Entity.WindowToWorld(mousePosf, cam);

                    flag.worldPos.x = transform.x;
                    flag.worldPos.y = transform.y;
                    flagVisible = true;
                }
            }

            if (e.Type == EventType.MouseButtonReleased && e.MouseButton.Button == Mouse.Button.Left)
            {
                if (trashHovering)
                {
                    entities[0].Remove(entityFloating);
                    roadPool.Free((Road)entityFloating);
                    trash.SetColor(0.0f, 0.0f, 0.0f, 1.0f);
                    trashHovering = false;
                }
                entityFloating = null;
            }

            //Update entities with the event.
            for (int x = 0; x < entities.Count; x++)
            {
                for (int y = 0; y < entities[x].Count; y++)
                    entities[x][y].UpdateEvent(e);
            }
            flag.UpdateEvent(e);
            panelOption.UpdateEvent(e);
            trash.UpdateEvent(e);
        }

	    public override void Draw()
	    {
            Gl.glPushMatrix();

            Gl.glLoadIdentity();

            trash.Draw();

            Gl.glPopMatrix();

	        for (int x = 0; x < entities.Count; x++)
	        {
                for (int y = 0; y < entities[x].Count; y++)
                {
                    if (entities[x][y] == entityFloating)
                        Gl.glColor4f(0.0f, 0.0f, 1.0f, 0.85f);

                    Gl.glPushMatrix();

                    entities[x][y].Draw();

                    Gl.glPopMatrix();

                    if (entities[x][y] == entityFloating)
                        Gl.glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
                }
	        }
	        if (flagVisible)
	        {
	            Gl.glPushMatrix();

	            flag.Draw();

	            Gl.glPopMatrix();
	        }
	        Gl.glPushMatrix();

	        Gl.glLoadIdentity();

	        panelOption.Draw();

	        Gl.glPopMatrix();
	    }

        public override void DebugDraw()
        {
            Gl.glPushMatrix();

            Gl.glLoadIdentity();

            trash.DebugDraw();

            Gl.glPopMatrix();

            for (int x = 0; x < entities.Count; x++)
            {
                for (int y = 0; y < entities[x].Count; y++)
                {
                    Gl.glPushMatrix();

                    // Disable camera transformation.
                    if (entities[x][y].drawingVec == entities[x][y].windowPos)
                        Gl.glLoadIdentity();

                    entities[x][y].DebugDraw();

                    Gl.glPopMatrix();
                }
            }
            if (flagVisible)
            {
                Gl.glPushMatrix();

                flag.DebugDraw();

                Gl.glPopMatrix();
            }

            Gl.glPushMatrix();

            //panelOption's drawing vector should always be windowPos, disable camera transformations.
            Gl.glLoadIdentity();

            panelOption.DebugDraw();

            Gl.glPopMatrix();
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
                entities[0].Add(newRoad);
                entityFloating = newRoad;
                entitySnappingDist.x = newRoad.GetWidth() / 4.0f;
                entitySnappingDist.y = newRoad.GetHeight() / 4.0f;
                newRoad.angle = 0.0f;
                floatingExactAngle = newRoad.angle;
            }
        }

        private void UpdateEntityFloating()
        {
            //TODO consider using layers instead.
            Vector2i mousePosi = Mouse.GetPosition(context.GetWindow());
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
            int[] closetEntityIndexX = { 0, 0 };
            int[] closetEntityIndexY = { 0, 0 };

            Utils.Vector2 shortestXYDist = new Utils.Vector2(entitySnappingDist.x, entitySnappingDist.y);
            List<float> xDistances = new List<float>();
            List<float> yDistances = new List<float>();

            for (int x = 0; x < entities.Count; x++)
            {
                for (int y = 0; y < entities[x].Count; y++)
                {
                    if (entityFloating == entities[x][y])
                        continue;

                    xDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().left - entities[x][y].aabb.GetBounds().left));
                    xDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().left - entities[x][y].aabb.GetBounds().right));
                    xDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().right - entities[x][y].aabb.GetBounds().left));
                    xDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().right - entities[x][y].aabb.GetBounds().right));

                    yDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().bottom - entities[x][y].aabb.GetBounds().bottom));
                    yDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().bottom - entities[x][y].aabb.GetBounds().top));
                    yDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().top - entities[x][y].aabb.GetBounds().bottom));
                    yDistances.Add(Math.Abs(entityFloating.aabb.GetBounds().top - entities[x][y].aabb.GetBounds().top));

                    //Find the shortest distance to the current entity and use it if it is shorter than shortest.
                    for (int i = 0; i < xDistances.Count; i++)
                    {
                        if (xDistances[i] < shortestXYDist.x)
                        {
                            shortestXYDist.x = xDistances[i];
                            closetEntityIndexX[0] = x;
                            closetEntityIndexX[1] = y;
                            boundsUsageX = i;
                        }
                    }

                    for (int i = 0; i < yDistances.Count; i++)
                    {
                        if (yDistances[i] < shortestXYDist.y)
                        {
                            shortestXYDist.y = yDistances[i];
                            closetEntityIndexY[0] = x;
                            closetEntityIndexY[1] = y;
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
                Utils.Rect iteratingEntityBounds = entities[closetEntityIndexX[0]][closetEntityIndexX[1]].aabb.GetBounds();
                float distanceToBound = 0.0f;

                switch (boundsUsageX)
                {
                    //Snap left aabb to left aabb.
                    case 0:
                    {
                        distanceToBound = entityFloating.worldPos.x - floatingBounds.left;
                        entityFloating.worldPos.x = iteratingEntityBounds.left + distanceToBound;
                        break;
                    }
                    //Snap left aabb to right aabb.
                    case 1:
                    {
                        distanceToBound = entityFloating.worldPos.x - floatingBounds.left;
                        entityFloating.worldPos.x = iteratingEntityBounds.right + distanceToBound;
                        break;
                    }
                    //Snap right aabb to left aabb.
                    case 2:
                    {
                        distanceToBound = floatingBounds.right - entityFloating.worldPos.x;
                        entityFloating.worldPos.x = iteratingEntityBounds.left - distanceToBound;
                        break;
                    }
                    //Snap right aabb to right aabb.
                    case 3:
                    {
                        distanceToBound = floatingBounds.right - entityFloating.worldPos.x;
                        entityFloating.worldPos.x = iteratingEntityBounds.right - distanceToBound;
                        break;
                    }
                }

                switch (boundsUsageY)
                {
                    //Snap bottom aabb to left bottom.
                    case 0:
                    {
                        distanceToBound = entityFloating.worldPos.y - floatingBounds.bottom;
                        entityFloating.worldPos.y = iteratingEntityBounds.bottom + distanceToBound;
                        break;
                    }
                    //Snap bottom aabb to top aabb.
                    case 1:
                    {
                        distanceToBound = entityFloating.worldPos.y - floatingBounds.bottom;
                        entityFloating.worldPos.y = iteratingEntityBounds.top + distanceToBound;
                        break;
                    }
                    //Snap top aabb to bottom aabb.
                    case 2:
                    {
                        distanceToBound = floatingBounds.top - entityFloating.worldPos.y;
                        entityFloating.worldPos.y = iteratingEntityBounds.bottom - distanceToBound;
                        break;
                    }
                    //Snap top aabb to top aabb.
                    case 3:
                    {
                        distanceToBound = floatingBounds.top - entityFloating.worldPos.y;
                        entityFloating.worldPos.y = iteratingEntityBounds.top - distanceToBound;
                        break;
                    }
                }
            }
        }

        private void UpdateAngle()
        {
            if (Keyboard.IsKeyPressed(Keyboard.Key.Up))
                floatingExactAngle += ROTATE_SPEED * context.GetDeltaTime();
            if (Keyboard.IsKeyPressed(Keyboard.Key.Down))
                floatingExactAngle -= ROTATE_SPEED * context.GetDeltaTime();

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

		public override bool Intersects(float x, float y)
		{
			for (int i = 0; i < entities.Count; i++)
			{
                for (int j = 0; j < entities[i].Count; j++)
                {
    				if (x > entities[i][j].aabb.GetBounds().left && x < entities[i][j].aabb.GetBounds().right)
    				{
    					if (y > entities[i][j].aabb.GetBounds().bottom && y < entities[i][j].aabb.GetBounds().top)
    						return true;
    				}
                }
			}
			return false;
		}

        public override bool Intersects(Utils.Rect bounds)
		{
			for (int x = 0; x < entities.Count; x++)
			{
                for (int y = 0; y < entities[x].Count; y++)
                {
                    if (!(entities[x][y].aabb.GetBounds().left > bounds.right || entities[x][y].aabb.GetBounds().right < bounds.left
                    || entities[x][y].aabb.GetBounds().bottom > bounds.top || entities[x][y].aabb.GetBounds().top < bounds.bottom))
                        return true;
                }
			}
			return false;
		}

		public bool GetFloating()
		{
			if (entityFloating != null)
				return true;
			else
				return false;
		}
	}
}
