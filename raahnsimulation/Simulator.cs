using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Gtk;

namespace RaahnSimulation
{
	public class Simulator
	{
		public enum StateChangeType
		{
			NONE = -1,
			PUSH = 0,
			POP = 1,
			CHANGE = 2
		}

        public const uint DEFAULT_WINDOW_WIDTH = 800;
        public const uint DEFAULT_WINDOW_HEIGHT = 600;
        public const uint MIN_WINDOW_WIDTH = 200;
        public const uint MIN_WINDOW_HEIGHT = 200;
        public const int MENU_OFFSET = 30;
        //Scaling down is usually better, if it even matters in this context.
        public const double WORLD_WINDOW_WIDTH = 3840.0;
        public const double WORLD_WINDOW_HEIGHT = 2160.0;

        //Shared mesh resources.
        public static Mesh lineRect;
        public static Mesh quad;
		private static Simulator simulator = new Simulator();

		public bool running;
        public bool debugging;
        public bool eventsEnabled;
        //Events are copied.
        private bool glInitFailed;
		private bool headLess;
		private bool windowHasFocus;
        private bool leftMouseButtonDown;
		private bool stateChangeRequested;
        //Some key states.
        private bool leftKeyDown;
        private bool rightKeyDown;
        private bool upKeyDown;
        private bool downKeyDown;
		private uint windowWidth;
		private uint windowHeight;
		private long lastTime;
		private long curTime;
        private double deltaTime;
		private List<State> states;
        private Queue<Event> eventQueue;
		private Window simWindow;
        private Gdk.Cursor blankCursor;
        private Fixed fixedLayout;
        private MenuBar menuBar;
        private GLWidget mainGLWidget;
        private Stopwatch stopwatch;
		private Camera camera;
		private TextureManager texMan;
		private State requestedState;
		private StateChangeType changeType;

	    public Simulator()
	    {
	        lastTime = 0;
	        curTime = 0;
	        deltaTime = 0.0;

	        running = true;
            glInitFailed = false;
	        headLess = false;
            debugging = false;
            eventsEnabled = true;
            //Upon initial creation of the window, some OSes will not raise a GainnedFocus event.
	        windowHasFocus = true;
            leftMouseButtonDown = false;
	        stateChangeRequested = false;

	        requestedState = null;
	        changeType = StateChangeType.NONE;
            stopwatch = new Stopwatch();
			states = new List<State>();
            eventQueue = new Queue<Event>();
	        texMan = new TextureManager();
	    }

		public static Simulator Instance()
		{
			return simulator;
		}

	    public int Execute()
	    {
	        if (!headLess)
	        {
	            if (!Init())
	            {
	                Clean();
	                return Utils.EXIT_F;
	            }

                ChangeState(MenuState.Instance());
                MainLoop();
	        }
            else
	        {
	            ChangeState(SimState.Instance());
	            MainLoopHeadless();
	        }

            Clean();

	        return Utils.EXIT_S;
	    }

