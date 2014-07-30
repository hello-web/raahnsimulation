using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Globalization;
using Tao.OpenGl;
using SFML.System;
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
		public float deltaTime;
		public Queue<Event> eventQueue;
		private bool headLess;
		private bool windowHasFocus;
		private bool stateChangeRequested;
		private uint windowWidth;
		private uint windowHeight;
		private int lastTime;
		private int curTime;
		private List<State> states;
		//Events are copied.
		private Window simWindow;
		private Clock clock;
		private uint vb;
		private uint ib;
		private Camera camera;
		private TextureManager texMan;
		private State requestedState;
		private StateChangeType changeType;

	    public Simulator()
	    {
	        lastTime = 0;
	        curTime = 0;
	        deltaTime = 0.0f;
	        running = true;
	        headLess = false;
	        windowHasFocus = false;
	        stateChangeRequested = false;
	        requestedState = null;
	        changeType = StateChangeType.NONE;
			clock = new Clock();
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
	            lastTime = clock.ElapsedTime.AsMilliseconds();
	            ChangeState(MenuState.Instance());
	            MainLoop();
	        }

	        return Utils.EXIT_S;
	    }

	    private bool Init()
	    {
	        //Create size based on monitor resolution.
	        VideoMode monitor = VideoMode.DesktopMode;
	        windowWidth = (uint)((float)monitor.Width * Utils.WIDTH_PERCENTAGE);
	        windowHeight = (uint)((float)monitor.Height * Utils.HEIGHT_PERCENTAGE);

	        simWindow = new Window(new VideoMode(windowWidth, windowHeight), Utils.WINDOW_TITLE, Styles.Default);

	        Vector2i windowPos = new Vector2i((int)((monitor.Width / 2) - (windowWidth / 2)), (int)((monitor.Height / 2) - (windowHeight / 2)));
	        simWindow.Position = windowPos;
	        /*Disable multiple keydown events from
	        occuring when a key is held down.*/
	        simWindow.SetKeyRepeatEnabled(false);

			simWindow.Resized += new EventHandler<SizeEventArgs>(OnResized);
			simWindow.KeyPressed += new EventHandler<KeyEventArgs>(OnKeyPressed);
			simWindow.KeyReleased += new EventHandler<KeyEventArgs>(OnKeyReleased);
			simWindow.MouseButtonPressed += new EventHandler<MouseButtonEventArgs>(OnMouseButtonPressed);
			simWindow.MouseButtonReleased += new EventHandler<MouseButtonEventArgs>(OnMouseButtonReleased);
			simWindow.GainedFocus += new EventHandler(OnGainnedFocus);
			simWindow.LostFocus += new EventHandler(OnLostFocus);
			simWindow.Closed += new EventHandler(OnClosed);

			simWindow.SetActive();

            string glVersion = Gl.glGetString(Gl.GL_VERSION).Substring(0, 3);
            if (float.Parse(glVersion, NumberStyles.Float, Utils.EN_US) < Utils.MIN_GL_VERSION)
            {
                Console.WriteLine("GL 1.5 not supported.");
                return false;
            }

	        Gl.glClearColor(0.0f, 0.5f, 0.0f, 0.0f);
	        //Enable blending for alpha values.
	        Gl.glEnable(Gl.GL_BLEND);
	        Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

	        if (!texMan.LoadTextures())
	            return false;

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
            Gl.glVertexPointer(3, Gl.GL_FLOAT, Utils.Vertex.Size, IntPtr.Zero);

	        Gl.glEnableClientState(Gl.GL_TEXTURE_COORD_ARRAY);
            Gl.glTexCoordPointer(2, Gl.GL_FLOAT, Utils.Vertex.Size, (IntPtr)(sizeof(float) * 3));

	        Gl.glViewport(0, 0, (int)windowWidth, (int)windowHeight);

	        return true;
	    }

	    private void MainLoop()
	    {
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
	            states[states.Count - 1].Update(Utils.NULL_EVENT);
	        }
	    }

	    private void Update()
	    {
			if (eventQueue.Count > 0)
				states[states.Count - 1].Update(eventQueue.Peek());
			else 
				states[states.Count - 1].Update(Utils.NULL_EVENT);
	        if (eventQueue.Count > 0)
	            eventQueue.Dequeue();
	    }

	    private void RenderFrame()
	    {
	        curTime = clock.ElapsedTime.AsMilliseconds();
	        deltaTime = (float)(curTime - lastTime) / 1000.0f;
	        lastTime = curTime;

	        Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);

	        Gl.glMatrixMode(Gl.GL_PROJECTION);

	        Gl.glLoadIdentity();

	        Gl.glOrtho(0.0, windowWidth, 0.0, windowHeight, -1.0, 1.0);

	        Gl.glMatrixMode(Gl.GL_MODELVIEW);

	        Gl.glLoadIdentity();

	        camera.Render();

	        states[states.Count - 1].Draw();

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
            simWindow.Close();
	        while (states.Count > 0)
	        {
	            states[states.Count - 1].Clean();
	            states.RemoveAt(states.Count - 1);
	        }
	        while (eventQueue.Count > 0)
	            eventQueue.Dequeue();
            Gl.glDeleteBuffers(1, ref ib);
            Gl.glDeleteBuffers(1, ref vb);
	    }

		static void OnResized(Object sender, SizeEventArgs ea)
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

		static void OnKeyPressed(Object sender, KeyEventArgs kea)
		{
			Event e = new Event();
			e.Type = EventType.KeyPressed;
			e.Key.Code = kea.Code;
			SaveEvent(e);
			if (kea.Code == Keyboard.Key.Escape)
				Simulator.Instance().running = false;
		}

		static void OnKeyReleased(Object sender, KeyEventArgs kea)
		{
			Event e = new Event();
			e.Type = EventType.KeyReleased;
			e.Key.Code = kea.Code;
			SaveEvent(e);
		}

		static void OnMouseButtonPressed(Object sender, MouseButtonEventArgs mbea)
		{
			Event e = new Event();
			e.Type = EventType.MouseButtonPressed;
			e.MouseButton.Button = mbea.Button;
			SaveEvent(e);
		}

		static void OnMouseButtonReleased(Object sender, MouseButtonEventArgs mbea)
		{
			Event e = new Event();
			e.Type = EventType.MouseButtonReleased;
			e.MouseButton.Button = mbea.Button;
			SaveEvent(e);
		}

		static void OnGainnedFocus(Object sender, EventArgs ea)
		{
			Event e = new Event();
			e.Type = EventType.GainedFocus;
			SaveEvent(e);
			Simulator s = Simulator.Instance();
			s.SetWindowHasFocus(true);
		}

		static void OnLostFocus(Object sender, EventArgs ea)
		{
			Event e = new Event();
			e.Type = EventType.LostFocus;
			SaveEvent(e);
			Simulator s = Simulator.Instance();
			s.SetWindowHasFocus(false);
		}

		static void OnClosed(Object sender, EventArgs ea)
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
		public uint GetWindowWidth()
		{
			return windowWidth;
		}
		public void SetWindowWidth(uint width)
		{
			windowWidth = width;
		}
		public uint GetWindowHeight()
		{
			return windowHeight;
		}
		public void SetWindowHeight(uint height)
		{
			windowHeight = height;
		}
		public bool GetHeadLess()
		{
			return headLess;
		}
		public bool GetWindowHasFocus()
		{
			return windowHasFocus;
		}
		public void SetWindowHasFocus(bool focus)
		{
			windowHasFocus = focus;
		}
	}
}
