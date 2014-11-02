using System;
using System.Collections.Generic;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class EntityPanel : Updateable
	{
        private const uint PANEL_OPTION_COUNT = 2;

        private const double PANEL_ITEM_OFFSET_X = 288.0;
        private const double PANEL_ITEM_OFFSET_Y = 54.0;
		private const double PANEL_SPACING = 192.0;
        private const double PANEL_HEIGHT_SPACE = 162.0;
        private const double TRASH_WIDTH = 307.2;
        private const double TRASH_HEIGHT = 345.6;

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
            background.SetTransformUsage(false);
            background.SetTexture(TextureManager.TextureType.PANEL);
            background.worldPos.x = 0.0;
            background.transformedWorldPos.y = 0.0;

	        Road road;

	        for (int i = 0; i < EntityMap.UNIQUE_ROAD_COUNT; i++)
	        {
	            road = new Road(context);
    
	            road.SetTransformUsage(false);

				road.worldPos.x = PANEL_ITEM_OFFSET_X + (i * (Road.ROAD_DIMENSION + PANEL_SPACING));
	            road.worldPos.y = PANEL_ITEM_OFFSET_Y;
				road.SetTexture((TextureManager.TextureType)(TextureManager.ROAD_INDEX_OFFSET + i));
	            items.Add(road);
	            intersections[i] = false;
	        }

            double charWidth = Text.CHAR_DEFAULT_WIDTH;
            double charHeight = Text.CHAR_DEFAULT_HEIGHT;

            panelOption = new ToggleText(context, PANEL_OPTIONS[0]);
            panelOption.SetTransformUsage(false);
            panelOption.SetCharBounds(items[0].worldPos.x, items[0].worldPos.y + items[0].GetHeight(), charWidth, charHeight, false);
            panelOption.aabb.SetSize(panelOption.GetWidth(), panelOption.GetHeight());

            for (uint i = 1; i < PANEL_OPTION_COUNT; i++)
                panelOption.AddString(PANEL_OPTIONS[i]);
            panelOption.Update();

            background.SetWidth(Simulator.WORLD_WINDOW_WIDTH);
            background.SetHeight(Road.ROAD_DIMENSION + panelOption.GetHeight() + PANEL_HEIGHT_SPACE);

            //Make the bottom of the visible map equivalent to the bottom of the map in the simulation.
            cam.Pan(0.0, -background.GetHeight());

            trash = new Graphic(context);
            trash.SetTransformUsage(false);
            trash.SetTexture(TextureManager.TextureType.TRASH);
            trash.SetColor(0.0, 0.0, 0.0, 1.0);
            trash.SetWidth(TRASH_WIDTH);
            trash.SetHeight(TRASH_HEIGHT);

            double xBorderOffset = trash.GetWidth() * 0.2;
            double yBorderOffset = trash.GetHeight() * 0.1;

            trash.worldPos.x = Simulator.WORLD_WINDOW_WIDTH - trash.GetWidth() - xBorderOffset;
            trash.worldPos.y = yBorderOffset;

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
                    items[i].SetColor(0.0, 0.0, 1.0, 0.85);
                else
                    items[i].SetColor(1.0, 1.0, 1.0, 1.0);
            }
	    }

        public void UpdateEvent(Event e)
        {

        }

	    public bool Intersects(double x, double y)
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
		public Utils.Vector2 GetDist(double x, double y, Camera cam)
		{
			int index = GetSelectedEntity();
			if (index == -1)
				return new Utils.Vector2(0.0, 0.0);

            double zoom = cam.GetZoom();

            double distX = (x - items[index].transformedWorldPos.x) * zoom;
			double distY = (y - items[index].transformedWorldPos.y) * zoom;

            return new Utils.Vector2(distX, distY);
		}
	}
}