	    private bool Init()
	    {
            OpenTK.Toolkit.Init();
            Application.Init();

	        simWindow = new Window(WindowType.Toplevel);
            simWindow.Title = Utils.WINDOW_TITLE;

            //Create size based on monitor resolution.
            windowWidth = (uint)((double)simWindow.Screen.Width * Utils.MENU_SCREEN_WIDTH_PERCENTAGE);
            windowHeight = (uint)((double)simWindow.Screen.Height * Utils.MENU_SCREEN_HEIGHT_PERCENTAGE);

            simWindow.Resize((int)windowWidth, (int)windowHeight);

            //Allow shrinking below the dimensions of the child widgets.
            simWindow.AllowShrink = true;

            //Force a minimum width and height.
            Gdk.Geometry minDim = new Gdk.Geometry();
            minDim.MinWidth = (int)MIN_WINDOW_WIDTH;
            minDim.MinHeight = (int)MIN_WINDOW_HEIGHT;

            simWindow.SetGeometryHints(simWindow, minDim, Gdk.WindowHints.MinSize);

            camera = new Camera(this);

            //Add some event signaling.
            simWindow.Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask 
                             | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.ScrollMask | Gdk.EventMask.StructureMask;

            //Add event handlers.
            simWindow.ConfigureEvent += OnConfigure;
            simWindow.KeyPressEvent += OnKeyPress;
            simWindow.KeyReleaseEvent += OnKeyRelease;
            simWindow.ButtonPressEvent += OnButtonPress;
            simWindow.ButtonReleaseEvent += OnButtonRelease;
            simWindow.MotionNotifyEvent += OnMotionNotify;
            simWindow.ScrollEvent += OnScroll;
            simWindow.FocusInEvent += OnFocusIn;
            simWindow.FocusOutEvent += OnFocusOut;
            simWindow.DeleteEvent += OnDelete;

            InitGUI();

            CenterWindow();

            if (glInitFailed)
                return false;

	        return true;
	    }

        private void InitGUI()
        {
            Gdk.Pixmap blank = new Gdk.Pixmap(null, 1, 1, 1);
            blankCursor = new Gdk.Cursor(blank, blank, Gdk.Color.Zero, Gdk.Color.Zero, 0, 0);

            mainGLWidget = new GLWidget(GraphicsMode.Default, InitGraphics, RenderFrame);
            mainGLWidget.SetSizeRequest((int)windowWidth, (int)(windowHeight - MENU_OFFSET));

            menuBar = new MenuBar();

            MenuItem helpOption = new MenuItem("Help");
            Menu helpMenu = new Menu();
            helpOption.Submenu = helpMenu;

            MenuItem aboutItem = new MenuItem("About");
            aboutItem.Activated += delegate { DisplayAboutDialog(); };
            helpMenu.Append(aboutItem);

            menuBar.Append(helpOption);

            fixedLayout = new Fixed();
            fixedLayout.Put(mainGLWidget, 0, MENU_OFFSET);
            fixedLayout.Put(menuBar, 0, 0);

            simWindow.Add(fixedLayout);

            simWindow.ShowAll();
        }

        private void InitGraphics()
        {
            //Check to make sure OpenGL 1.5 is supported.
            string glVersion = GL.GetString(StringName.Version).Substring(0, 3);
            Console.Write(Utils.VERBOSE_GL_VERSION);
            Console.WriteLine(glVersion);

            if (double.Parse(glVersion) < Utils.MIN_GL_VERSION)
            {
                glInitFailed = true;
                Console.WriteLine(Utils.GL_VERSION_UNSUPPORTED);
                return;
            }

            GL.ClearColor(Utils.BACKGROUND_COLOR_VALUE, Utils.BACKGROUND_COLOR_VALUE, Utils.BACKGROUND_COLOR_VALUE, 0.0f);

            //Enable blending for alpha values.
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            if (!texMan.LoadTextures())
            {
                Console.WriteLine(Utils.TEXTURE_LOAD_FAILED);
                glInitFailed = true;
                return;
            }

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);

            lineRect = new Mesh(2, BeginMode.Lines);

            float[] lrVertices = new float[]
            {
                0.0f, 0.0f,
                1.0f, 0.0f,
                0.0f, 1.0f,
                1.0f, 1.0f
            };

            ushort[] lrIndices =
            {
                0, 1,
                1, 3,
                3, 2,
                2, 0
            };

            lineRect.SetVertices(lrVertices, false);
            lineRect.SetIndices(lrIndices);
            lineRect.Allocate(BufferUsageHint.StaticDraw);

            quad = new Mesh(2, BeginMode.Triangles);

            float[] quadVertices = 
            {
                0.0f, 0.0f, 0.0f, 0.0f,
                1.0f, 0.0f, 1.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 1.0f, 1.0f
            };

