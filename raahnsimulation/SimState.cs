using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;
using OpenTK.Graphics;

namespace RaahnSimulation
{
    public class SimState : State
    {
        public const double DEFAULT_UPDATE_DELAY = 10.0;
        private const int NO_PADDING = 0;
        private const int VERTICAL_PADDING = 20;
        private const int HORIZONTAL_PADDING_SHORT = 40;
        private const int HORIZONTAL_PADDING_LONG = 60;
        private const double DELAY_CHOOSER_MIN = 0.0;
        private const double DELAY_CHOOSER_MAX = 100.0;
        private const double DELAY_CHOOSER_STEP = 1.0;
        private const double CAR_WIDTH = 260.0;
        private const double CAR_HEIGHT = 160.0;
        private const double HIGHLIGHT_R = 0.0;
        private const double HIGHLIGHT_G = 1.0;
        private const double HIGHLIGHT_B = 0.0;
        private const double HIGHLIGHT_T = 1.0;
        private const string TICK_COUNTER_INITIAL = "0";
        private const string TICK_FONT_SIZE = "32";

        public static double updateDelay;
        private static SimState simState = new SimState();

        //Experiment must be initialized outside of SimState.
        public Experiment experiment;
        //Number of times the agent has been updated.
        private uint ticksElapsed;
        private bool panning;
        private bool simulationRunning;
        private double defaultCarX;
        private double defaultCarY;
        private double defaultCarAngle;
        private Gtk.Label tickCounter;
        private Gtk.MenuBar menuBar;
        private Gtk.SpinButton delayChooser;
        private QuadTree quadTree;
        private Cursor cursor;
        private Car raahnCar;
        private EntityMap entityMap;
        private Stopwatch timer;

        public SimState()
        {
            ticksElapsed = 0;

            panning = false;
            simulationRunning = false;
            experiment = null;
            quadTree = null;
            raahnCar = null;
            entityMap = null;
            timer = null;

            updateDelay = DEFAULT_UPDATE_DELAY;
        }

