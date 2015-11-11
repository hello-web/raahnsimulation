using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Xml.Serialization;
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

        public const uint MIN_WINDOW_WIDTH = 200;
        public const uint MIN_WINDOW_HEIGHT = 200;
        //Scaling down is usually better, if it even matters in this context.
        public const double WORLD_WINDOW_WIDTH = 3840.0;
        public const double WORLD_WINDOW_HEIGHT = 2160.0;

        public delegate bool OptionAction(Simulator sim, List<string> arguments);

        public static readonly OptionAction[] OPTION_ACTIONS = 
        {
            ExperimentOption, HeadlessOption, HelpOption
        };

		public bool running;
        public bool debugging;
        public bool eventsEnabled;
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
        private Stopwatch stopwatch;
		private State requestedState;
		private StateChangeType changeType;

	    public Simulator()
	    {
	        lastTime = 0;
	        curTime = 0;
	        deltaTime = 0.0;

	        running = true;
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
	    }

	    public int Execute(string[] argv)
	    {
            //If InterpretArgs returns false, don't continue.
            if (!InterpretArgs(argv))
                return Utils.EXIT_S;

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
            else if (SimState.Instance().experiment != null)
            {
                if (!InitHeadless())
                {
                    Clean();
                    return Utils.EXIT_F;
                }

                ChangeState(SimState.Instance());
                MainLoopHeadless();
            }
            else
                Console.WriteLine(Utils.NO_EXPERIMENT_FILE);

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

            Gdk.Pixmap blank = new Gdk.Pixmap(null, 1, 1, 1);
            blankCursor = new Gdk.Cursor(blank, blank, Gdk.Color.Zero, Gdk.Color.Zero, 0, 0);

            simWindow.ShowAll();

            CenterWindow();

	        return true;
	    }

        private bool InitHeadless()
        {
            OpenTK.Toolkit.Init();
            new OpenTK.GameWindow();

            return true;
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

                states[states.Count - 1].RenderFrame();

                if (stateChangeRequested)
                {
                    if (!HandleStateChangeRequest())
                    {
                        Console.WriteLine(Utils.STATE_CHANGE_ERROR);
                        running = false;
                    }
                }
	        }
	    }

	    private void MainLoopHeadless()
	    {
            Console.WriteLine(Utils.VERBOSE_SIM_START);

            stopwatch.Start();

            while (running)
                UpdateHeadless();

            Console.WriteLine(Utils.TIME_ELAPSED, stopwatch.Elapsed.TotalSeconds);
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

                if (e.type == Gdk.EventType.Configure)
                {
                    windowWidth = (uint)e.width;
                    windowHeight = (uint)e.height;
                }

                states[states.Count - 1].UpdateEvent(e);
            }

            //Regular update per frame.
            states[states.Count - 1].Update();
        }

        private void UpdateHeadless()
        {
            curTime = stopwatch.ElapsedMilliseconds;
            deltaTime = (double)(curTime - lastTime) / 1000.0;
            lastTime = curTime;

            //Regular update per frame.
            states[states.Count - 1].Update();
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

		private bool HandleStateChangeRequest()
		{
			switch (changeType)
			{
				case StateChangeType.PUSH:
				{
                    if (!PushState(requestedState))
                        return false;
					break;
				}
				case StateChangeType.POP:
				{
					PopState();
					break;
				}
				case StateChangeType.CHANGE:
				{
                    if (!ChangeState(requestedState))
                        return false;
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

            return true;
		}

	    private bool ChangeState(State newState)
	    {
	        //Stop the current state and change to the new state
	        if (states.Count > 0)
	        {
	            states[states.Count - 1].Clean();
	            states.RemoveAt(states.Count - 1);
	        }

	        states.Add(newState);

            if (!newState.Init(this))
                return false;

            return true;
	    }

	    private bool PushState(State newState)
	    {
	        if (states.Count > 0)
	            states[states.Count - 1].Pause();

	        states.Add(newState);
	        
            if (!newState.Init(this))
                return false;

            return true;
	    }

	    private void PopState()
	    {
            if (states.Count > 0)
            {
                states[states.Count - 1].Clean();

                states.RemoveAt(states.Count - 1);

                states[states.Count - 1].Resume();
            }
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

            if (!headLess)
                blankCursor.Dispose();
	    }

        private bool InterpretArgs(string[] argv)
        {
            for (int x = 0; x < argv.Length; x++)
            {
                int optionType = -1;
                List<string> argumentList = null;

                for (int y = 0; y < Utils.OPTIONS.Length; y++)
                {
                    if (argv[x].Equals(Utils.OPTIONS[y].optString))
                    {
                        if (x + Utils.OPTIONS[y].argCount >= argv.Length)
                        {
                            Console.WriteLine(Utils.TOO_FEW_ARGS);
                            return false;
                        }

                        optionType = y;

                        //Build argument list.
                        argumentList = new List<string>();

                        for (int z = 0; z < Utils.OPTIONS[y].argCount; z++)
                            argumentList.Add(argv[x + z + 1]);

                        x += (int)Utils.OPTIONS[y].argCount;

                        break;
                    }
                }

                if (optionType > -1)
                {
                    if (!OPTION_ACTIONS[optionType](this, argumentList))
                        return false;
                }
            }

            return true;
        }

        private static bool ExperimentOption(Simulator sim, List<string> arguments)
        {
            string file = arguments[0];

            //Default to using the local path. If not there, use the experiment folder.
            if (!File.Exists(file))
            {
                file = Utils.EXPERIMENT_FOLDER + arguments[0];

                if (!File.Exists(file))
                {
                    Console.WriteLine(Utils.FILE_NOT_FOUND, arguments[0]);
                    return false;
                }
            }

            TextReader expReader = new StreamReader(file);

            try
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(Experiment));
                SimState.Instance().experiment = (Experiment)deserializer.Deserialize(expReader);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            finally
            {
                expReader.Close();
            }

            return true;
        }

        private static bool HeadlessOption(Simulator sim, List<string> arguments)
        {
            sim.SetHeadLess(true);
            return true;
        }

        private static bool HelpOption(Simulator sim, List<string> arguments)
        {
            Console.WriteLine(Utils.VERBOSE_HELP);
            return false;
        }

        public void DisplayAboutDialog()
        {
            AboutDialog about = new AboutDialog();

            about.ProgramName = Utils.WINDOW_TITLE;
            about.Version = Utils.VERSION_STRING;

            about.Run();
            about.Destroy();
        }

        public void SetHeadLess(bool value)
        {
            headLess = value;
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
                running = false;

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
            e.window = (Gtk.Window)sender;

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
            e.window = (Gtk.Window)sender;

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
            e.window = (Gtk.Window)sender;

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
            e.window = (Gtk.Window)sender;

            SaveEvent(e);
        }

		private void OnFocusIn(object sender, FocusInEventArgs ea)
		{
            Event e = new Event();
            e.type = Gdk.EventType.FocusChange;
            e.window = (Gtk.Window)sender;

			SaveEvent(e);
		}

		private void OnFocusOut(object sender, FocusOutEventArgs ea)
		{
            Event e = new Event();
            e.type = Gdk.EventType.FocusChange;
            e.window = (Gtk.Window)sender;

            leftMouseButtonDown = false;

			SaveEvent(e);
		}

		private void OnDelete(object sender, DeleteEventArgs ea)
		{
            Event e = new Event();
            e.type = Gdk.EventType.Delete;
            e.window = (Gtk.Window)sender;

			SaveEvent(e);

            running = false;
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