            ushort[] quadIndices =
            {
                0, 1, 2,
                2, 3, 1
            };

            quad.SetVertices(quadVertices, true);
            quad.SetIndices(quadIndices);
            //Also makes quad's vertex buffer current.
            quad.Allocate(BufferUsageHint.StaticDraw);
            quad.MakeCurrent();

            GL.Viewport(0, 0, (int)windowWidth, (int)windowHeight);
        }

	    private void MainLoop()
	    {
            stopwatch.Start();

	        while (running)
	        {
                //Update GTK events.
                while (Application.EventsPending())
                    Application.RunIteration();

	            Update();

                if (mainGLWidget.Visible)
                    mainGLWidget.RenderFrame();

	            if (stateChangeRequested)
	                HandleStateChangeRequest();
	        }
	    }

	    private void MainLoopHeadless()
	    {
	        while (running)
	        {
	            states[states.Count - 1].Update();
	        }
	    }

	    private void Update()
	    {
            curTime = stopwatch.ElapsedMilliseconds;
            deltaTime = (double)(curTime - lastTime) / 1000.0;
            lastTime = curTime;

            //Update with events.
            Event e;

            while (eventQueue.Count > 0)
            {
                e = eventQueue.Peek();
                eventQueue.Dequeue();

                states[states.Count - 1].UpdateEvent(e);

                if (e.type == Gdk.EventType.Configure)
                {
                    windowWidth = (uint)e.width;
                    windowHeight = (uint)e.height;
                    camera.windowWorldRatio.x = (double)windowWidth / Simulator.WORLD_WINDOW_WIDTH;
                    camera.windowWorldRatio.y = (double)windowHeight / Simulator.WORLD_WINDOW_HEIGHT;

                    ResizeGL(windowWidth, windowHeight - MENU_OFFSET);
                    ResizeFrame();
                }
            }

            //Regular update per frame.
            states[states.Count - 1].Update();
        }

        private void ResizeFrame()
        {
            GL.Viewport(0, 0, (int)windowWidth, (int)windowHeight);
        }

	    private void RenderFrame()
	    {
	        GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.MatrixMode(MatrixMode.Projection);

            GL.LoadIdentity();

            GL.Ortho(0.0, WORLD_WINDOW_WIDTH, 0.0, WORLD_WINDOW_HEIGHT, -1.0, 1.0);

            GL.MatrixMode(MatrixMode.Modelview);

	        GL.LoadIdentity();

	        camera.Transform();

	        states[states.Count - 1].Draw();
	    }

	    public bool RequestStateChange(StateChangeType sc, State newState)
		{
			//Make sure state is valid
			if (sc != StateChangeType.POP && newState == null)
				return false;

			stateChangeRequested = true;
			changeType = sc;
			requestedState = newState;

			return true;
		}

		private void HandleStateChangeRequest()
		{
			switch (changeType)
			{
				case StateChangeType.PUSH:
				{
					PushState(requestedState);
					break;
				}
				case StateChangeType.POP:
				{
					PopState();
					break;
				}
				case StateChangeType.CHANGE:
				{
					ChangeState(requestedState);
					break;
				}
				case StateChangeType.NONE:
	                break;
                default:
                    break;
			}

			stateChangeRequested = false;
			changeType = StateChangeType.NONE;
			requestedState = null;
		}

	    private void ChangeState(State newState)
	    {
	        //Stop the current state and change to the new state
	        if (states.Count > 0)
	        {
	            states[states.Count - 1].Clean();
	            states.RemoveAt(states.Count - 1);
	        }

	        states.Add(newState);
	        newState.Init(this);
	    }

	    private void PushState(State newState)
	    {
	        if (states.Count > 0)
	            states[states.Count - 1].Pause();

	        states.Add(newState);
	        newState.Init(this);
	    }

	    private void PopState()
	    {
	        if (states.Count > 0)
	        {
	            states[states.Count - 1].Clean();
	            states.RemoveAt(states.Count - 1);
	        }

	        if (states.Count > 0)
	            states[states.Count - 1].Resume();
	    }