        public override bool Init(Simulator context)
        {
            if (!base.Init(context))
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

            Gtk.MenuItem helpOption = new Gtk.MenuItem(Utils.MENU_HELP);
            Gtk.Menu helpMenu = new Gtk.Menu();
            helpOption.Submenu = helpMenu;

            Gtk.MenuItem aboutItem = new Gtk.MenuItem(Utils.MENU_ABOUT);
            aboutItem.Activated += delegate { context.DisplayAboutDialog(); };
            helpMenu.Append(aboutItem);

            menuBar.Append(helpOption);

            //Must be instantiated and added to the window before entites.
            mainGLWidget = new GLWidget(GraphicsMode.Default, InitGraphics, Draw);

            //Controls for the simulation.
            Gtk.HBox controlBox = new Gtk.HBox();

            //Controls for delay.
            Gtk.VBox speedControls = new Gtk.VBox();

            Gtk.Label delayLabel = new Gtk.Label(Utils.DELAY_DESCRIPTION);

            delayChooser = new Gtk.SpinButton(DELAY_CHOOSER_MIN, DELAY_CHOOSER_MAX, DELAY_CHOOSER_STEP);
            delayChooser.Value = updateDelay;
            delayChooser.ValueChanged += OnDelayChooserChanged;

            speedControls.PackStart(delayLabel, false, false, NO_PADDING);
            speedControls.PackStart(delayChooser, false, false, VERTICAL_PADDING);

            Pango.FontDescription tickFont = Pango.FontDescription.FromString(TICK_FONT_SIZE);

            Gtk.Label tickCounterDescription = new Gtk.Label(Utils.TICKS_ELAPSED);
            tickCounterDescription.ModifyFont(tickFont);

            controlBox.PackStart(speedControls, false, false, HORIZONTAL_PADDING_LONG);
            controlBox.PackEnd(tickCounterDescription, false, false, HORIZONTAL_PADDING_SHORT);

            //Button panel.
            Gtk.HBox buttonPanel = new Gtk.HBox();

            Gtk.Image playImage = new Gtk.Image(Gtk.Stock.MediaPlay, Gtk.IconSize.Button);
            Gtk.Image pauseImage = new Gtk.Image(Gtk.Stock.MediaPause, Gtk.IconSize.Button);
            Gtk.Image restartImage = new Gtk.Image(Gtk.Stock.MediaPrevious, Gtk.IconSize.Button);

            Gtk.Button playButton = new Gtk.Button(playImage);
            playButton.Clicked += OnPlayClicked;

            Gtk.Button pauseButton = new Gtk.Button(pauseImage);
            pauseButton.Clicked += OnPauseClicked;

            Gtk.Button restartButton = new Gtk.Button(restartImage);
            restartButton.Clicked += OnRestartClicked;

            tickCounter = new Gtk.Label(TICK_COUNTER_INITIAL);
            tickCounter.ModifyFont(tickFont);

            buttonPanel.PackStart(playButton, false, false, NO_PADDING);
            buttonPanel.PackStart(pauseButton, false, false, NO_PADDING);
            buttonPanel.PackStart(restartButton, false, false, NO_PADDING);
            buttonPanel.PackEnd(tickCounter, false, false, HORIZONTAL_PADDING_SHORT);

            mcVbox.PackStart(menuBar, false, true, NO_PADDING);
            mcVbox.PackStart(mainGLWidget, true, true, NO_PADDING);
            mcVbox.PackStart(controlBox, false, false, NO_PADDING);
            mcVbox.PackStart(buttonPanel, false, false, NO_PADDING);

            mainWindow.Add(mainContainer);

            mainContainer.ShowAll();

            if (!GetGLInitialized())
                return false;

            quadTree = new QuadTree(new AABB(Simulator.WORLD_WINDOW_WIDTH, Simulator.WORLD_WINDOW_HEIGHT));

            cursor = new Cursor(context, mainGLWidget);

            raahnCar = new Car(context, quadTree);

            if (experiment != null)
            {
                string sensorPath = null;
                string networkPath = null;

                if (!string.IsNullOrEmpty(experiment.sensorFile))
                    sensorPath = Utils.SENSOR_FOLDER + experiment.sensorFile;
                else
                    Console.WriteLine(Utils.NO_SENSOR_FILE);

                if (!string.IsNullOrEmpty(experiment.networkFile))
                    networkPath = Utils.NETWORK_FOLDER + experiment.networkFile;
                else
                    Console.WriteLine(Utils.NO_NETWORK_FILE);

                raahnCar.LoadConfig(sensorPath, networkPath);
            }

            raahnCar.SetWidth(CAR_WIDTH);
            raahnCar.SetHeight(CAR_HEIGHT);
            raahnCar.SetPosition(0.0, 0.0);

            if (experiment != null)
            {
                string mapFilePath = Utils.MAP_FOLDER + experiment.mapFile;
                entityMap = new EntityMap(context, 0, raahnCar, quadTree, mapFilePath);

                defaultCarX = entityMap.GetDefaultCarX();
                defaultCarY = entityMap.GetDefaultCarY();
                defaultCarAngle = entityMap.GetDefaultAngle();
            }
            else
            {
                entityMap = new EntityMap(context, 0, raahnCar, quadTree);

                defaultCarX = 0.0;
                defaultCarY = 0.0;
                defaultCarAngle = 0.0;
            }

            AddEntity(raahnCar, 0);
            AddEntity(cursor, 1);

            quadTree.AddEntity(raahnCar);

            //Update the car with initial map information.
            raahnCar.UpdateMinimal();

            timer = new Stopwatch();

            return true;
        }

