using System;
using System.Collections.Generic;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class EntityPanel : Updateable
	{
        private const uint PANEL_OPTION_COUNT = 2;

        private const float PANEL_ITEM_OFFSET_X_PERCENTAGE = 0.075f;
        private const float PANEL_ITEM_OFFSET_Y_PERCENTAGE = 0.025f;
		private const float PANEL_SPACING_PERCENTAGE = 0.05f;
        private const float PANEL_HEIGHT_SPACE_PERCENTAGE = 0.075f;
        private const float TRASH_WIDTH_PERCENTAGE = 0.08f;
        private const float TRASH_HEIGHT_PERCENTAGE = 0.16f;

        private const string P0 = "Roads";
        private const string P1 = "Obstacles";

        private static readonly string[] PANEL_OPTIONS = { P0, P1 };

		private bool[] intersections;
		private List<Road> items;
        private Simulator context;
        private State currentState;
		private Cursor cursor;
        private ToggleText panelOption;
        private Graphic background;
        private Graphic trash;

	    public EntityPanel(Simulator sim, Cursor c, Camera cam, int layerIndex)
	    {
            context = sim;
	        cursor = c;
            currentState = context.GetState();

            items = new List<Road>();
			intersections = new bool[EntityMap.UNIQUE_ROAD_COUNT];

            background = new Graphic(context);
            background.SetWindowAsDrawingVec(true);
            background.SetTexture(TextureManager.TextureType.PANEL);
            background.windowPos.x = 0.0f;
            background.worldPos.y = 0.0f;

	        Road road;
	        float winWidth = (float)context.GetWindowWidth();
            float roadWidth = winWidth * Road.ROAD_DIMENSION_PERCENTAGE;

	        for (int i = 0; i < EntityMap.UNIQUE_ROAD_COUNT; i++)
	        {
	            road = new Road(context);
    
	            road.SetWindowAsDrawingVec(true);

                float xOffset = PANEL_ITEM_OFFSET_X_PERCENTAGE * winWidth;
                float yOffset = PANEL_ITEM_OFFSET_Y_PERCENTAGE * (float)context.GetWindowHeight();
				road.windowPos.x = xOffset + (i * (roadWidth + PANEL_SPACING_PERCENTAGE * winWidth));
	            road.windowPos.y = yOffset;
				road.SetTexture((TextureManager.TextureType)(TextureManager.ROAD_INDEX_OFFSET + i));
	            items.Add(road);
	            intersections[i] = false;
	        }

            float charWidth = (float)context.GetWindowWidth() * Utils.CHAR_WIDTH_PERCENTAGE;
            float charHeight = (float)context.GetWindowHeight() * Utils.CHAR_HEIGHT_PERCENTAGE;

            panelOption = new ToggleText(context, PANEL_OPTIONS[0]);
            panelOption.SetWindowAsDrawingVec(true);
            panelOption.SetCharBounds(items[0].windowPos.x, items[0].windowPos.y + items[0].GetHeight(), charWidth, charHeight, false);
            panelOption.aabb.SetSize(panelOption.GetWidth(), panelOption.GetHeight());

            for (uint i = 1; i < PANEL_OPTION_COUNT; i++)
                panelOption.AddString(PANEL_OPTIONS[i]);
            panelOption.Update();

            float panelSpacing = (float)context.GetWindowHeight() * PANEL_HEIGHT_SPACE_PERCENTAGE;

            background.SetWidth((float)context.GetWindowWidth());
            background.SetHeight(roadWidth + panelOption.GetHeight() + panelSpacing);

            //Make the bottom of the visible map equivalent to the bottom of the map in the simulation.
            cam.Pan(0.0f, -background.GetHeight());

            trash = new Graphic(context);
            trash.SetWindowAsDrawingVec(true);
            trash.SetTexture(TextureManager.TextureType.TRASH);
            trash.SetColor(0.0f, 0.0f, 0.0f, 1.0f);
            trash.SetWidth(TRASH_WIDTH_PERCENTAGE * (float)context.GetWindowWidth());
            trash.SetHeight(TRASH_HEIGHT_PERCENTAGE * (float)context.GetWindowHeight());

            float xBorderOffset = trash.GetWidth() * 0.2f;
            float yBorderOffset = trash.GetHeight() * 0.1f;

            trash.windowPos.x = (float)context.GetWindowWidth() - trash.GetWidth() - xBorderOffset;
            trash.windowPos.y = yBorderOffset;

            currentState.AddEntity(background, layerIndex);
            for (int i = 0; i < EntityMap.UNIQUE_ROAD_COUNT; i++)
                currentState.AddEntity(items[i], layerIndex);
            currentState.AddEntity(panelOption, layerIndex);
            currentState.AddEntity(trash, layerIndex);
	    }

	    ~EntityPanel()
	    {
	        while (items.Count > 0)
			{
	            items.RemoveAt(items.Count - 1);
	        }
	    }

	    public void Update()
	    {
            for (int i = 0; i < items.Count; i++)
            {
                intersections[i] = items[i].Intersects(cursor.aabb.GetBounds());
                if (intersections[i])
                    items[i].SetColor(0.0f, 0.0f, 1.0f, 0.85f);
                else
                    items[i].SetColor(1.0f, 1.0f, 1.0f, 1.0f);
            }
	    }

        public void UpdateEvent(Event e)
        {

        }

	    public bool Intersects(float x, float y)
	    {
            if (x > background.aabb.GetBounds().left && x < background.aabb.GetBounds().right)
            {
                if (y > background.aabb.GetBounds().bottom && y < background.aabb.GetBounds().top)
                    return true;
            }
	        return false;
	    }

        public bool Intersects(Utils.Rect bounds)
	    {
	        for (int i = 0; i < items.Count; i++)
	        {
                if (!(background.aabb.GetBounds().left > bounds.right || background.aabb.GetBounds().right < bounds.left
                || background.aabb.GetBounds().bottom > bounds.top || background.aabb.GetBounds().top < bounds.bottom))
	                return true;
	        }
	        return false;
	    }

        public Graphic GetTrash()
        {
            return trash;
        }

		public int GetSelectedEntity()
		{
			//Return -1 for no selected item.
			if (Mouse.IsButtonPressed(Mouse.Button.Left))
			{
				for (int i = 0; i < EntityMap.UNIQUE_ROAD_COUNT; i++)
				{
					if (intersections[i])
						return i;
				}
			}
			return -1;
		}
		public Utils.Vector2 GetDist(float x, float y, Camera cam)
		{
			int index = GetSelectedEntity();
			if (index == -1)
				return new Utils.Vector2(0.0f, 0.0f);

            float zoom = cam.GetZoom();

            float distX = (x - items[index].worldPos.x) * zoom;
			float distY = (y - items[index].worldPos.y) * zoom;

            return new Utils.Vector2(distX, distY);
		}
	}
}
