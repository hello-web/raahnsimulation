using System;
using System.Collections.Generic;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class EntityPanel : Entity
	{
		private const float PANEL_SPACING_PERCENTAGE = 0.05f;

		private bool[] intersections;
		private List<Road> items;
		private Cursor cursor;

	    public EntityPanel(Simulator sim, Cursor c) : base(sim)
	    {
	        cursor = c;
            items = new List<Road>();
			intersections = new bool[RoadMap.UNIQUE_ROAD_COUNT];
	        Road road;
	        float winWidth = (float)context.GetWindowWidth();
	        for (int i = 0; i < RoadMap.UNIQUE_ROAD_COUNT; i++)
	        {
	            road = new Road(context);
				road.SetWidth(winWidth * RoadMap.ROAD_WIDTH_PERCENTAGES[i]);
				road.SetHeight((float)context.GetWindowHeight() * RoadMap.ROAD_HEIGHT_PERCENTAGES[i]);
                road.aabb.SetSize(road.GetWidth(), road.GetHeight());
	            road.SetWindowAsDrawingVec(true);

                uint roadIndex = 0;
                if (i > 0)
                    roadIndex = (uint)(i - 1);

				road.windowPos.x = i * (winWidth * RoadMap.ROAD_WIDTH_PERCENTAGES[roadIndex] + PANEL_SPACING_PERCENTAGE * winWidth);
	            road.windowPos.y = 0.0f;
				road.SetTexture((TextureManager.TextureType)(TextureManager.ROAD_INDEX_OFFSET + i));
	            items.Add(road);
	            intersections[i] = false;
	        }
	    }

	    ~EntityPanel()
	    {
	        while (items.Count > 0)
			{
	            items.RemoveAt(items.Count - 1);
	        }
	    }

	    public override void Update()
	    {
	        for (int i = 0; i < items.Count; i++)
	        {
	            items[i].Update();
	            intersections[i] = items[i].Intersects(cursor.aabb.GetBounds());
	        }
	    }

        public override void UpdateEvent(Event e)
        {
            base.UpdateEvent(e);
            for (int i = 0; i < items.Count; i++)
                items[i].UpdateEvent(e);
        }

	    public override void Draw()
	    {
	        for (int i = 0; i < items.Count; i++)
	        {
	            if (intersections[i])
	                Gl.glColor4f(0.0f, 0.0f, 1.0f, 0.85f);

	            Gl.glPushMatrix();

                // Disable camera transformation.
	            Gl.glLoadIdentity();

	            items[i].Draw();

	            Gl.glPopMatrix();

	            if (intersections[i])
	                Gl.glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
	        }
	    }

        public override void DebugDraw()
        {
            for (int i = 0; i < items.Count; i++)
            {
                Gl.glPushMatrix();

                // Disable camera transformation.
                Gl.glLoadIdentity();

                items[i].DebugDraw();

                Gl.glPopMatrix();
            }
        }

	    public override bool Intersects(float x, float y)
	    {
	        for (int i = 0; i < items.Count; i++)
	        {
                if (x > items[i].aabb.GetBounds().left && x < items[i].aabb.GetBounds().right)
	            {
                    if (y > items[i].aabb.GetBounds().bottom && y < items[i].aabb.GetBounds().top)
	                    return true;
	            }
	        }
	        return false;
	    }

        public override bool Intersects(Utils.Rect bounds)
	    {
	        for (int i = 0; i < items.Count; i++)
	        {
                if (!(items[i].aabb.GetBounds().left > bounds.right || items[i].aabb.GetBounds().right < bounds.left
                || items[i].aabb.GetBounds().bottom > bounds.top || items[i].aabb.GetBounds().top < bounds.bottom))
	                return true;
	        }
	        return false;
	    }

		public int GetSelectedEntity()
		{
			//Return -1 for no selected item.
			if (Mouse.IsButtonPressed(Mouse.Button.Left))
			{
				for (int i = 0; i < RoadMap.UNIQUE_ROAD_COUNT; i++)
				{
					if (intersections[i])
						return i;
				}
			}
			return -1;
		}
		public Utils.Vector2 GetDist(float x, float y)
		{
			int index = GetSelectedEntity();
			if (index == -1)
				return new Utils.Vector2(0.0f, 0.0f);
			float distX = x - items[index].worldPos.x;
			float distY = y - items[index].worldPos.y;
			return new Utils.Vector2(distX, distY);
		}
	}
}
