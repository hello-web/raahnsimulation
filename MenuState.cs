using System;
using SFML.Window;

namespace RaahnSimulation
{
	public class MenuState : State
	{
	    private static MenuState menuState = new MenuState();
		private Text title;
        private Text version;
		private Button startSim;
        private Button startMap;
		private ClickableEntity.OnClickType startSimOnClick;
		private ClickableEntity.OnClickType startMapOnClick;

	    public MenuState()
	    {
			startSimOnClick = StartSimOnClick;
			startMapOnClick = StartMapOnClick;
	    }

	    public override void Init(Simulator sim)
	    {
	        base.Init(sim);

	        double charWidth = (double)context.GetWindowWidth() * Utils.CHAR_WIDTH_PERCENTAGE;
	        double charHeight = (double)context.GetWindowHeight() * Utils.CHAR_HEIGHT_PERCENTAGE;

	        title = new Text(context, Utils.WINDOW_TITLE);
	        title.SetWindowAsDrawingVec(true);
	        title.SetCharBounds((double)context.GetWindowWidth() / 2.0f, (double)context.GetWindowHeight() - charHeight, charWidth, charHeight, true);

            version = new Text(context, Utils.VERSION_STRING);
            version.SetWindowAsDrawingVec(true);
            version.SetCharBounds(0.0f, 0.0f, charWidth, charHeight, false);

            double startSimWidth = charWidth * Utils.START_SIM.Length;

	        startSim = new Button(context, Utils.START_SIM);
	        startSim.SetWindowAsDrawingVec(true);
	        startSim.SetBounds((double)context.GetWindowWidth() / 2.0f, title.windowPos.y - (2.0f * charHeight), startSimWidth, charHeight, true);
            startSim.SetOnClickListener(startSimOnClick);

            double startMapWidth = charWidth * Utils.START_MAP.Length;

	        startMap = new Button(context, Utils.START_MAP);
	        startMap.SetWindowAsDrawingVec(true);
	        startMap.SetBounds((double)context.GetWindowWidth() / 2.0f, startSim.windowPos.y - (2.0f * charHeight), startMapWidth, charHeight, true);
	        startMap.SetOnClickListener(startMapOnClick);

	        AddEntity(title, 0);
            AddEntity(version, 0);
            AddEntity(startSim, 0);
            AddEntity(startMap, 0);
	    }

	    public override void Update()
	    {
	        base.Update();
	    }

        public override void UpdateEvent(Event e)
        {
            base.UpdateEvent(e);
        }

	    public override void Draw()
	    {
	        base.Draw();
	    }

		public static MenuState Instance()
		{
			return menuState;
		}

	    public static void StartSimOnClick(Simulator sim)
	    {
	        sim.RequestStateChange(Simulator.StateChangeType.PUSH, SimState.Instance());
	    }

	    public static void StartMapOnClick(Simulator sim)
	    {
	        sim.RequestStateChange(Simulator.StateChangeType.PUSH, MapState.Instance());
	    }
	}
}
