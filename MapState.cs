using System;
using SFML.Window;

namespace RaahnSimulation
{
	public class MapState : State
	{
	    private static MapState mapState = new MapState();
		private bool panning;
		private Camera camera;
		private Cursor cursor;
		private EntityPanel entityPanel;
		private MapBuilder mapBuilder;

	    public MapState()
	    {
	        panning = false;
            camera = null;
	        entityPanel = null;
	        mapBuilder = null;
	    }

	    public override void Init(Simulator sim)
	    {
	        base.Init(sim);

	        camera = context.GetCamera();

	        context.GetWindow().SetMouseCursorVisible(false);

	        cursor = new Cursor(context);

	        entityPanel = new EntityPanel(context, cursor, camera, 2);

	        mapBuilder = new MapBuilder(context, cursor, camera, entityPanel, 0);

            AddEntity(cursor, 3);
	    }

	    public override void Update()
	    {
            bool mouseOutOfBounds = false;
            Vector2i mouseworldPos = Mouse.GetPosition(context.GetWindow());

            if (mouseworldPos.X < 0 || mouseworldPos.Y < 0)
                mouseOutOfBounds = true;
            else if (mouseworldPos.X > context.GetWindowWidth() || mouseworldPos.Y > context.GetWindowHeight())
                mouseOutOfBounds = true;

            if (entityPanel.Intersects(cursor.aabb.GetBounds()) || mouseOutOfBounds)
                panning = false;
	        else if (panning) //If we just set panning to false, no need to pan. else if is better.
	        {
	            Utils.Vector2 deltaPos = cursor.GetDeltaPosition();
	            camera.Pan(-deltaPos.x, -deltaPos.y);
	        }

	        //Perform camera transformations before updating positions.
            entityPanel.Update();
            mapBuilder.Update();
	        base.Update();
	    }

        public override void UpdateEvent(Event e)
        {
            if (e.Type == EventType.MouseWheelMoved)
            {
                if (e.MouseWheel.Delta > 0)
                    camera.ZoomTo(cursor.worldPos.x, cursor.worldPos.y, (double)e.MouseWheel.Delta * Camera.MOUSE_SCROLL_ZOOM);
                else
                    camera.ZoomTo(cursor.worldPos.x, cursor.worldPos.y, (double)(-e.MouseWheel.Delta) * (1.0 / Camera.MOUSE_SCROLL_ZOOM));
            }

            //Update mapBuilder before checking whether or not to pan.
            mapBuilder.UpdateEvent(e);

            if (e.MouseButton.Button == Mouse.Button.Left)
            {
                if (e.Type == EventType.MouseButtonPressed)
                {
                    if (!entityPanel.Intersects(cursor.aabb.GetBounds())
                        && !mapBuilder.Floating() && context.GetWindowHasFocus())
                        panning = true;
                }
                else if (e.Type == EventType.MouseButtonReleased)
                    panning = false;
            }

            entityPanel.UpdateEvent(e);
            base.UpdateEvent(e);
        }

	    public override void Draw()
	    {
	        base.Draw();
	    }

	    public override void Clean()
	    {
            mapBuilder.SaveMap();
	        base.Clean();
	    }

		public static MapState Instance()
		{
			return mapState;
		}

		public bool GetPanning()
		{
			return panning;
		}
	}
}
