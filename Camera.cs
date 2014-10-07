using Tao.OpenGl;

namespace RaahnSimulation
{
	public class Camera
	{
        public const float MOUSE_SCROLL_ZOOM = 0.25f;

        private float zoom;
		private Utils.Vector2 vecPos;
        private Utils.Vector2 zoomPoint;

	    public Camera()
	    {
	        vecPos = new Utils.Vector2(0.0f, 0.0f);
            zoomPoint = new Utils.Vector2(0.0f, 0.0f);
            Reset();
	    }

        public void Reset()
        {
            zoom = 1.0f;

            vecPos.x = 0.0f;
            vecPos.y = 0.0f;

            zoomPoint.x = 0.0f;
            zoomPoint.y = 0.0f;
        }

	    public void Transform()
	    {
	        Gl.glTranslatef(-vecPos.x, -vecPos.y, Utils.DISCARD_Z_POS);

            Gl.glTranslatef(zoomPoint.x, zoomPoint.y, Utils.DISCARD_Z_SCALE);
            Gl.glScalef(zoom, zoom, Utils.DISCARD_Z_SCALE);
            Gl.glTranslatef(-zoomPoint.x, -zoomPoint.y, -Utils.DISCARD_Z_SCALE);
	    }

	    public void SetPosition(Utils.Vector2 pos)
	    {
	        vecPos.x = pos.x;
	        vecPos.y = pos.y;
	    }

	    public void IncrementPosition(Utils.Vector2 pos)
	    {
	        vecPos.x += pos.x;
	        vecPos.y += pos.y;
	    }

        public void SetZoom(float newZoom)
        {
            zoom = newZoom;
        }

        public void SetZoomPoint(float x, float y)
        {
            zoomPoint.x = x;
            zoomPoint.y = y;
        }

        public void IncrementZoom(float zoomChange)
        {
            zoom += zoomChange;
        }

		public Utils.Vector2 GetPosition()
		{
			return vecPos;
		}
	}
}