	    public void SetHeadLess(bool value)
	    {
	        headLess = value;
	    }

	    private void Clean()
	    {
            stopwatch.Stop();

            blankCursor.Dispose();

	        while (states.Count > 0)
	        {
	            states[states.Count - 1].Clean();
	            states.RemoveAt(states.Count - 1);
	        }
	        while (eventQueue.Count > 0)
	            eventQueue.Dequeue();

            //Deletes textures if loaded.
            texMan.DeleteTextures();

            if (!glInitFailed)
            {
                lineRect.Free();
                quad.Free();
            }

            //Free the GL context after deleting GL objects.
            mainGLWidget.Dispose();
	    }

        private void DisplayAboutDialog()
        {
            AboutDialog about = new AboutDialog();

            about.ProgramName = Utils.WINDOW_TITLE;
            about.Version = Utils.VERSION_STRING;

            about.Run();
            about.Destroy();
        }

        //Window moved or resized. Must use GLib.ConnectBefore
        //to avoid an event terminating the cycle before this event.
        [GLib.ConnectBefore]
		private void OnConfigure(object sender, ConfigureEventArgs ea)
		{
            Event e = new Event();
            e.type = Gdk.EventType.Configure;
            e.X = ea.Event.X;
            e.Y = ea.Event.Y;
            e.width = ea.Event.Width;
            e.height = ea.Event.Height;

			SaveEvent(e);
		}

        //Space not registered without GLib.ConnectBefore
        [GLib.ConnectBefore]
		private void OnKeyPress(object sender, KeyPressEventArgs ea)
		{
            if (ea.Event.Key == Gdk.Key.Escape)
            {
                Simulator.Instance().running = false;
                //Don't bother saving the event.
                return;
            }

            Event e = new Event();
            e.type = Gdk.EventType.KeyPress;
            e.key = ea.Event.Key;

            switch (ea.Event.Key)
            {
                case Gdk.Key.Left:
                {
                    leftKeyDown = true;
                    break;
                }
                case Gdk.Key.Right:
                {
                    rightKeyDown = true;
                    break;
                }
                case Gdk.Key.Up:
                {
                    upKeyDown = true;
                    break;
                }
                case Gdk.Key.Down:
                {
                    downKeyDown = true;
                    break;
                }
            }

			SaveEvent(e);
		}

        [GLib.ConnectBefore]
		private void OnKeyRelease(object sender, KeyReleaseEventArgs ea)
		{
            Event e = new Event();
            e.type = Gdk.EventType.KeyRelease;
            e.key = ea.Event.Key;

			SaveEvent(e);

            switch (ea.Event.Key)
            {
                case Gdk.Key.Left:
                {
                    leftKeyDown = false;
                    break;
                }
                case Gdk.Key.Right:
                {
                    rightKeyDown = false;
                    break;
                }
                case Gdk.Key.Up:
                {
                    upKeyDown = false;
                    break;
                }
                case Gdk.Key.Down:
                {
                    downKeyDown = false;
                    break;
                }
            }
		}

        //Mouse button press.
		private void OnButtonPress(object sender, ButtonPressEventArgs ea)
		{
            Event e = new Event();
            e.type = Gdk.EventType.ButtonPress;
            e.button = ea.Event.Button;
            e.X = ea.Event.X;
            e.Y = ea.Event.Y;

            if (ea.Event.Button == Utils.GTK_BUTTON_LEFT)
                leftMouseButtonDown = true;

			SaveEvent(e);
		}

        //Mouse button release.
		private void OnButtonRelease(object sender, ButtonReleaseEventArgs ea)
		{
            Event e = new Event();
            e.type = Gdk.EventType.ButtonRelease;
            e.button = ea.Event.Button;
            e.X = ea.Event.X;
            e.Y = ea.Event.Y;

            if (ea.Event.Button == Utils.GTK_BUTTON_LEFT)
                leftMouseButtonDown = false;

			SaveEvent(e);
		}

