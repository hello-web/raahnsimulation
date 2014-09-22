using System;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class Car : Entity
	{
		private const float CAR_SPEED_X_PERCENTAGE = 0.25f;
		private const float CAR_SPEED_Y_PERCENTAGE = 0.25f;
		//120 degrees per second.
		private const float CAR_ROTATE_SPEED = 120.0f;

	    public Car(Simulator sim) : base(sim)
	    {
	        texture = TextureManager.TextureType.CAR;
	        speed.x = (float)context.GetWindowWidth() * CAR_SPEED_X_PERCENTAGE;
	        speed.y = (float)context.GetWindowHeight() * CAR_SPEED_Y_PERCENTAGE;
	    }

	    public override void Update()
	    {
	        if (Keyboard.IsKeyPressed(Keyboard.Key.Left))
	            angle += CAR_ROTATE_SPEED * context.GetDeltaTime();
	        if (Keyboard.IsKeyPressed(Keyboard.Key.Right))
	            angle -= CAR_ROTATE_SPEED * context.GetDeltaTime();

	        base.Update();

	        if (Keyboard.IsKeyPressed(Keyboard.Key.Up))
	        {
	            worldPos.x += velocity.x * context.GetDeltaTime();
	            worldPos.y += velocity.y * context.GetDeltaTime();
	        }
	        if (Keyboard.IsKeyPressed(Keyboard.Key.Down))
	        {
	            worldPos.x -= velocity.x * context.GetDeltaTime();
	            worldPos.y -= velocity.y * context.GetDeltaTime();
	        }
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
	        Gl.glDrawElements(Gl.GL_TRIANGLES, Utils.INDEX_COUNT, Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);
	    }

        public override void DebugDraw()
        {
            base.DebugDraw();
        }
	}
}
