using System;
using System.Collections.Generic;
using Tao.OpenGl;
using SFML.System;
using SFML.Window;

namespace RaahnSimulation
{
	public class MapBuilder : Entity
	{
		private const int PANEL_OPTION_COUNT = 2;
		private const float MAP_WIDTH_PERCENTAGE = 0.05f;
		private const float MAP_HEIGHT_PERCENTAGE = 0.1f;
		private const string P0 = "Roads";
		private const string P1 = "Obstacles";


		private static readonly string[] PANEL_OPTIONS = { P0, P1 };

		private bool flagVisible;
		private List<Road> roads;
		private RoadPool roadPool;
		private Cursor cursor;
		private Camera cam;
		private EntityPanel entityPanel;
		private Entity entityFloating;
		private Graphic flag;
		private ToggleText panelOption;
		private Utils.Vector2 dist;

	    public MapBuilder(Simulator sim, Cursor c, Camera camera, EntityPanel panel) : base(sim)
	    {
	        flagVisible = false;
	        roadPool = new RoadPool(context);
            roads = new List<Road>();

	        float charWidth = (float)context.GetWindowWidth() * Utils.CHAR_WIDTH_PERCENTAGE;
	        float charHeight = (float)context.GetWindowHeight() * Utils.CHAR_HEIGHT_PERCENTAGE;

	        panelOption = new ToggleText(context, PANEL_OPTIONS[0]);
	        panelOption.SetWindowAsDrawingVec(true);
	        panelOption.SetCharBounds(0.0f, RoadMap.ROAD_HEIGHT_PERCENTAGES[0] * (float)context.GetWindowHeight(), charWidth, charHeight, false);
	        for (int i = 1; i < PANEL_OPTION_COUNT; i++)
	            panelOption.AddString(PANEL_OPTIONS[i]);
	        panelOption.Update(Utils.NULL_EVENT);

	        flag = new Graphic(context);
	        flag.SetTexture(TextureManager.TextureType.FLAG);
	        flag.width = MAP_WIDTH_PERCENTAGE * (float)context.GetWindowWidth();
	        flag.height = MAP_HEIGHT_PERCENTAGE * (float)context.GetWindowHeight();
	        cursor = c;
	        cam = camera;
	        entityPanel = panel;
	        entityFloating = null;
            dist = new Utils.Vector2(0.0f, 0.0f);
	    }

	    ~MapBuilder()
	    {
	        while (roads.Count > 0)
	        {
	            roadPool.Free(roads[roads.Count - 1]);
	            roads.RemoveAt(roads.Count - 1);
	        }
	    }

	    public override void Update(Nullable<Event> nEvent)
	    {
	        if (entityFloating != null)
	        {
	            UpdateEntityFloating();

	            if (!Mouse.IsButtonPressed(Mouse.Button.Left))
	                entityFloating = null;
	        }
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
	                for (int i = roads.Count - 1; i >= 0; i--)
	                {
	                    if (roads[i].Intersects(cursor.bounds) && Mouse.IsButtonPressed(Mouse.Button.Left))
	                    {
	                        entityFloating = roads[i];
	                        dist.x = cursor.worldPos.x - roads[i].worldPos.x;
	                        dist.y = cursor.worldPos.y - roads[i].worldPos.y;
	                        UpdateEntityFloating();
	                        break;
	                    }
	                }
	            }
	        }
            //If there is an event, process it.
	        if (nEvent != null)
	        {
	            if (nEvent.Value.Type == EventType.KeyPressed && nEvent.Value.Key.Code == Keyboard.Key.Space)
	            {
	                if (flagVisible)
	                    flagVisible = false;
	                else
	                {
	                    Vector2i mousePosi = Mouse.GetPosition(context.GetWindow());
	                    float x = (float)(mousePosi.X) - (cursor.width / 2.0f);
	                    float y = (float)(context.GetWindowHeight() - mousePosi.Y) - cursor.height;
	                    Utils.Vector2 mousePosf = new Utils.Vector2(x, y);
	                    Entity.WindowToWorld(mousePosf, flag.worldPos, cam);
	                    flagVisible = true;
	                }
	            }
	        }

	        for (int i = 0; i < roads.Count; i++)
	            roads[i].Update(nEvent);
	        flag.Update(nEvent);
	        panelOption.Update(nEvent);
	        base.Update(nEvent);
	    }

	    public override void Draw()
	    {
	        for (int i = 0; i < roads.Count; i++)
	        {
	            if (roads[i] == entityFloating)
	                Gl.glColor4f(0.0f, 0.0f, 1.0f, 0.85f);
	            Gl.glPushMatrix();
	            roads[i].Draw();
	            Gl.glPopMatrix();
	            if (roads[i] == entityFloating)
	                Gl.glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
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

		public override bool Intersects(float x, float y)
		{
			for (int i = 0; i < roads.Count; i++)
			{
				if (x > roads[i].bounds.left && x < roads[i].bounds.right)
				{
					if (y > roads[i].bounds.bottom && y < roads[i].bounds.top)
						return true;
				}
			}
			return false;
		}

        public override bool Intersects(Utils.Rect bounds)
		{
			for (int i = 0; i < roads.Count; i++)
			{
				if (!(roads[i].bounds.left > bounds.right || roads[i].bounds.right < bounds.left
				      || roads[i].bounds.bottom > bounds.top || roads[i].bounds.top < bounds.bottom))
					return true;
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

	    private void AddEntity(int itemIndex)
	    {
	        if (!roadPool.Empty())
	        {
	            Road newRoad = roadPool.Alloc();
	            newRoad.SetTexture((TextureManager.TextureType)(itemIndex + TextureManager.ROAD_INDEX_OFFSET));
	            newRoad.width = RoadMap.ROAD_WIDTH_PERCENTAGES[itemIndex] * (float)context.GetWindowWidth();
	            newRoad.height = RoadMap.ROAD_HEIGHT_PERCENTAGES[itemIndex] * (float)context.GetWindowHeight();
	            roads.Add(newRoad);
	            entityFloating = newRoad;
	        }
	    }

	    private void UpdateEntityFloating()
	    {
	        //TODO consider using layers instead.
	        Vector2i mousePosi = Mouse.GetPosition(context.GetWindow());
	        float x = (float)(mousePosi.X) - (cursor.width / 2.0f) - dist.x;
	        float y = (float)(context.GetWindowHeight() - mousePosi.Y) - cursor.height - dist.y;
	        Utils.Vector2 mousePosf = new Utils.Vector2(x, y);
	        Entity.WindowToWorld(mousePosf, entityFloating.worldPos, cam);
	        if (Keyboard.IsKeyPressed(Keyboard.Key.Up))
	            entityFloating.angle += ENTITY_ROTATE_SPEED * context.deltaTime;
	        if (Keyboard.IsKeyPressed(Keyboard.Key.Down))
	            entityFloating.angle -= ENTITY_ROTATE_SPEED * context.deltaTime;
	    }
	}
}
