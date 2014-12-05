using System;
using SFML.Window;

namespace RaahnSimulation
{
	public class MapState : State
	{
        private const double SAVE_BUTTON_WIDTH = 800.0;
        private const double SAVE_BUTTON_HEIGHT = 150.0;
        private const double SAVE_BUTTON_X = 2500.0;
        private const double SAVE_BUTTON_Y = 50.0;

	    private static MapState mapState = new MapState();
		private bool panning;
		private Camera camera;
		private Cursor cursor;
		private EntityPanel entityPanel;
		private MapBuilder mapBuilder;
        private Button saveMap;

	    public MapState()
	    {
	        panning = false;
            camera = null;
	        entityPanel = null;
	        mapBuilder = null;
            saveMap = null;
	    }

	    public override void Init(Simulator sim)
	    {
	        base.Init(sim);

	        camera = context.GetCamera();

	        context.GetWindow().SetMouseCursorVisible(false);

	        cursor = new Cursor(context);

	        entityPanel = new EntityPanel(context, cursor, camera, 2);

	        mapBuilder = new MapBuilder(context, cursor, camera, entityPanel, 0);

            saveMap = new Button(context, Utils.SAVE_MAP);
            saveMap.SetTransformUsage(false);
            saveMap.SetBounds(SAVE_BUTTON_X, SAVE_BUTTON_Y, SAVE_BUTTON_WIDTH, SAVE_BUTTON_HEIGHT, false);
            saveMap.SetOnClickListener(SaveMapOnClick);

            AddEntity(saveMap, 2);
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

        public void Save(string file)
        {
            mapBuilder.SaveMap(file);
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

        //Saves the map to the file specified by the user.
        public static void SaveMapOnClick(Simulator sim)
        {
            MapState mapState = MapState.Instance();
            Window mainWindow = sim.GetWindow();

            string file;

            Gtk.FileChooserDialog saveDialog = new Gtk.FileChooserDialog(Utils.SAVE_FILE, null, Gtk.FileChooserAction.Save);
            saveDialog.AddButton(Utils.SAVE_BUTTON, Gtk.ResponseType.Ok);
            saveDialog.AddButton(Utils.CANCEL_BUTTON, Gtk.ResponseType.Cancel);

            //Since SFML.Net and GTK# could not be integrated, only one window
            //should have focus at a given time, or else things get weird...
            mainWindow.SetVisible(false);

            if (saveDialog.Run() == (int)Gtk.ResponseType.Ok)
            {
                //saveDialog makes sure the Filename has at least some text, so we don't have to check it.
                if (saveDialog.Filename.EndsWith(Utils.MAP_FILE_EXTENSION))
                    file = saveDialog.Filename;
                else
                    file = saveDialog.Filename + Utils.MAP_FILE_EXTENSION;

                mapState.Save(file);
            }

            saveDialog.Destroy();

            mainWindow.SetVisible(true);
            //At least with X11 the window needs to be repositioned.
            mainWindow.Position = new Vector2i(sim.windowDefaultX, sim.windowDefaultY);
        }
	}
}
