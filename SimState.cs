using System.Collections.Generic;
using SFML.Window;

namespace RaahnSimulation
{
	public class SimState : State
	{
		private const float CAR_WIDTH_SCALE = 0.1f;
		private const float CAR_HEIGHT_SCALE = 0.1f;
        private const float HIGHLIGHT_R = 0.0f;
        private const float HIGHLIGHT_G = 1.0f;
        private const float HIGHLIGHT_B = 0.0f;
        private const float HIGHLIGHT_T = 1.0f;

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

            quadTree = new QuadTree(new AABB((float)context.GetWindowWidth(), (float)context.GetWindowHeight()));

	        EntityMap = new EntityMap(context, 0, quadTree, Utils.ROAD_FILE);

	        raahnCar = new Car(context);
	        raahnCar.SetWidth((float)context.GetWindowWidth() * CAR_WIDTH_SCALE);
	        raahnCar.SetHeight((float)context.GetWindowHeight() * CAR_HEIGHT_SCALE);
	        raahnCar.worldPos.x = (float)context.GetWindowWidth() *  0.1f;
	        raahnCar.worldPos.y = (float)context.GetWindowHeight() * 0.1f;
            raahnCar.Update();

            AddEntity(raahnCar, 0);

            quadTree.AddEntity(raahnCar);
	    }

	    public override void Update()
	    {
            base.Update();
            EntityMap.Update();
            quadTree.Update();

            Utils.Vector2 lowerLeft = camera.WindowToWorld(0.0f, 0.0f);
            Utils.Vector2 upperRight = camera.WindowToWorld((float)context.GetWindowWidth(), (float)context.GetWindowHeight());

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
                float mouseX = (float)e.MouseWheel.X;
                float mouseY = (float)context.GetWindowHeight() - (float)e.MouseWheel.Y;

                if (e.MouseWheel.Delta > 0)
                    camera.ZoomTo(mouseX, mouseY, (float)e.MouseWheel.Delta * Camera.MOUSE_SCROLL_ZOOM);
                else
                    camera.ZoomTo(mouseX, mouseY, (float)(-e.MouseWheel.Delta) * (1.0f / Camera.MOUSE_SCROLL_ZOOM));
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