        public override void Update()
        {
            cursor.Update();

            int mouseX;
            int mouseY;

            mainGLWidget.GetPointer(out mouseX, out mouseY);
            Gdk.Rectangle glBounds = mainGLWidget.Allocation;

            if (mouseX < 0 || mouseY < 0)
                panning = false;
            else if (mouseX > glBounds.Width || mouseY > glBounds.Height)
                panning = false;

            if (panning)
            {
                Utils.Vector2 deltaPos = cursor.GetDeltaPosition();
                camera.Pan(-deltaPos.x, -deltaPos.y);
            }

            if (simulationRunning)
            {
                if (timer.IsRunning)
                {
                    if (timer.ElapsedMilliseconds < updateDelay)
                        return;
                    else
                        timer.Restart();
                }
                else
                    timer.Start();

                ticksElapsed++;
                tickCounter.Text = ticksElapsed.ToString();

                base.Update();
                entityMap.Update();
                quadTree.Update();

                Utils.Vector2 lowerLeft = camera.TransformWorld(0.0, 0.0);
                Utils.Vector2 upperRight = camera.TransformWorld(Simulator.WORLD_WINDOW_WIDTH, Simulator.WORLD_WINDOW_HEIGHT);

                AABB viewBounds = new AABB(upperRight.x - lowerLeft.x, upperRight.y - lowerLeft.y);
                viewBounds.Translate(lowerLeft.x, lowerLeft.y);

                List<Entity> entitiesInBounds = quadTree.Query(viewBounds);

                //We want to check if raahnCar intersects anything,
                //but we should not check if it intersects itself.
                if (entitiesInBounds.Contains(raahnCar))
                    entitiesInBounds.Remove(raahnCar);

                //Reset the list of entities raahnCar collides with.
                raahnCar.entitiesHovering.Clear();

                for (int i = 0; i < entitiesInBounds.Count; i++)
                {
                    //Only colorable entities are added to the quad tree,
                    //so we can cast it to a colorable entity.
                    Entity curEntity = (Entity)entitiesInBounds[i];

                    if (raahnCar.aabb.Intersects(curEntity.aabb.GetBounds()))
                    {
                        raahnCar.entitiesHovering.Add(curEntity);
                        curEntity.SetColor(HIGHLIGHT_R, HIGHLIGHT_G, HIGHLIGHT_B, HIGHLIGHT_T);
                    }
                    else
                        curEntity.SetColor(Entity.DEFAULT_COLOR_R, Entity.DEFAULT_COLOR_G, Entity.DEFAULT_COLOR_B, Entity.DEFAULT_COLOR_T);
                }
            }
        }

        public override void UpdateEvent(Event e)
        {
            if (e.type == Gdk.EventType.KeyPress)
            {
                if (e.key == Gdk.Key.Left || e.key == Gdk.Key.Right)
                    context.GetWindow().Focus = mainGLWidget;
            }
            if (e.type == Gdk.EventType.Scroll)
            {
                double mouseX = (double)e.X;
                double mouseY = (double)(context.GetWindowHeight() - e.Y);
                Utils.Vector2 transform = camera.ProjectWindow(mouseX, mouseY);

                if (e.scrollDirection == Gdk.ScrollDirection.Up)
                    camera.ZoomTo(transform.x, transform.y, Camera.MOUSE_SCROLL_ZOOM);
                else if (e.scrollDirection == Gdk.ScrollDirection.Down)
                    camera.ZoomTo(transform.x, transform.y, (1.0 / Camera.MOUSE_SCROLL_ZOOM));
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
                Gdk.Rectangle glBounds = GetBounds();

                if (e.Y > glBounds.Y || e.Y < glBounds.Bottom)
                    context.GetWindow().Focus = mainGLWidget;

                if (e.button == Utils.GTK_BUTTON_LEFT)
                    panning = true;
            }
            else if (e.type == Gdk.EventType.ButtonRelease)
            {
                if (e.button == Utils.GTK_BUTTON_LEFT)
                    panning = false;
            }

            entityMap.UpdateEvent(e);

            base.UpdateEvent(e);
        }

        public override void Draw()
        {
            base.Draw();

            if (context.debugging)
                quadTree.DebugDraw();
        }

        public override void Clean()
        {
            base.Clean();
        }

        public static SimState Instance()
        {
            return simState;
        }

        private void OnDelayChooserChanged(object sender, EventArgs ea)
        {
            updateDelay = delayChooser.Value;
        }

        private void OnPlayClicked(object sender, EventArgs ea)
        {
            simulationRunning = true;
        }

        private void OnPauseClicked(object sender, EventArgs ea)
        {
            simulationRunning = false;
        }

        private void OnRestartClicked(object sender, EventArgs ea)
        {
            ticksElapsed = 0;
            tickCounter.Text = TICK_COUNTER_INITIAL;

            simulationRunning = false;

            raahnCar.SetPosition(defaultCarX, defaultCarY);
            raahnCar.angle = defaultCarAngle;

            //Update the car to its original state.
            raahnCar.UpdateMinimal();

            raahnCar.ResetBrain();
        }
    }
}