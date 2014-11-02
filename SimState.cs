using System.Collections.Generic;
using SFML.Window;

namespace RaahnSimulation
{
	public class SimState : State
	{
		private const double CAR_WIDTH_SCALE = 0.1;
		private const double CAR_HEIGHT_SCALE = 0.1;
        private const double HIGHLIGHT_R = 0.0;
        private const double HIGHLIGHT_G = 1.0;
        private const double HIGHLIGHT_B = 0.0;
        private const double HIGHLIGHT_T = 1.0;

	    private static SimState simState = new SimState();

        private QuadTree quadTree;
        private Camera camera;
		private Car raahnCar;
		private EntityMap EntityMap;

	    public SimState()
	    {

	    }

	    public override void Init(Simulator context)
	    {
	        base.Init(context);

            camera = context.GetCamera();

            quadTree = new QuadTree(new AABB(Simulator.WORLD_WINDOW_WIDTH, Simulator.WORLD_WINDOW_HEIGHT));

            raahnCar = new Car(context, quadTree);
            raahnCar.SetWidth(Simulator.WORLD_WINDOW_WIDTH * CAR_WIDTH_SCALE);
            raahnCar.SetHeight(Simulator.WORLD_WINDOW_HEIGHT * CAR_HEIGHT_SCALE);
            raahnCar.transformedWorldPos.x = 0.0;
            raahnCar.transformedWorldPos.y = 0.0;
            raahnCar.Update();

	        EntityMap = new EntityMap(context, 0, raahnCar, quadTree, Utils.ROAD_FILE);

            AddEntity(raahnCar, 0);

            quadTree.AddEntity(raahnCar);
	    }

	    public override void Update()
	    {
            base.Update();
            EntityMap.Update();
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
                ColorableEntity curEntity = (ColorableEntity)entitiesInBounds[i];

                if (raahnCar.Intersects(curEntity.aabb.GetBounds()))
                {
                    raahnCar.entitiesHovering.Add(curEntity);
                    curEntity.SetColor(HIGHLIGHT_R, HIGHLIGHT_G, HIGHLIGHT_B, HIGHLIGHT_T);
                }
                else if (curEntity.Modified())
                    curEntity.SetColor(Entity.DEFAULT_COLOR_R, Entity.DEFAULT_COLOR_G, Entity.DEFAULT_COLOR_B, Entity.DEFAULT_COLOR_T);
            }
	    }

        public override void UpdateEvent(Event e)
        {
            if (e.Type == EventType.MouseWheelMoved)
            {
                double mouseX = (double)e.MouseWheel.X;
                double mouseY = (double)(context.GetWindowHeight() - e.MouseWheel.Y);
                Utils.Vector2 transform = camera.ProjectWindow(mouseX, mouseY);

                if (e.MouseWheel.Delta > 0)
                    camera.ZoomTo(transform.x, transform.y, (double)e.MouseWheel.Delta * Camera.MOUSE_SCROLL_ZOOM);
                else
                    camera.ZoomTo(transform.x, transform.y, (double)(-e.MouseWheel.Delta) * (1.0 / Camera.MOUSE_SCROLL_ZOOM));
            }

            EntityMap.UpdateEvent(e);

            base.UpdateEvent(e);
        }

	    public override void Draw()
	    {
	        base.Draw();
            if (context.debugging)
            {
                quadTree.DebugDraw();
            }
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
