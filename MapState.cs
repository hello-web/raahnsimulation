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

	        entityPanel = new EntityPanel(context, cursor, 1);

	        mapBuilder = new MapBuilder(context, cursor, camera, entityPanel, 0);

            AddEntity(cursor, 2);
	    }

	    public override void Update()
	    {
            if (entityPanel.Intersects(cursor.aabb.GetBounds()))
                panning = false;
	        if (panning)
	        {
	            Utils.Vector2 deltaPos = cursor.GetDeltaPosition();
	            camera.IncrementPosition(new Utils.Vector2(-deltaPos.x, -deltaPos.y));
	        }

	        //Perform camera transformations before updating positions.
            entityPanel.Update();
            mapBuilder.Update();
	        base.Update();
	    }

        public override void UpdateEvent(Event e)
        {
            //Update mapBuilder before checking whether or not to pan.
            mapBuilder.UpdateEvent(e);
            if (e.Type == EventType.MouseButtonPressed && e.MouseButton.Button == Mouse.Button.Left)
            {
                if (!entityPanel.Intersects(cursor.aabb.GetBounds())
                && !mapBuilder.Floating() && context.GetWindowHasFocus())
                panning = true;
            }

            if (e.Type == EventType.MouseButtonReleased && e.MouseButton.Button == Mouse.Button.Left)
                panning = false;

            entityPanel.UpdateEvent(e);
            base.UpdateEvent(e);
        }

	    public override void Draw()
	    {
	        base.Draw();
	    }

	    public override void Clean()
	    {
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
