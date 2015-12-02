using System;
using OpenTK.Graphics;

namespace RaahnSimulation
{
    public class MapState : State
    {
        //Alignment for title text for the controls.
        private const float CONTROL_TITLE_X = 0.01f;
        private const string CONTROL_TITLE_SIZE = "18";

        private static MapState mapState = new MapState();

        private bool panning;
        private Gtk.MenuBar menuBar;
        private Cursor cursor;
        private MapBuilder mapBuilder;

        public MapState()
        {
            panning = false;
            mapBuilder = null;
        }

        public override bool Init(Simulator sim)
        {
            if (!base.Init(sim))
                return false;

            Gtk.Window mainWindow = context.GetWindow();

            mainWindow.GdkWindow.Cursor = context.GetBlankCursor();

            uint newWinWidth = (uint)((double)mainWindow.Screen.Width * Utils.DEFAULT_SCREEN_WIDTH_PERCENTAGE);
            uint newWinHeight = (uint)((double)mainWindow.Screen.Height * Utils.DEFAULT_SCREEN_HEIGHT_PERCENTAGE);

            context.SetWindowSize(newWinWidth, newWinHeight);
            context.CenterWindow();

            //Initialize the layout.
            mainContainer = new Gtk.VBox();
            Gtk.VBox mcVbox = (Gtk.VBox)mainContainer;

            menuBar = new Gtk.MenuBar();

            Gtk.MenuItem fileOption = new Gtk.MenuItem(Utils.MENU_FILE);
            Gtk.Menu fileMenu = new Gtk.Menu();
            fileOption.Submenu = fileMenu;

            Gtk.MenuItem saveItem = new Gtk.MenuItem(Utils.MENU_SAVE);
            saveItem.Activated += delegate { SaveMapOnClick(); };
            fileMenu.Append(saveItem);

            Gtk.MenuItem helpOption = new Gtk.MenuItem(Utils.MENU_HELP);
            Gtk.Menu helpMenu = new Gtk.Menu();
            helpOption.Submenu = helpMenu;

            Gtk.MenuItem aboutItem = new Gtk.MenuItem(Utils.MENU_ABOUT);
            aboutItem.Activated += delegate { context.DisplayAboutDialog(); };
            helpMenu.Append(aboutItem);

            menuBar.Append(fileOption);
            menuBar.Append(helpOption);

            Gtk.Frame mapFrame = new Gtk.Frame(Utils.MAP_FRAME);

            //Must be instantiated and added to the window before entites.
            mainGLWidget = new GLWidget(GraphicsMode.Default, InitGraphics, Draw);

            mapFrame.Add(mainGLWidget);

            Gtk.VBox controls = new Gtk.VBox();

            Pango.FontDescription tickFont = Pango.FontDescription.FromString(CONTROL_TITLE_SIZE);

            Gtk.Label controlsTitle = new Gtk.Label(Utils.MAP_CONTROLS_TITLE);
            controlsTitle.ModifyFont(tickFont);
            controlsTitle.SetAlignment(CONTROL_TITLE_X, 0.0f);

            Gtk.HBox itemPanel = new Gtk.HBox();

            Gtk.Button wallButton = new Gtk.Button(new Gtk.Image(Utils.LINE_ICON));
            wallButton.Clicked += delegate { WallButtonOnClick(); };
            wallButton.TooltipText = Utils.WALL_TOOLTIP;

            Gtk.Button pointButton = new Gtk.Button(new Gtk.Image(Utils.POINT_ICON));
            pointButton.Clicked += delegate { PointButtonOnClick(); };
            pointButton.TooltipText = Utils.POINT_TOOLTIP;

            Gtk.Button selectButton = new Gtk.Button(new Gtk.Image(Utils.SELECT_ICON));
            selectButton.Clicked += delegate { SelectButtonOnClick(); };
            selectButton.TooltipText = Utils.SELECT_TOOLTIP;

            itemPanel.PackStart(wallButton, false, false, Utils.NO_PADDING);
            itemPanel.PackStart(pointButton, false, false, Utils.NO_PADDING);
            itemPanel.PackStart(selectButton, false, false, Utils.NO_PADDING);

            controls.PackStart(controlsTitle, false, false, Utils.NO_PADDING);
            controls.PackStart(itemPanel, false, false, Utils.NO_PADDING);

            mcVbox.PackStart(menuBar, false, true, Utils.NO_PADDING);
            mcVbox.PackStart(mapFrame, true, true, Utils.NO_PADDING);
            mcVbox.PackStart(controls, false, false, Utils.NO_PADDING);

            mainWindow.Add(mainContainer);

            mainContainer.ShowAll();

            if (!GetGLInitialized())
                return false;

            cursor = new Cursor(context, mainGLWidget);

            mapBuilder = new MapBuilder(context, cursor, 0);

            AddEntity(cursor, 1);

            return true;
        }

        public override void Update()
        {
            bool mouseOutOfBounds = false;

            int mouseX;
            int mouseY;

            mainGLWidget.GetPointer(out mouseX, out mouseY);

            Gdk.Rectangle glBounds = mainGLWidget.Allocation;

            if (mouseX < 0 || mouseY < 0)
                mouseOutOfBounds = true;
            else if (mouseX > glBounds.Width || mouseY > glBounds.Height)
                mouseOutOfBounds = true;

            if (mouseOutOfBounds)
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
                Gdk.Rectangle glBounds = GetBounds();

                if (e.Y < glBounds.Y || e.Y > glBounds.Bottom)
                    simWindow.GdkWindow.Cursor = null;
                else
                    simWindow.GdkWindow.Cursor = context.GetBlankCursor();
            }
            else if (e.type == Gdk.EventType.ButtonPress)
            {
                if (e.button == Utils.GTK_BUTTON_LEFT)
                {
                    if (context.GetWindowHasFocus())
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
            base.Draw();
            mapBuilder.Draw();
        }

        public void Save(string file)
        {
            mapBuilder.SaveMap(file);
        }

        public override void Clean()
        {
            Point.CleanShared();
            Wall.CleanShared();
            base.Clean();
        }

        //Saves the map to the file specified by the user.
        public void SaveMapOnClick()
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

        public void WallButtonOnClick()
        {
            mapBuilder.SetMode(MapBuilder.Mode.WALL);
        }

        public void PointButtonOnClick()
        {
            mapBuilder.SetMode(MapBuilder.Mode.POINT);
        }

        public void SelectButtonOnClick()
        {
            mapBuilder.SetMode(MapBuilder.Mode.SELECT);
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