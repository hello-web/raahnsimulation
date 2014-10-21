using System;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class Road : ColorableEntity
	{
        //Roads are squares, so only one constant is needed.
        public const float ROAD_DIMENSION_PERCENTAGE = 0.1f;

	    public Road(Simulator sim) : base(sim)
	    {
            type = EntityType.ROAD;
	        //Set default texture.
	        texture = TextureManager.TextureType.ROAD_0;

            width = height = (float)context.GetWindowWidth() * ROAD_DIMENSION_PERCENTAGE;
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

	        RotateAroundCenter();

	        Gl.glTranslatef(drawingVec.x, drawingVec.y, Utils.DISCARD_Z_POS);
	        Gl.glScalef(width, height, Utils.DISCARD_Z_SCALE);
	        Gl.glDrawElements(mesh.GetRenderMode(), mesh.GetIndexCount(), Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);
	    }

        public override void DebugDraw()
        {
            base.DebugDraw();
        }
	}
}
