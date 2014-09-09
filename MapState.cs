using System;
using SFML.Window;

namespace RaahnSimulation
{
	public class MapState : State
	{
	    private static MapState mapState = new MapState();
		private bool panning;
		private Camera cam;
		private Cursor cursor;
		private EntityPanel entityPanel;
		private MapBuilder mapBuilder;

	    public MapState()
	    {
	        panning = false;
	        cam = null;
	        entityPanel = null;
	        mapBuilder = null;
	    }

	    public override void Init(Simulator sim)
	    {
	        base.Init(sim);

	        cam = context.GetCamera();

	        context.GetWindow().SetMouseCursorVisible(false);

	        cursor = new Cursor(context);

	        entityPanel = new EntityPanel(context, cursor);

	        mapBuilder = new MapBuilder(context, cursor, cam, entityPanel);

	        entityList.Add(entityPanel);
	        entityList.Add(mapBuilder);
	        entityList.Add(cursor);
	    }

	    public override void Update()
	    {
	        if (panning)
	        {
	            Utils.Vector2 deltaPos = cursor.GetDeltaPosition();
	            cam.IncrementPosition(new Utils.Vector2(-deltaPos.x, -deltaPos.y));
	        }
	        //Perform camera transformations before updating positions.
	        base.Update();

	        if (!Mouse.IsButtonPressed(Mouse.Button.Left))
	            panning = false;
	        if (Mouse.IsButtonPressed(Mouse.Button.Left) && !entityPanel.Intersects(cursor.aabb.GetBounds())
	        && !mapBuilder.GetFloating() && context.GetWindowHasFocus())
	            panning = true;
	    }

        public override void UpdateEvent(Event e)
        {
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
