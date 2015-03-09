using System;
using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
    public class Wall : Entity
    {
        private const uint VBO_SIZE = 4;
        private const uint IBO_SIZE = 2;
        private const double COLOR_R = 0.0;
        private const double COLOR_G = 1.0;
        private const double COLOR_B = 0.0;
        private const double COLOR_A = 0.0;

        private static readonly ushort[] indices = { 0, 1 };

        private static Mesh sharedMesh = null;

        private float[] vertices;

        public Wall(Simulator sim) : base(sim)
        {
            if (sharedMesh == null)
            {
                sharedMesh = new Mesh(2, BeginMode.Lines);
                sharedMesh.AllocateEmpty(VBO_SIZE, IBO_SIZE, BufferUsageHint.StaticDraw);
            }

            //Second coordinate is the distance from 0,0.
            vertices = new float[VBO_SIZE]
            {
                0.0f, 0.0f,
                0.0f, 0.0f
            };

            type = EntityType.WALL;
        }

        ~Wall()
        {
            if (sharedMesh != null)
                sharedMesh.Free();
        }

        public void SetRelativeEndPoint(double relX, double relY)
        {
            vertices[2] = (float)relX;
            vertices[3] = (float)relY;
        }

        public override void Draw()
        {
            GL.Color4(COLOR_R, COLOR_G, COLOR_B, COLOR_A);

            //Set the shared mesh resource to the wall's vertices and indices.
            sharedMesh.SetVertices(vertices, false);
            sharedMesh.SetIndices(indices);
            sharedMesh.Update();

            sharedMesh.MakeCurrent();

            GL.Translate(drawingVec.x, drawingVec.y, Utils.DISCARD_Z_POS);
            GL.DrawElements(sharedMesh.GetRenderMode(), sharedMesh.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);

            GL.Color4(1.0, 1.0, 1.0, 1.0);
        }
    }
}