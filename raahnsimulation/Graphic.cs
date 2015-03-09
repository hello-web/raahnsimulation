using System;
using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
	public class Graphic : ColorableEntity
	{
		private const double DEFALUT_WIDTH = 768.0;
		private const double DEFAULT_HEIGHT = 432.0;

		public Graphic(Simulator sim) : base(sim)
		{
			texture = TextureManager.TextureType.DEFAULT;
            width = DEFALUT_WIDTH;
			height = DEFAULT_HEIGHT;

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

			GL.Translate(drawingVec.x, drawingVec.y, Utils.DISCARD_Z_POS);
			GL.Scale(width, height, Utils.DISCARD_Z_SCALE);

			GL.DrawElements(mesh.GetRenderMode(), mesh.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);
		}

        public override void DebugDraw()
        {
            base.DebugDraw();
        }
	}
}
