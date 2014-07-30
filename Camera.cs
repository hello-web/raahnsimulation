using Tao.OpenGl;

namespace RaahnSimulation
{
	public class Camera
	{
		private Utils.Vector2 vecPos;

	    public Camera()
	    {
	        vecPos = new Utils.Vector2(0.0f, 0.0f);
	    }

	    public void Render()
	    {
	        Gl.glTranslatef(-vecPos.x, -vecPos.y, Utils.DISCARD_Z_POS);
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

		public Utils.Vector2 GetPosition()
		{
			return vecPos;
		}
	}
}
