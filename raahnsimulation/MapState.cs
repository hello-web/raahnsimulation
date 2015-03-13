using System;

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

            Gtk.Window simWindow = context.GetWindow();

            simWindow.GdkWindow.Cursor = context.GetBlankCursor();

            uint newWinWidth = (uint)((double)simWindow.Screen.Width * Utils.DEFAULT_SCREEN_WIDTH_PERCENTAGE);
            uint newWinHeight = (uint)((double)simWindow.Screen.Height * Utils.DEFAULT_SCREEN_HEIGHT_PERCENTAGE);

            context.SetWindowSize(newWinWidth, newWinHeight);
            context.CenterWindow();

            context.SetGLVisible(true);
            context.ResizeGL(newWinWidth, newWinHeight - Simulator.MENU_OFFSET);

            cursor = new Cursor(context);

            entityPanel = new EntityPanel(context, cursor, camera, 2);

            mapBuilder = new MapBuilder(context, cursor, 0);

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

            int mouseX;
            int mouseY;

            context.GetWindow().GetPointer(out mouseX, out mouseY);

            if (mouseX < 0 || mouseY < 0)
                mouseOutOfBounds = true;
            else if (mouseX > context.GetWindowWidth() || mouseY > context.GetWindowHeight())
                mouseOutOfBounds = true;

            if (entityPanel.Intersects(cursor.aabb.GetBounds()) || mouseOutOfBounds)
                panning = false;
            else if (panning) //If we just set panning to false, no need to pan. else if is better.
            {
                Utils.Vector2 deltaPos = cursor.GetDeltaPosition();
                camera.Pan(-deltaPos.x, -deltaPos.y);
            }

            //Perform camera transformations before updating positions.
            base.Update();
        }

        public override void UpdateEvent(Event e)
        {
            base.UpdateEvent(e);
            cursor.Update();
            //Update mapBuilder before checking whether or not to pan.
            mapBuilder.UpdateEvent(e);
            entityPanel.UpdateEvent(e);

            if (e.type == Gdk.EventType.Scroll)
            {
                if (e.scrollDirection == Gdk.ScrollDirection.Up)
                    camera.ZoomTo(cursor.GetWorldX(), cursor.GetWorldY(), Camera.MOUSE_SCROLL_ZOOM);
                else if (e.scrollDirection == Gdk.ScrollDirection.Down)
                    camera.ZoomTo(cursor.GetWorldX(), cursor.GetWorldY(), (1.0 / Camera.MOUSE_SCROLL_ZOOM));
            }
            else if (e.type == Gdk.EventType.MotionNotify)
            {
                Gtk.Window simWindow = context.GetWindow();

                if (e.Y < Simulator.MENU_OFFSET)
                    simWindow.GdkWindow.Cursor = null;
                else
                    simWindow.GdkWindow.Cursor = context.GetBlankCursor();
            }
            else if (e.type == Gdk.EventType.ButtonPress)
            {
                if (e.button == Utils.GTK_BUTTON_LEFT)
                {
                    if (!entityPanel.Intersects(cursor.aabb.GetBounds())
                        && context.GetWindowHasFocus())
                        panning = true;
                }
            }
            else if (e.type == Gdk.EventType.ButtonRelease)
            {
                if (e.button == Utils.GTK_BUTTON_LEFT)
                    panning = false;
            }
        }

        public override void Draw()
        {
            mapBuilder.Draw();
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

            string file;

            Gtk.FileChooserDialog saveDialog = new Gtk.FileChooserDialog(Utils.SAVE_FILE, null, Gtk.FileChooserAction.Save);
            saveDialog.AddButton(Utils.SAVE_BUTTON, Gtk.ResponseType.Ok);
            saveDialog.AddButton(Utils.CANCEL_BUTTON, Gtk.ResponseType.Cancel);
            saveDialog.SetCurrentFolder(Utils.MAP_FOLDER);

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
        }
    }
}