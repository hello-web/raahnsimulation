using System;
using System.Collections.Generic;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class State
	{
		protected List<Entity> entityList;
		protected Simulator context;

	    protected State()
	    {
	        context = null;
			entityList = new List<Entity>();
	    }

	    ~State()
	    {
	    }

	    public virtual void Init(Simulator sim)
	    {
	        context = sim;
	    }

	    /*This method should be called at the
	    beginning of the subclass's update method.
	    Make sure to avoid event handling if
	    currentEvent is null*/
	    public virtual void Update()
	    {
	        for (int i = 0; i < entityList.Count; i++)
	            entityList[i].Update();
	    }

        public virtual void UpdateEvent(Event e)
        {
            for (int i = 0; i < entityList.Count; i++)
                entityList[i].UpdateEvent(e);
        }

	    public virtual void Draw()
	    {
	        for (int i = 0; i < entityList.Count; i++)
	        {
	            Gl.glPushMatrix();

	            entityList[i].Draw();

	            Gl.glPopMatrix();

                if (context.debugging)
                {
                    Gl.glPushMatrix();

                    Gl.glDisable(Gl.GL_TEXTURE_2D);

                    Gl.glColor4f(1.0f, 1.0f, 1.0f, 0.5f);

                    // Disable camera transformation.
                    if (entityList[i].drawingVec == entityList[i].windowPos)
                        Gl.glLoadIdentity();

                    entityList[i].DebugDraw();

                    Gl.glPopMatrix();

                    Gl.glEnable(Gl.GL_TEXTURE_2D);

                    Gl.glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
                }
	        }
	    }

	    public virtual void Pause()
	    {
	    }

	    public virtual void Resume()
	    {
	    }

	    public virtual void Clean()
	    {
	        entityList.Clear();
	    }
	}
}
