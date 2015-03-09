using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
	public class Camera
	{
        public const double MOUSE_SCROLL_ZOOM = 1.25;

        public Utils.Vector2 windowWorldRatio;
        private double zoom;
		private Utils.Vector2 vecPos;

	    public Camera(Simulator sim)
	    {
            double widthRatio = (double)sim.GetWindowWidth() / Simulator.WORLD_WINDOW_WIDTH;
            double heightRatio = (double)sim.GetWindowHeight() / Simulator.WORLD_WINDOW_HEIGHT;

            windowWorldRatio = new Utils.Vector2(widthRatio, heightRatio);
	        vecPos = new Utils.Vector2(0.0, 0.0);
            Reset();
	    }

        public void Reset()
        {
            zoom = 1.0;

            vecPos.x = 0.0;
            vecPos.y = 0.0;
        }

	    public void Transform()
	    {
            GL.Scale(zoom, zoom, Utils.DISCARD_Z_SCALE);
            GL.Translate(-vecPos.x, -vecPos.y, Utils.DISCARD_Z_POS);
	    }

	    public void Pan(double dx, double dy)
	    {
	        vecPos.x += dx / zoom;
	        vecPos.y += dy / zoom;
	    }

        public void Zoom(double zoomFactor)
        {
            zoom *= zoomFactor;
        }

        public void ZoomTo(double x, double y, double zoomFactor)
        {
            Pan(x, y);
            Zoom(zoomFactor);
            Pan(-x, -y);
        }

        public Utils.Vector2 TransformWorld(double windowX, double windowY)
        {
            double x = (windowX / zoom) + vecPos.x;
            double y = (windowY / zoom) + vecPos.y;
            return new Utils.Vector2(x, y);
        }

        public Utils.Vector2 UntransformWorld(double worldX, double worldY)
        {
            double x = (worldX - vecPos.x) * zoom;
            double y = (worldY - vecPos.y) * zoom;
            return new Utils.Vector2(x, y);
        }

        public Utils.Vector2 TransformWorld(Utils.Vector2 world)
        {
            return TransformWorld(world.x, world.y);
        }

        public Utils.Vector2 UntransformWorld(Utils.Vector2 tWorld)
        {
            return UntransformWorld(tWorld.x, tWorld.y);
        }

        public Utils.Vector2 ProjectWindow(double windowX, double windowY)
        {
            return new Utils.Vector2(windowX / windowWorldRatio.x, windowY / windowWorldRatio.y);
        }

        public Utils.Vector2 UnProjectWorld(double worldX, double worldY)
        {
            return new Utils.Vector2(worldX * windowWorldRatio.x, worldY * windowWorldRatio.y);
        }

        public Utils.Vector2 ProjectWindow(Utils.Vector2 window)
        {
            return new Utils.Vector2(window.x / windowWorldRatio.x, window.y / windowWorldRatio.y);
        }

        public Utils.Vector2 UnProjectWorld(Utils.Vector2 world)
        {
            return new Utils.Vector2(world.x * windowWorldRatio.x, world.y * windowWorldRatio.y);
        }

        //Performs camera transformations on world coordinates.
        public Utils.Rect TransformWorld(Utils.Rect window)
        {
            Utils.Rect transform = new Utils.Rect();

            transform.left = (window.left / zoom) + vecPos.x;
            transform.right = (window.right / zoom) + vecPos.x;
            transform.bottom = (window.bottom / zoom) + vecPos.y;
            transform.top = (window.top / zoom) + vecPos.y;

            return transform;
        }

        //Undoes camera transformations on world coordinates.
        public Utils.Rect UntransformWorld(Utils.Rect world)
        {
            Utils.Rect transform = new Utils.Rect();

            transform.left = (world.left - vecPos.x) * zoom;
            transform.right = (world.right - vecPos.x) * zoom;
            transform.bottom = (world.bottom - vecPos.y) * zoom;
            transform.top = (world.top - vecPos.y) * zoom;

            return transform;
        }

        public Utils.Rect ProjectWindow(Utils.Rect window)
        {
            Utils.Rect transform = new Utils.Rect();

            transform.left /= windowWorldRatio.x;
            transform.right /= windowWorldRatio.x;
            transform.bottom /= windowWorldRatio.y;
            transform.top /= windowWorldRatio.y;

            return transform;
        }

        public Utils.Rect UnProjectWorld(Utils.Rect world)
        {
            Utils.Rect transform = new Utils.Rect();

            transform.left *= windowWorldRatio.x;
            transform.right *= windowWorldRatio.x;
            transform.bottom *= windowWorldRatio.y;
            transform.top *= windowWorldRatio.y;

            return transform;
        }

        public double GetZoom()
        {
            return zoom;
        }

		public Utils.Vector2 GetPosition()
		{
			return vecPos;
		}
	}
}
