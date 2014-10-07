using System;
using SFML.Window;

namespace RaahnSimulation
{
	public class SimState : State
	{
		private const float CAR_WIDTH_SCALE = 0.1f;
		private const float CAR_HEIGHT_SCALE = 0.1f;

	    private static SimState simState = new SimState();
		private Car raahnCar;
		private RoadMap roadMap;

	    public SimState()
	    {

	    }

	    public override void Init(Simulator context)
	    {
	        base.Init(context);
	        roadMap = new RoadMap(context, Utils.ROAD_FILE);

	        raahnCar = new Car(context);
	        raahnCar.SetWidth((float)context.GetWindowWidth() * CAR_WIDTH_SCALE);
	        raahnCar.SetHeight((float)context.GetWindowHeight() * CAR_HEIGHT_SCALE);
            raahnCar.aabb.SetSize(raahnCar.GetWidth(), raahnCar.GetHeight());
	        raahnCar.worldPos.x = (float)context.GetWindowWidth() *  0.1f;
	        raahnCar.worldPos.y = (float)context.GetWindowHeight() * 0.1f;

            AddEntity(roadMap, 0);
            AddEntity(raahnCar, 0);
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

	    public override void Clean()
	    {
	        base.Clean();
	    }

		public static SimState Instance()
		{
			return simState;
		}
	}
}
