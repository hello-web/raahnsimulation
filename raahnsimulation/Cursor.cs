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

        public Cursor(Simulator sim) : base(sim)
        {
            texture = TextureManager.TextureType.CURSOR_0;
            width = CURSOR_WIDTH;
            height = CURSOR_HEIGHT;
            aabb.SetSize(width, height);
            lastPos = new Utils.Vector2(0.0, 0.0);
            deltaPos = new Utils.Vector2(0.0, 0.0);
            SetTransformUsage(false);
            Update();
        }

        public override void Update()
        {
            int mouseX;
            int mouseY;

            context.GetWindow().GetPointer(out mouseX, out mouseY);

            //Subtract width / 2 to center the mouse.
            //Subtract height to draw from top to bottom instread of bottom to top.
            double windowX = (double)(mouseX);
            double windowY = (double)(context.GetWindowHeight() - mouseY);

            Utils.Vector2 projection = context.GetCamera().ProjectWindow(windowX, windowY);

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

            RotateAroundCenter();

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