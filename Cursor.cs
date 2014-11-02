using System;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class Cursor : Entity
	{
		private const double CURSOR_WIDTH = 96;
		private const double CURSOR_HEIGHT = 108;

		private Utils.Vector2 lastPos;
		private Utils.Vector2 deltaPos;

	    public Cursor(Simulator sim) : base(sim)
		{
	        texture = TextureManager.TextureType.CURSOR_0;
	        width = CURSOR_WIDTH;
            height = CURSOR_HEIGHT;
            aabb.SetSize(width, height);
	        lastPos = new Utils.Vector2(0.0, 0.0);
	        deltaPos = new Utils.Vector2(0.0, 0.0);
			SetTransformUsage(false);
	        Update();
	    }

	    public override void Update()
	    {
	        Vector2i mousePos = Mouse.GetPosition(context.GetWindow());
	        //Subtract width / 2 to center the mouse.
	        //Subtract height to draw from top to bottom instread of bottom to top.
	        double windowX = (double)(mousePos.X);
	        double windowY = (double)(context.GetWindowHeight() - mousePos.Y);
            worldPos.Copy(context.GetCamera().ProjectWindow(windowX, windowY));

	        if (Mouse.IsButtonPressed(Mouse.Button.Left))
	        {
	            if (mousePos.X < (double)context.GetWindowWidth() && mousePos.Y < (double)context.GetWindowHeight())
	            {
	                if (mousePos.X > 0 && mousePos.Y > 0)
	                {
	                    texture = TextureManager.TextureType.CURSOR_1;
	                    deltaPos.x = worldPos.x - lastPos.x;
	                    deltaPos.y = worldPos.y - lastPos.y;
	                }
	            }
	        }
	        else
	            texture = TextureManager.TextureType.CURSOR_0;

	        lastPos.x = worldPos.x;
            lastPos.y = worldPos.y;

	        base.Update();
	    }

        public override void UpdateEvent(Event e)
        {
            base.UpdateEvent(e);
        }

	    public override void Draw()
	    {
	        base.Draw();

	        Gl.glLoadIdentity();

	        RotateAroundCenter();

	        Gl.glTranslated(worldPos.x, worldPos.y, Utils.DISCARD_Z_POS);
	        Gl.glScaled(width, height, Utils.DISCARD_Z_SCALE);
	        Gl.glDrawElements(mesh.GetRenderMode(), mesh.GetIndexCount(), Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);
	    }

        public override void DebugDraw()
        {
            base.DebugDraw();
        }

		public Utils.Vector2 GetDeltaPosition()
		{
			return deltaPos;
		}
	}
}
