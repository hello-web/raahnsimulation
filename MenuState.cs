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

	        double charWidth = Text.CHAR_DEFAULT_WIDTH;
	        double charHeight = Text.CHAR_DEFAULT_HEIGHT;

	        title = new Text(context, Utils.WINDOW_TITLE);
	        title.SetTransformUsage(false);
	        title.SetCharBounds(Simulator.WORLD_WINDOW_WIDTH / 2.0, Simulator.WORLD_WINDOW_HEIGHT - charHeight, charWidth, charHeight, true);

            version = new Text(context, Utils.VERSION_STRING);
            version.SetTransformUsage(false);
            version.SetCharBounds(0.0, 0.0, charWidth, charHeight, false);

            double startSimWidth = charWidth * Utils.START_SIM.Length;

	        startSim = new Button(context, Utils.START_SIM);
	        startSim.SetTransformUsage(false);
	        startSim.SetBounds(Simulator.WORLD_WINDOW_WIDTH / 2.0, title.worldPos.y - (2.0 * charHeight), startSimWidth, charHeight, true);
            startSim.SetOnClickListener(startSimOnClick);

            double startMapWidth = charWidth * Utils.START_MAP.Length;

	        startMap = new Button(context, Utils.START_MAP);
	        startMap.SetTransformUsage(false);
	        startMap.SetBounds(Simulator.WORLD_WINDOW_WIDTH / 2.0, startSim.worldPos.y - (2.0 * charHeight), startMapWidth, charHeight, true);
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
