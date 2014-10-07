using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
using Tao.OpenGl;
using SFML.Window;

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

		private static Simulator simulator = new Simulator();

		public bool running;
        public bool debugging;
        //Events are copied.
		public Queue<Event> eventQueue;
        private bool glInitFailed;
		private bool headLess;
        private bool terminalOpen;
		private bool windowHasFocus;
		private bool stateChangeRequested;
		private uint windowWidth;
		private uint windowHeight;
        private uint vb;
        private uint ib;
		private long lastTime;
		private long curTime;
        private float deltaTime;
		private List<State> states;
		private Window simWindow;
        private Stopwatch stopwatch;
        private Keyboard.Key terminalKey;
		private Camera camera;
        private Terminal terminal;
		private TextureManager texMan;
		private State requestedState;
		private StateChangeType changeType;

	    public Simulator()
	    {
	        lastTime = 0;
	        curTime = 0;
	        deltaTime = 0.0f;

	        running = true;
            glInitFailed = false;
	        headLess = false;
            terminalOpen = false;
            debugging = false;
            //Upon initial creation of the window, some OSes will not raise a GainnedFocus event.
	        windowHasFocus = true;
	        stateChangeRequested = false;

            terminalKey = Keyboard.Key.F1;
	        requestedState = null;
	        changeType = StateChangeType.NONE;
            stopwatch = new Stopwatch();
			camera = new Camera();
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
	        }

	        if (headLess)
	        {
	            ChangeState(SimState.Instance());
	            MainLoopHeadless();
	        }
	        else
	        {
	            ChangeState(MenuState.Instance());
	            MainLoop();
	        }

            Clean();

	        return Utils.EXIT_S;
	    }

	    private bool Init()
	    {
	        //Create size based on monitor resolution.
	        VideoMode monitor = VideoMode.DesktopMode;
	        windowWidth = (uint)((float)monitor.Width * Utils.WIDTH_PERCENTAGE);
	        windowHeight = (uint)((float)monitor.Height * Utils.HEIGHT_PERCENTAGE);

	        simWindow = new Window(new VideoMode(windowWidth, windowHeight), Utils.WINDOW_TITLE, Styles.Close);

	        Vector2i windowPos = new Vector2i((int)((monitor.Width / 2) - (windowWidth / 2)), (int)((monitor.Height / 2) - (windowHeight / 2)));
	        simWindow.Position = windowPos;

            //Check to make sure OpenGL 1.5 is supported.
            string glVersion = Gl.glGetString(Gl.GL_VERSION).Substring(0, 3);
            Console.WriteLine("GL Version " + glVersion);
            if (float.Parse(glVersion, NumberStyles.Float, Utils.EN_US) < Utils.MIN_GL_VERSION)
            {
                glInitFailed = true;
                Console.WriteLine(Utils.GL_VERSION_UNSUPPORTED);
                return false;
            }

	        /*Disable multiple keydown events from
	        occuring when a key is held down.*/
	        simWindow.SetKeyRepeatEnabled(false);

			simWindow.Resized += new EventHandler<SizeEventArgs>(OnResized);
			simWindow.KeyPressed += new EventHandler<KeyEventArgs>(OnKeyPressed);
			simWindow.KeyReleased += new EventHandler<KeyEventArgs>(OnKeyReleased);
			simWindow.MouseButtonPressed += new EventHandler<MouseButtonEventArgs>(OnMouseButtonPressed);
			simWindow.MouseButtonReleased += new EventHandler<MouseButtonEventArgs>(OnMouseButtonReleased);
            simWindow.MouseMoved += new EventHandler<MouseMoveEventArgs>(OnMouseMoved);
            simWindow.TextEntered += new EventHandler<TextEventArgs>(OnTextEntered);
            simWindow.MouseWheelMoved += new EventHandler<MouseWheelEventArgs>(OnMouseWheelMoved);
			simWindow.GainedFocus += new EventHandler(OnGainnedFocus);
			simWindow.LostFocus += new EventHandler(OnLostFocus);
			simWindow.Closed += new EventHandler(OnClosed);

			simWindow.SetActive();

            terminal = new Terminal(this);

            Gl.glClearColor(Utils.BACKGROUND_COLOR_VALUE, Utils.BACKGROUND_COLOR_VALUE, Utils.BACKGROUND_COLOR_VALUE, 0.0f);

	        //Enable blending for alpha values.
	        Gl.glEnable(Gl.GL_BLEND);
	        Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

            if (!texMan.LoadTextures())
            {
                Console.WriteLine(Utils.TEXTURE_LOAD_FAILED);
                return false;
            }

	        //Allocate verticies and indicies
            //Can't get struct elements to be placed sequentially, using an array for now.
	        /*Utils.Vertex[] vertices = new Utils.Vertex[]
	        {
	            new Utils.Vertex(new Utils.Vector3(0.0f, 0.0f, 0.0f), new Utils.Vector2(0.0f, 0.0f)),
	            new Utils.Vertex(new Utils.Vector3(1.0f, 0.0f, 0.0f), new Utils.Vector2(1.0f, 0.0f)),
	            new Utils.Vertex(new Utils.Vector3(0.0f, 1.0f, 0.0f), new Utils.Vector2(0.0f, 1.0f)),
	            new Utils.Vertex(new Utils.Vector3(1.0f, 1.0f, 0.0f), new Utils.Vector2(1.0f, 1.0f))
	        };*/

            float[] vertices = new float[]
            {
                0.0f, 0.0f, 0.0f, 0.0f, 0.0f,
                1.0f, 0.0f, 0.0f, 1.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f, 1.0f
            };

	        ushort[] indices =
	        {
	            0, 1, 2,
	            2, 3, 1
	        };

	        Gl.glGenBuffers(1, out vb);
	        Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, vb);
	        Gl.glBufferData(Gl.GL_ARRAY_BUFFER, (IntPtr)(sizeof(float) * vertices.Length), vertices, Gl.GL_STATIC_DRAW);

	        Gl.glGenBuffers(1, out ib);
	        Gl.glBindBuffer(Gl.GL_ELEMENT_ARRAY_BUFFER, ib);
			Gl.glBufferData(Gl.GL_ELEMENT_ARRAY_BUFFER, (IntPtr)(sizeof(ushort) * indices.Length), indices, Gl.GL_STATIC_DRAW);

	        Gl.glEnableClientState(Gl.GL_VERTEX_ARRAY);
            Gl.glVertexPointer(3, Gl.GL_FLOAT, Utils.VertexSize, IntPtr.Zero);

	        Gl.glEnableClientState(Gl.GL_TEXTURE_COORD_ARRAY);
            Gl.glTexCoordPointer(2, Gl.GL_FLOAT, Utils.VertexSize, (IntPtr)(sizeof(float) * 3));

	        Gl.glViewport(0, 0, (int)windowWidth, (int)windowHeight);

	        return true;
	    }

	    private void MainLoop()
	    {
            stopwatch.Start();

	        while (running)
	        {
				simWindow.DispatchEvents();

	            Update();
	            RenderFrame();

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
            deltaTime = (float)(curTime - lastTime) / 1000.0f;
            lastTime = curTime;

            //Update with events.
            Event e;
            while (eventQueue.Count > 0)
            {
                e = eventQueue.Peek();
                eventQueue.Dequeue();

                states[states.Count - 1].UpdateEvent(e);

                if (e.Type == EventType.KeyPressed && e.Key.Code == terminalKey)
                {
                    if (terminalOpen)
                        terminalOpen = false;
                    else
                        terminalOpen = true;
                }

                if (terminalOpen)
                    terminal.UpdateEvent(e);
            }

            //Regular update per frame.
            states[states.Count - 1].Update();

            if (terminalOpen)
                terminal.Update();
	    }

	    private void RenderFrame()
	    {
	        Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);

	        Gl.glMatrixMode(Gl.GL_PROJECTION);

	        Gl.glLoadIdentity();

	        Gl.glOrtho(0.0, windowWidth, 0.0, windowHeight, -1.0, 1.0);

	        Gl.glMatrixMode(Gl.GL_MODELVIEW);

	        Gl.glLoadIdentity();

	        camera.Transform();

	        states[states.Count - 1].Draw();

            if (terminalOpen)
                terminal.Draw();

	        simWindow.Display();
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
                Gl.glDeleteBuffers(1, ref ib);
                Gl.glDeleteBuffers(1, ref vb);
            }

            simWindow.Close();
	    }

		public static void OnResized(Object sender, SizeEventArgs ea)
		{
			Event e = new Event();
			e.Type = EventType.Resized;
			e.Size.Width = ea.Width;
			e.Size.Height = ea.Height;
			SaveEvent(e);
			Simulator s = Simulator.Instance();
			s.SetWindowWidth(ea.Width);
			s.SetWindowHeight(ea.Height);

			Gl.glViewport(0, 0, (int)s.GetWindowWidth(), (int)s.GetWindowHeight());
		}

		public static void OnKeyPressed(Object sender, KeyEventArgs kea)
		{
			Event e = new Event();
			e.Type = EventType.KeyPressed;
			e.Key.Code = kea.Code;
			SaveEvent(e);
			if (kea.Code == Keyboard.Key.Escape)
				Simulator.Instance().running = false;
		}

		public static void OnKeyReleased(Object sender, KeyEventArgs kea)
		{
			Event e = new Event();
			e.Type = EventType.KeyReleased;
			e.Key.Code = kea.Code;
			SaveEvent(e);
		}

		public static void OnMouseButtonPressed(Object sender, MouseButtonEventArgs mbea)
		{
			Event e = new Event();
			e.Type = EventType.MouseButtonPressed;
			e.MouseButton.Button = mbea.Button;
			SaveEvent(e);
		}

		public static void OnMouseButtonReleased(Object sender, MouseButtonEventArgs mbea)
		{
			Event e = new Event();
			e.Type = EventType.MouseButtonReleased;
			e.MouseButton.Button = mbea.Button;
			SaveEvent(e);
		}

        public static void OnMouseMoved(object sender, MouseMoveEventArgs mmea)
        {
            Event e = new Event();
            e.Type = EventType.MouseMoved;
            e.MouseMove.X = mmea.X;
            e.MouseMove.Y = mmea.Y;
            SaveEvent(e);
        }

        public static void OnTextEntered(Object sender, TextEventArgs tea)
        {
            Event e = new Event();
            e.Type = EventType.TextEntered;
            e.Text.Unicode = (char)tea.Unicode[0];
            SaveEvent(e);
        }

        public static void OnMouseWheelMoved(Object sender, MouseWheelEventArgs mwea)
        {
            Event e = new Event();
            e.Type = EventType.MouseWheelMoved;
            e.MouseWheel.Delta = mwea.Delta;
            e.MouseWheel.X = mwea.X;
            e.MouseWheel.Y = mwea.Y;
            SaveEvent(e);
        }

		public static void OnGainnedFocus(Object sender, EventArgs ea)
		{
			Event e = new Event();
			e.Type = EventType.GainedFocus;
			SaveEvent(e);
			Simulator s = Simulator.Instance();
			s.SetWindowHasFocus(true);
		}

		public static void OnLostFocus(Object sender, EventArgs ea)
		{
			Event e = new Event();
			e.Type = EventType.LostFocus;
			SaveEvent(e);
			Simulator s = Simulator.Instance();
			s.SetWindowHasFocus(false);
		}

		public static void OnClosed(Object sender, EventArgs ea)
		{
			Event e = new Event();
			e.Type = EventType.Closed;
			SaveEvent(e);

            Simulator.Instance().running = false;
		}

		static void SaveEvent(Event e)
		{
			Simulator s = Simulator.Instance();
			/* Process all events but leave keep them in a queue
	        to be processed by all entities.*/
			s.eventQueue.Enqueue(e);
		}

        public bool GetHeadLess()
        {
            return headLess;
        }

        public bool GetWindowHasFocus()
        {
            return windowHasFocus;
        }

        public uint GetWindowWidth()
        {
            return windowWidth;
        }

        public uint GetWindowHeight()
        {
            return windowHeight;
        }

        public float GetDeltaTime()
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

		public void SetWindowWidth(uint width)
		{
			windowWidth = width;
		}

		public void SetWindowHeight(uint height)
		{
			windowHeight = height;
		}

		public void SetWindowHasFocus(bool focus)
		{
			windowHasFocus = focus;
		}
	}
}
