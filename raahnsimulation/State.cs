using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
	public class State
	{
        //Shared mesh resources.
        private static Mesh lineRect;
        private static Mesh quad;

		protected Simulator context;
        protected Gtk.Container mainContainer;
        protected GLWidget mainGLWidget;
        protected Camera camera;
        private bool glInitialized;
        //The top most layer is the last layer, layer 2 would be drawn over layer 1.
        private List<LinkedList<Entity>> layers;
        private TextureManager texMan;

	    protected State()
	    {
	        context = null;

            mainContainer = null;
            mainGLWidget = null;

            glInitialized = false;

			layers = new List<LinkedList<Entity>>();
            //Start off with only one layer.
            layers.Add(new LinkedList<Entity>());

            texMan = null;

            camera = null;
	    }

        public static Mesh GetLineRect()
        {
            return lineRect;
        }

        public static Mesh GetQuad()
        {
            return quad;
        }

        public void AddEntity(Entity entity, uint layerIndex)
        {
            uint layer = layerIndex + 1;

            if (layer > layers.Count)
            {
                int layersNeeded = (int)layer - layers.Count;

                for (int i = 0; i < layersNeeded; i++)
                    layers.Add(new LinkedList<Entity>());
            }

            layers[(int)layerIndex].AddLast(entity);
        }

        public void ChangeLayer(Entity entity, uint newLayerIndex)
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

	    public virtual bool Init(Simulator sim)
	    {
	        context = sim;

            camera = new Camera(context);

            return true;
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
            if (e.type == Gdk.EventType.Configure)
            {
                if (mainGLWidget != null)
                {
                    Gdk.Rectangle widget = mainGLWidget.Allocation;

                    camera.windowWorldRatio.x = (double)widget.Width / Simulator.WORLD_WINDOW_WIDTH;
                    camera.windowWorldRatio.y = (double)widget.Height / Simulator.WORLD_WINDOW_HEIGHT;

                    GL.Viewport(0, 0, widget.Width, widget.Height);
                }
            }

            for (int i = 0; i < layers.Count; i++)
            {
                foreach (Entity curEntity in layers[i])
                    curEntity.UpdateEvent(e);
            }
        }

        public void RenderFrame()
        {
            if (mainGLWidget != null)
            {
                if (mainGLWidget.Visible)
                    mainGLWidget.RenderFrame();
            }
        }

        //This method should be called to draw all entities
        //within the state. and debugging information for them.
	    public virtual void Draw()
	    {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.MatrixMode(MatrixMode.Projection);

            GL.LoadIdentity();

            GL.Ortho(0.0, Simulator.WORLD_WINDOW_WIDTH, 0.0, Simulator.WORLD_WINDOW_HEIGHT, -1.0, 1.0);

            GL.MatrixMode(MatrixMode.Modelview);

            GL.LoadIdentity();

            camera.Transform();

	        for (int i = 0; i < layers.Count; i++)
	        {
                foreach (Entity curEntity in layers[i])
                {
                    if (curEntity.visible)
                    {
                        GL.PushMatrix();

                        Utils.Vector3 color = curEntity.GetColor();

                        GL.Color4(color.x, color.y, color.z, curEntity.GetTransparency());

                        // Disable camera transformation.
                        if (!curEntity.UsesTransform())
                            GL.LoadIdentity();

                        curEntity.Draw();

                        GL.Color4(1.0, 1.0, 1.0, 1.0);

                        GL.PopMatrix();

                        if (context.debugging)
                        {
                            GL.PushMatrix();

                            GL.Disable(EnableCap.Texture2D);

                            GL.Color4(0.0, 0.0, 0.0, 0.5);

                            // Disable camera transformation.
                            if (!curEntity.UsesTransform())
                                GL.LoadIdentity();

                            curEntity.DebugDraw();

                            GL.PopMatrix();

                            GL.Enable(EnableCap.Texture2D);

                            GL.Color4(1.0, 1.0, 1.0, 1.0);
                        }
                    }
                }
	        }
	    }

	    public virtual void Pause()
	    {
            if (mainContainer != null)
            {
                Gtk.Window mainWindow = context.GetWindow();

                if (mainWindow.Child == mainContainer)
                    context.GetWindow().Remove(mainContainer);
            }
	    }

	    public virtual void Resume()
	    {
            if (mainContainer != null)
                context.GetWindow().Add(mainContainer);

            mainContainer.ShowAll();
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

            if (glInitialized)
            {
                //Deletes textures if loaded.
                texMan.DeleteTextures();

                lineRect.Free();
                quad.Free();
            }

            if (mainContainer != null)
            {
                Gtk.Window mainWindow = context.GetWindow();

                if (mainWindow.Child == mainContainer)
                    context.GetWindow().Remove(mainContainer);
            }

            //Free the GL context after deleting GL objects.
            if (mainGLWidget != null)
                mainGLWidget.Dispose();
	    }

        public bool GetGLInitialized()
        {
            return glInitialized;
        }

        public TextureManager GetTexMan()
        {
            return texMan;
        }

        public Camera GetCamera()
        {
            return camera;
        }

        public Gdk.Rectangle GetBounds()
        {
            if (mainGLWidget != null)
                return mainGLWidget.Allocation;
            else
                return default(Gdk.Rectangle);
        }

        protected void InitGraphics()
        {
            //Check to make sure OpenGL 1.5 is supported.
            string glVersion = GL.GetString(StringName.Version).Substring(0, 3);
            Console.Write(Utils.VERBOSE_GL_VERSION);
            Console.WriteLine(glVersion);

            if (double.Parse(glVersion) < Utils.MIN_GL_VERSION)
            {
                Console.WriteLine(Utils.GL_VERSION_UNSUPPORTED);
                return;
            }

            GL.ClearColor(Utils.BACKGROUND_COLOR_VALUE, Utils.BACKGROUND_COLOR_VALUE, Utils.BACKGROUND_COLOR_VALUE, 0.0f);

            //Enable blending for alpha values.
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            texMan = new TextureManager();

            if (!texMan.LoadTextures())
            {
                Console.WriteLine(Utils.TEXTURE_LOAD_FAILED);
                texMan.DeleteTextures();

                return;
            }

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);

            lineRect = new Mesh(2, BeginMode.Lines);

            float[] lrVertices = new float[]
            {
                0.0f, 0.0f,
                1.0f, 0.0f,
                0.0f, 1.0f,
                1.0f, 1.0f
            };

            ushort[] lrIndices =
            {
                0, 1,
                1, 3,
                3, 2,
                2, 0
            };

            lineRect.SetVertices(lrVertices, false);
            lineRect.SetIndices(lrIndices);
            lineRect.Allocate(BufferUsageHint.StaticDraw);

            quad = new Mesh(2, BeginMode.Triangles);

            float[] quadVertices = 
            {
                0.0f, 0.0f, 0.0f, 0.0f,
                1.0f, 0.0f, 1.0f, 0.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 1.0f, 1.0f
            };

            ushort[] quadIndices =
            {
                0, 1, 2,
                2, 3, 1
            };

            quad.SetVertices(quadVertices, true);
            quad.SetIndices(quadIndices);
            //Also makes quad's vertex buffer current.
            quad.Allocate(BufferUsageHint.StaticDraw);
            quad.MakeCurrent();

            Gdk.Rectangle widgetBox = mainGLWidget.Allocation;

            GL.Viewport(0, 0, widgetBox.Width, widgetBox.Height);

            glInitialized = true;
        }
	}
}
