using Tao.OpenGl;

namespace RaahnSimulation
{
	public class Camera
	{
        public const double MOUSE_SCROLL_ZOOM = 1.25f;

        private double zoom;
		private Utils.Vector2 vecPos;

	    public Camera()
	    {
	        vecPos = new Utils.Vector2(0.0f, 0.0f);
            Reset();
	    }

        public void Reset()
        {
            zoom = 1.0f;

            vecPos.x = 0.0f;
            vecPos.y = 0.0f;
        }

	    public void Transform()
	    {
            Gl.glScaled(zoom, zoom, Utils.DISCARD_Z_SCALE);
            Gl.glTranslated(-vecPos.x, -vecPos.y, Utils.DISCARD_Z_POS);
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

        public Utils.Vector2 WorldToWindow(double worldX, double worldY)
        {
            double x = (worldX - vecPos.x) * zoom;
            double y = (worldY - vecPos.y) * zoom;
            return new Utils.Vector2(x, y);
        }

        public Utils.Vector2 WindowToWorld(double windowX, double windowY)
        {
            double x = (windowX / zoom) + vecPos.x;
            double y = (windowY / zoom) + vecPos.y;
            return new Utils.Vector2(x, y);
        }

        public Utils.Vector2 WorldToWindow(Utils.Vector2 world)
        {
            return WorldToWindow(world.x, world.y);
        }

        public Utils.Vector2 WindowToWorld(Utils.Vector2 window)
        {
            return WindowToWorld(window.x, window.y);
        }

        //Only transforms bounding properties.
        public Utils.Rect WorldToWindow(Utils.Rect world)
        {
            Utils.Rect transform = new Utils.Rect();

            transform.left = (world.left - vecPos.x) * zoom;
            transform.right = (world.right - vecPos.x) * zoom;
            transform.bottom = (world.bottom - vecPos.y) * zoom;
            transform.top = (world.top - vecPos.y) * zoom;

            return transform;
        }

        //Only transforms bounding properties.
        public Utils.Rect WindowToWorld(Utils.Rect window)
        {
            Utils.Rect transform = new Utils.Rect();

            transform.left = (window.left / zoom) + vecPos.x;
            transform.right = (window.right / zoom) + vecPos.x;
            transform.bottom = (window.bottom / zoom) + vecPos.y;
            transform.top = (window.top / zoom) + vecPos.y;

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