        //Mouse move.
        private void OnMotionNotify(object sender, MotionNotifyEventArgs ea)
        {
            Event e = new Event();
            e.type = Gdk.EventType.MotionNotify;
            e.X = ea.Event.X;
            e.Y = ea.Event.Y;

            SaveEvent(e);
        }

        //Mouse wheel scroll.
        private void OnScroll(object sender, ScrollEventArgs ea)
        {
            Event e = new Event();
            e.type = Gdk.EventType.Scroll;
            e.scrollDirection = ea.Event.Direction;
            e.X = ea.Event.X;
            e.Y = ea.Event.Y;

            SaveEvent(e);
        }

		private void OnFocusIn(object sender, FocusInEventArgs ea)
		{
            Event e = new Event();
            e.type = Gdk.EventType.FocusChange;

			SaveEvent(e);
		}

		private void OnFocusOut(object sender, FocusOutEventArgs ea)
		{
            Event e = new Event();
            e.type = Gdk.EventType.FocusChange;

            leftMouseButtonDown = false;

			SaveEvent(e);
		}

		private void OnDelete(object sender, DeleteEventArgs ea)
		{
            Event e = new Event();
            e.type = Gdk.EventType.Delete;

			SaveEvent(e);

            Simulator.Instance().running = false;
            mainGLWidget.Invalidate();
		}

		private void SaveEvent(Event e)
		{
            //Only record events if they are enabled by the application.
            if (!eventsEnabled)
                return;
			//Process all events but leave keep them in a queue
	        //to be processed by all entities.
			eventQueue.Enqueue(e);
		}

        public void CenterWindow()
        {
            if (simWindow != null)
            {
                int centerX = (simWindow.Screen.Width / 2) - ((int)windowWidth / 2);
                int centerY = (simWindow.Screen.Height / 2) - ((int)windowHeight / 2);
                simWindow.Move(centerX, centerY);
            }
        }

        public void SetGLVisible(bool visible)
        {
            mainGLWidget.Visible = visible;
        }

        public void ResizeGL(uint width, uint height)
        {
            mainGLWidget.SetSizeRequest((int)width, (int)height);
        }

        public bool GetHeadLess()
        {
            return headLess;
        }

        public bool GetWindowHasFocus()
        {
            return windowHasFocus;
        }

        public bool GetLeftMouseButtonDown()
        {
            return leftMouseButtonDown;
        }

        public bool GetLeftKeyDown()
        {
            return leftKeyDown;
        }

        public bool GetRightKeyDown()
        {
            return rightKeyDown;
        }

        public bool GetUpKeyDown()
        {
            return upKeyDown;
        }

        public bool GetDownKeyDown()
        {
            return downKeyDown;
        }

        public uint GetWindowWidth()
        {
            return windowWidth;
        }

        public uint GetWindowHeight()
        {
            return windowHeight;
        }

        public double GetDeltaTime()
        {
            return deltaTime;
        }

        public State GetState()
        {
            return states[states.Count - 1];
        }

        public Window GetWindow()
        {
            return simWindow;
        }

        public Gdk.Cursor GetBlankCursor()
        {
            return blankCursor;
        }

        public Gtk.Fixed GetMainContainer()
        {
            return fixedLayout;
        }

        public Camera GetCamera()
        {
            return camera;
        }

        public TextureManager GetTexMan()
        {
            return texMan;
        }

        public Stopwatch GetStopwatch()
        {
            return stopwatch;
        }

        public void SetWindowSize(uint width, uint height)
        {
            windowWidth = width;
            windowHeight = height;

            simWindow.Resize((int)windowWidth, (int)windowHeight);
        }

        public void SetWindowHasFocus(bool focus)
        {
            windowHasFocus = focus;
        }
	}
}
