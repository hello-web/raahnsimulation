using System;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class Graphic : Entity
	{
		private const float DEFALUT_WIDTH_PERCENTAGE = 0.2f;
		private const float DEFAULT_HEIGHT_PERCENTAGE = 0.2f;

		public Graphic(Simulator sim) : base(sim)
		{
			texture = TextureManager.TextureType.DEFAULT;
			width = DEFALUT_WIDTH_PERCENTAGE * (float)context.GetWindowWidth();
			height = DEFAULT_HEIGHT_PERCENTAGE * (float)context.GetWindowHeight();
            aabb.SetSize(width, height);
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
