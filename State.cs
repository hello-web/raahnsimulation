using System;
using System.Collections.Generic;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class State
	{
		protected Simulator context;
        //The top most layer is the last layer, layer 2 would be drawn over layer 1.
        private List<LinkedList<Entity>> layers;

	    protected State()
	    {
	        context = null;
			layers = new List<LinkedList<Entity>>();
            //Start off with only one layer.
            layers.Add(new LinkedList<Entity>());
	    }

        public void AddEntity(Entity entity, int layerIndex)
        {
            int layer = layerIndex + 1;
            if (layer > layers.Count)
            {
                for (int i = 0; i < layer; i++)
                    layers.Add(new LinkedList<Entity>());
            }
            layers[layerIndex].AddLast(entity);
        }

        public void ChangeLayer(Entity entity, int newLayerIndex)
        {
            bool shouldBreak = false;
            bool removed = false;

            for (int i = 0; i < layers.Count; i++)
            {
                foreach (Entity curEntity in layers[i])
                {
                    if (curEntity == entity)
                    {
                        //If the new layer is the same as the old, skip it.
                        if (i != newLayerIndex)
                        {
                            layers[i].Remove(entity);
                            removed = true;
                        }
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak)
                {
                    //If the layer has no more entities remove it, unless it is
                    //the first layer because we always want at least one layer.
                    if (layers[i].Count == 0 && i != 0)
                        layers.RemoveAt(i);
                    break;
                }
            }
            if (removed)
                AddEntity(entity, newLayerIndex);
        }

        public void RemoveEntity(Entity entity)
        {
            bool shouldBreak = false;

            for (int i = 0; i < layers.Count; i++)
            {
                foreach (Entity curEntity in layers[i])
                {
                    if (curEntity == entity)
                    {
                        layers[i].Remove(entity);
                        shouldBreak = true;
                        break;
                    }
                }
                if (shouldBreak)
                {
                    //If the layer has no more entities remove it.
                    if (layers[i].Count == 0)
                        layers.RemoveAt(i);
                    break;
                }
            }
        }

        public int GetTopLayerIndex()
        {
            return layers.Count - 1;
        }

	    public virtual void Init(Simulator sim)
	    {
	        context = sim;
	    }

	    //This method should be called to update
        //all entities within the state.
	    public virtual void Update()
	    {
            for (int i = 0; i < layers.Count; i++)
            {
                foreach (Entity curEntity in layers[i])
                    curEntity.Update();
            }
	    }

        //This method should be called to update all
        //entities within the state with an event that just occured.
        public virtual void UpdateEvent(Event e)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                foreach (Entity curEntity in layers[i])
                    curEntity.UpdateEvent(e);
            }
        }

        //This method should be called to draw all entities
        //within the state. and debugging information for them.
	    public virtual void Draw()
	    {
	        for (int i = 0; i < layers.Count; i++)
	        {
                foreach (Entity curEntity in layers[i])
                {
                    if (curEntity.visible)
                    {
                        Gl.glPushMatrix();

                        Utils.Vector3 color = curEntity.GetColor();

                        Gl.glColor4d(color.x, color.y, color.z, curEntity.GetTransparency());

                        // Disable camera transformation.
                        if (curEntity.drawingVec == curEntity.windowPos)
                            Gl.glLoadIdentity();

                        curEntity.Draw();

                        Gl.glColor4d(1.0f, 1.0f, 1.0f, 1.0f);

                        Gl.glPopMatrix();

                        if (context.debugging)
                        {
                            Gl.glPushMatrix();

                            Gl.glDisable(Gl.GL_TEXTURE_2D);

                            Gl.glColor4d(0.0f, 0.0f, 0.0f, 0.5f);

                            // Disable camera transformation.
                            if (curEntity.drawingVec == curEntity.windowPos)
                                Gl.glLoadIdentity();

                            curEntity.DebugDraw();

                            Gl.glPopMatrix();

                            Gl.glEnable(Gl.GL_TEXTURE_2D);

                            Gl.glColor4d(1.0f, 1.0f, 1.0f, 1.0f);
                        }
                    }
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
            for (int i = 0; i < layers.Count; i++)
            {
                foreach (Entity entity in layers[i])
                    entity.Clean();
                layers[i].Clear();
            }

            layers.Clear();
	    }
	}
}
