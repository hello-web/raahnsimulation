using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
	public class EntityPanel
	{
        private const uint PANEL_OPTION_COUNT = 1;

        private const double PANEL_ITEM_OFFSET_X = 300.0;
        private const double PANEL_ITEM_OFFSET_Y = 50.0;
        private const double PANEL_OPTION_Y = 350.0;
		private const double PANEL_SPACING = 200.0;
        private const double PANEL_HEIGHT_SPACE = 525.0;
        private const double TRASH_WIDTH = 300.0;
        private const double TRASH_HEIGHT = 350.0;

        private const string P0 = "Wall";

        private static readonly string[] PANEL_OPTIONS = { P0 };

        private uint itemIndex;
		private List<List<Entity>> items;
        private Simulator context;
        private State currentState;
		private Cursor cursor;
        private Entity selectedEntity;
        private ToggleText panelOption;
        private Graphic background;
        private Graphic trash;

	    public EntityPanel(Simulator sim, Cursor c, Camera cam, uint layerIndex)
	    {
            context = sim;
	        cursor = c;
            itemIndex = 0;
            selectedEntity = null;
            currentState = context.GetState();

            items = new List<List<Entity>>();

            for (int i = 0; i < PANEL_OPTION_COUNT; i++)
                items.Add(new List<Entity>());

            background = new Graphic(context);
            background.SetTransformUsage(false);
            background.SetTexture(TextureManager.TextureType.PANEL);
            background.SetPosition(0.0, 0.0);

            double charWidth = Text.CHAR_DEFAULT_WIDTH;
            double charHeight = Text.CHAR_DEFAULT_HEIGHT;

            panelOption = new ToggleText(context, PANEL_OPTIONS[0]);
            panelOption.SetTransformUsage(false);
            panelOption.SetCharBounds(PANEL_ITEM_OFFSET_X, PANEL_OPTION_Y, charWidth, charHeight, false);
            panelOption.aabb.SetSize(panelOption.GetWidth(), panelOption.GetHeight());
            panelOption.SetOnClickListener(PanelOptionOnClick);

            for (uint i = 1; i < PANEL_OPTION_COUNT; i++)
                panelOption.AddString(PANEL_OPTIONS[i]);
            panelOption.Update();

            background.SetWidth(Simulator.WORLD_WINDOW_WIDTH);
            background.SetHeight(PANEL_HEIGHT_SPACE);

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

            double trashX = Simulator.WORLD_WINDOW_WIDTH - trash.GetWidth() - xBorderOffset;

            trash.SetPosition(trashX, yBorderOffset);

            currentState.AddEntity(background, layerIndex);

            for (int x = 0; x < items.Count; x++)
            {
                for (int y = 0; y < items[x].Count; y++)
                    currentState.AddEntity(items[x][y], layerIndex);
            }

            currentState.AddEntity(panelOption, layerIndex);
            currentState.AddEntity(trash, layerIndex);
	    }

	    ~EntityPanel()
	    {
            for (int i = 0; i < items.Count; i++)
                items[i].Clear();

	        items.Clear();
	    }

        public void UpdateEvent(Event e)
        {
            if (e.type == Gdk.EventType.ButtonPress)
            {
                selectedEntity = null;

                for (int x = 0; x < items.Count; x++)
                {
                    for (int y = 0; y < items[x].Count; y++)
                    {
                        if (items[x][y].aabb.Intersects(cursor.aabb.GetBounds()))
                        {
                            items[x][y].SetColor(0.0, 0.0, 1.0, 0.85);

                            if (e.button == Utils.GTK_BUTTON_LEFT)
                                selectedEntity = items[x][y];
                        }
                        else
                            items[x][y].SetColor(1.0, 1.0, 1.0, 1.0);
                    }
                }
            }
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

		public Entity GetSelectedEntity()
		{
			return selectedEntity;
		}

		public Utils.Vector2 GetDist(double x, double y, Camera cam)
		{
			if (selectedEntity == null)
				return new Utils.Vector2(0.0, 0.0);

            double zoom = cam.GetZoom();

            double distX = (x - selectedEntity.GetTransformedX()) * zoom;
			double distY = (y - selectedEntity.GetTransformedY()) * zoom;

            return new Utils.Vector2(distX, distY);
		}

        private void PanelOptionOnClick(Simulator sim)
        {
            for (uint i = 0; i < items[(int)itemIndex].Count; i++)
                items[(int)itemIndex][(int)i].visible = false;

            if (itemIndex < items.Count - 1)
                itemIndex++;
            else
                itemIndex = 0;

            for (uint i = 0; i < items[(int)itemIndex].Count; i++)
                items[(int)itemIndex][(int)i].visible = true;
        }
	}
}
