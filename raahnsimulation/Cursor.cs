using System;
using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
    public class Cursor : Entity
    {
        private const double CURSOR_WIDTH = 96;
        private const double CURSOR_HEIGHT = 108;

        private Utils.Vector2 lastPos;
        private Utils.Vector2 deltaPos;
        private GLWidget mainGLWidget;

        public Cursor(Simulator sim, GLWidget glWidget) : base(sim)
        {
            texture = TextureManager.TextureType.CURSOR_0;

            width = CURSOR_WIDTH;
            height = CURSOR_HEIGHT;

            aabb.SetSize(width, height);

            lastPos = new Utils.Vector2(0.0, 0.0);
            deltaPos = new Utils.Vector2(0.0, 0.0);

            SetTransformUsage(false);

            mainGLWidget = glWidget;

            Update();
        }

        public override void Update()
        {
            int mouseX;
            int mouseY;

            mainGLWidget.GetPointer(out mouseX, out mouseY);

            Gdk.Rectangle glBounds = currentState.GetBounds();

            //Subtract height / 2 to vertically position from the middle of the cursor.
            double windowX = (double)(mouseX);
            double windowY = (double)(glBounds.Height - mouseY);

            Utils.Vector2 projection = camera.ProjectWindow(windowX, windowY);

            drawingVec.x = projection.x;
            drawingVec.y = projection.y;

            if (context.GetLeftMouseButtonDown())
            {
                if (mouseX < (double)context.GetWindowWidth() && mouseY < (double)context.GetWindowHeight())
                {
                    if (mouseX > 0 && mouseY > 0)
                    {
                        texture = TextureManager.TextureType.CURSOR_1;
                        deltaPos.x = drawingVec.x - lastPos.x;
                        deltaPos.y = drawingVec.y - lastPos.y;
                    }
                }
            }
            else
                texture = TextureManager.TextureType.CURSOR_0;

            lastPos.x = drawingVec.x;
            lastPos.y = drawingVec.y;

            base.Update();
        }

        public override void UpdateEvent(Event e)
        {
            base.UpdateEvent(e);
        }

        public override void Draw()
        {
            base.Draw();

            GL.LoadIdentity();

            GL.Translate(center.x, center.y, Utils.DISCARD_Z_POS);
            GL.Rotate(angle, 0.0, 0.0, 1.0);
            GL.Translate(-center.x, -center.y, -Utils.DISCARD_Z_POS);

            GL.Translate(drawingVec.x, drawingVec.y, Utils.DISCARD_Z_POS);
            GL.Scale(width, height, Utils.DISCARD_Z_SCALE);
            GL.DrawElements(mesh.GetRenderMode(), mesh.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);
        }

        public override void DebugDraw()
        {
            base.DebugDraw();
        }

        public Utils.Vector2 GetDeltaPosition()
        {
            return deltaPos;
        }
    }
}