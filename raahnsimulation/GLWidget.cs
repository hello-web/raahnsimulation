using System;
using Tao.OpenGl;
using Gtk;
using GtkGL;

namespace RaahnSimulation 
{
    class GLWidget : GLArea 
    {
        public delegate void GLEvent();

        private const int DEFAULT_WIDTH = 300;
        private const int DEFAULT_HEIGHT = 300;

        private static readonly Int32[] DEFAULT_ATTRIBUTES = 
        {
            (int)GtkGL._GDK_GL_CONFIGS.Rgba,
            (int)GtkGL._GDK_GL_CONFIGS.RedSize,1,
            (int)GtkGL._GDK_GL_CONFIGS.GreenSize,1,
            (int)GtkGL._GDK_GL_CONFIGS.BlueSize,1,
            (int)GtkGL._GDK_GL_CONFIGS.DepthSize,1,
            (int)GtkGL._GDK_GL_CONFIGS.Doublebuffer,
            (int)GtkGL._GDK_GL_CONFIGS.None,
        };

        private GLEvent onInit;
        private GLEvent onResize;
        private GLEvent onDraw;

        public GLWidget(GLEvent init, GLEvent draw, GLEvent resize) : base(DEFAULT_ATTRIBUTES) 
        {
            Construct(init, draw, resize);
        }

        public GLWidget(GLEvent init, GLEvent draw, GLEvent resize, Int32[] attributes) : base(attributes)
        {
            Construct(init, draw, resize);
        }

        private void Construct(GLEvent init, GLEvent draw, GLEvent resize)
        {
            SetSizeRequest(DEFAULT_WIDTH, DEFAULT_HEIGHT);

            Realized += OnRealized;

            onInit = init;
            onResize = resize;
            onDraw = draw;
        }

        //Init GL states.
        private void OnRealized(object o, EventArgs e)
        {
            if (MakeCurrent() == 0)
                return;

            onInit();
        }

        //The widget is resized, change the viewport.
        public void OnConfigure()
        {   
            if (MakeCurrent() == 0)
                return;

            onResize();
        }

        //Render the frame here.
        public void RenderFrame()
        {
            if (MakeCurrent() == 0)
                return;

            onDraw();

            SwapBuffers();
        }
    }
}