using System;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class Cursor : Entity
	{
		private const float CURSOR_SIZE_PERCENTAGE_X = 0.025f;
		private const float CURSOR_SIZE_PERCENTAGE_Y = 0.05f;

		private Utils.Vector2 lastPos;
		private Utils.Vector2 deltaPos;

	    public Cursor(Simulator sim) : base(sim)
		{
	        texture = TextureManager.TextureType.CURSOR_0;
	        width = (float)context.GetWindowWidth() * CURSOR_SIZE_PERCENTAGE_X;
	        height = (float)context.GetWindowHeight() * CURSOR_SIZE_PERCENTAGE_Y;
            aabb.SetSize(width, height);
	        lastPos = new Utils.Vector2(0.0f, 0.0f);
	        deltaPos = new Utils.Vector2(0.0f, 0.0f);
			SetWindowAsDrawingVec(true);
	        Update();
	    }

	    public override void Update()
	    {
	        Vector2i mousePos = Mouse.GetPosition(context.GetWindow());
	        //Subtract width / 2 to center the mouse.
	        //Subtract height to draw from top to bottom instread of bottom to top.
	        windowPos.x = (float)(mousePos.X) - (width / 2.0f);
	        windowPos.y = (float)(context.GetWindowHeight() - mousePos.Y) - height;

	        if (Mouse.IsButtonPressed(Mouse.Button.Left))
	        {
	            if (mousePos.X < (float)context.GetWindowWidth() && mousePos.Y < context.GetWindowHeight())
	            {
	                if (mousePos.X > 0 && mousePos.Y > 0)
	                {
	                    texture = TextureManager.TextureType.CURSOR_1;
	                    deltaPos.x = windowPos.x - lastPos.x;
	                    deltaPos.y = windowPos.y - lastPos.y;
	                }
	            }
	        }
	        else
	            texture = TextureManager.TextureType.CURSOR_0;

	        lastPos.x = windowPos.x;
            lastPos.y = windowPos.y;

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

	        Gl.glTranslatef(drawingVec.x, drawingVec.y, Utils.DISCARD_Z_POS);
	        Gl.glScalef(width, height, Utils.DISCARD_Z_SCALE);
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
