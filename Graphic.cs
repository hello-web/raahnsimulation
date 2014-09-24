using System;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class Graphic : Entity
	{
		private const float DEFALUT_WIDTH_PERCENTAGE = 0.2f;
		private const float DEFAULT_HEIGHT_PERCENTAGE = 0.2f;
        private Utils.Vector3 color;
        private float transparency;

		public Graphic(Simulator sim) : base(sim)
		{
			texture = TextureManager.TextureType.DEFAULT;
			width = DEFALUT_WIDTH_PERCENTAGE * (float)context.GetWindowWidth();
			height = DEFAULT_HEIGHT_PERCENTAGE * (float)context.GetWindowHeight();

            //Default to white and completely opaque.
            color = new Utils.Vector3(1.0f, 1.0f, 1.0f);
            transparency = 1.0f;

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

            Gl.glColor4f(color.x, color.y, color.z, transparency);

			Gl.glTranslatef(drawingVec.x, drawingVec.y, Utils.DISCARD_Z_POS);
			Gl.glScalef(width, height, Utils.DISCARD_Z_SCALE);

			Gl.glDrawElements(Gl.GL_TRIANGLES, Utils.INDEX_COUNT, Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);

            Gl.glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
		}

        public override void DebugDraw()
        {
            base.DebugDraw();
        }

        public void SetColor(float x, float y, float z, float t)
        {
            color.x = x;
            color.y = y;
            color.z = z;
            transparency = t;
        }
	}
}
