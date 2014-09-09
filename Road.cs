using System;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class Road : Entity
	{
	    public Road(Simulator sim) : base(sim)
	    {
	        //Set default texture.
	        texture = TextureManager.TextureType.ROAD_0;
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
	        Gl.glDrawElements(Gl.GL_TRIANGLES, Utils.INDEX_COUNT, Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);
	    }
	}
}
