using System;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class Graphic : ColorableEntity
	{
		private const double DEFALUT_WIDTH_PERCENTAGE = 0.2f;
		private const double DEFAULT_HEIGHT_PERCENTAGE = 0.2f;

		public Graphic(Simulator sim) : base(sim)
		{
			texture = TextureManager.TextureType.DEFAULT;
			width = DEFALUT_WIDTH_PERCENTAGE * (double)context.GetWindowWidth();
			height = DEFAULT_HEIGHT_PERCENTAGE * (double)context.GetWindowHeight();

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

			Gl.glTranslated(drawingVec.x, drawingVec.y, Utils.DISCARD_Z_POS);
			Gl.glScaled(width, height, Utils.DISCARD_Z_SCALE);

			Gl.glDrawElements(mesh.GetRenderMode(), mesh.GetIndexCount(), Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);
		}

        public override void DebugDraw()
        {
            base.DebugDraw();
        }
	}
}
