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
