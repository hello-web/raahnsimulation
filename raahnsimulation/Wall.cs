using System;
using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
    public class Wall : Entity
    {
        private const uint IBO_SIZE = 2;
        private const double COLOR_R = 0.0;
        private const double COLOR_G = 0.0;
        private const double COLOR_B = 0.0;
        private const double COLOR_A = 1.0;

        private static readonly ushort[] INDICES = { 0, 1 };

        private static Mesh sharedMesh = null;

        //Point to which the line extends to relative to the starting point.
        private double relativeX;
        private double relativeY;
        private float[] vertices;
        private Utils.LineSegment line;

        public Wall(Simulator sim) : base(sim)
        {
            //Second coordinate is the distance from 0,0.
            vertices = new float[]
            {
                0.0f, 0.0f,
                0.0f, 0.0f
            };

            if (sharedMesh == null)
            {
                sharedMesh = new Mesh(2, BeginMode.Lines);
                sharedMesh.AllocateEmpty((uint)vertices.Length, IBO_SIZE, BufferUsageHint.DynamicDraw);
            }

            sharedMesh.SetVertices(vertices, false);
            sharedMesh.SetIndices(INDICES);

            line = new Utils.LineSegment();

            type = EntityType.WALL;

            color.x = COLOR_R;
            color.y = COLOR_G;
            color.z = COLOR_B;
            transparency = COLOR_A;
        }

        public static void CleanShared()
        {
            if (sharedMesh != null)
                sharedMesh.Free();
        }

        public override void SetPosition(double x, double y)
        {
            base.SetPosition(x, y);

            UpdateLine();
        }

        public void SetRelativeEndPoint(double relX, double relY)
        {
            relativeX = relX;
            relativeY = relY;

            vertices[2] = (float)relativeX;
            vertices[3] = (float)relativeY;

            UpdateLine();
        }

        public void UpdateLine()
        {
            Utils.Point2 startPoint = new Utils.Point2(drawingVec.x, drawingVec.y);
            Utils.Point2 endPoint = new Utils.Point2(drawingVec.x + relativeX, drawingVec.y + relativeY);

            line.SetUp(startPoint, endPoint);
        }

        public override void Draw()
        {
            GL.Disable(EnableCap.Texture2D);

            GL.Color4(color.x, color.y, color.z, transparency);

            //Set the shared mesh resource to the wall's vertices and indices.
            sharedMesh.SetVertices(vertices, false);
            sharedMesh.SetIndices(INDICES);
            sharedMesh.Update();

            sharedMesh.MakeCurrent();

            GL.Translate(drawingVec.x, drawingVec.y, Utils.DISCARD_Z_POS);

            GL.DrawElements(sharedMesh.GetRenderMode(), sharedMesh.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);

            GL.Color4(1.0, 1.0, 1.0, 1.0);

            GL.Enable(EnableCap.Texture2D);
        }

        public override void ResetColor()
        {
            color.x = COLOR_R;
            color.y = COLOR_G;
            color.z = COLOR_B;
            transparency = COLOR_A;
        }

        public double GetRelativeX()
        {
            return relativeX;
        }

        public double GetRelativeY()
        {
            return relativeY;
        }

        public Utils.Point2 GetEndPoint()
        {
            return new Utils.Point2(drawingVec.x + relativeX, drawingVec.y + relativeY);
        }

        public Utils.LineSegment GetLineSegment()
        {
            return line;
        }
    }
}