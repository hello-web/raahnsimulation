using System;
using SFML.Window;

namespace RaahnSimulation
{
	public class MenuState : State
	{
	    private static MenuState menuState = new MenuState();
		private Text title;
		private Text startSim;
		private Text startMap;
		private Text version;
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

	        float charWidth = (float)context.GetWindowWidth() * Utils.CHAR_WIDTH_PERCENTAGE;
	        float charHeight = (float)context.GetWindowHeight() * Utils.CHAR_HEIGHT_PERCENTAGE;

	        title = new Text(context, Utils.WINDOW_TITLE);
	        title.SetWindowAsDrawingVec(true);
	        title.SetCharBounds((float)context.GetWindowWidth() / 2.0f, (float)context.GetWindowHeight() - charHeight, charWidth, charHeight, true);
            title.aabb.UpdateSize(title.width, title.height);

	        startSim = new Text(context, Utils.START_SIM);
	        startSim.SetWindowAsDrawingVec(true);
	        startSim.SetCharBounds((float)context.GetWindowWidth() / 2.0f, title.windowPos.y - 2.0f * charHeight, charWidth, charHeight, true);
            startSim.SetOnClickListener(startSimOnClick);
            startSim.aabb.UpdateSize(startSim.width, startSim.height);

	        startMap = new Text(context, Utils.START_MAP);
	        startMap.SetWindowAsDrawingVec(true);
	        startMap.SetCharBounds((float)context.GetWindowWidth() / 2.0f, startSim.windowPos.y - 2.0f * charHeight, charWidth, charHeight, true);
	        startMap.SetOnClickListener(startMapOnClick);
            startMap.aabb.UpdateSize(startMap.width, startMap.height);

	        version = new Text(context, Utils.VERSION_STRING);
	        version.SetWindowAsDrawingVec(true);
	        version.SetCharBounds(0.0f, 0.0f, charWidth, charHeight, false);
            version.aabb.UpdateSize(version.width, version.height);

	        entityList.Add(title);
	        entityList.Add(startSim);
	        entityList.Add(startMap);
	        entityList.Add(version);
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
