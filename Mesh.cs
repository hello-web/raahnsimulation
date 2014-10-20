using System;
using Tao.OpenGl;

namespace RaahnSimulation
{
    public class Mesh
    {
        //Size as in number of elements.
        private const int UV_COORD_COUNT = 2;

        //Pointer to current mesh.
        private static Mesh currentMesh = null;

        //Size as in number of elements.
        private int vertexCoordCount;
        private int uvOffset;
        private int renderMode;
        private uint vb;
        private uint ib;
        private bool allocated;
        private float[] verticesWithUV;
        private ushort[] indices;

        public Mesh(int coordCount, int mode)
        {
            vertexCoordCount = coordCount;
            uvOffset = sizeof(float) * vertexCoordCount;
            renderMode = mode;

            vb = 0;
            ib = 0;

            allocated = false;

            verticesWithUV = null;
            indices = null;
        }

        public void SetVerticesWithUV(float[] coords)
        {
            verticesWithUV = coords;
        }

        public void SetIndices(ushort[] i)
        {
            indices = i;
        }

        public void SetRenderMode(int mode)
        {
            renderMode = mode;
        }

        public void MakeCurrent()
        {
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, vb);
            Gl.glBindBuffer(Gl.GL_ELEMENT_ARRAY_BUFFER, ib);

            Gl.glVertexPointer(vertexCoordCount, Gl.GL_FLOAT, Utils.VertexSize, IntPtr.Zero);
            Gl.glTexCoordPointer(UV_COORD_COUNT, Gl.GL_FLOAT, Utils.VertexSize, (IntPtr)uvOffset);

            currentMesh = this;
        }

        public int GetRenderMode()
        {
            return renderMode;
        }

        public int GetIndexCount()
        {
            return indices.Length;
        }

        public bool IsCurrent()
        {
            if (currentMesh == this)
                return true;
            else
                return false;
        }

        //Returns false if failed to allocate.
        public bool Allocate()
        {
            if (verticesWithUV == null || indices == null || allocated)
                return false;

            Gl.glGenBuffers(1, out vb);
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, vb);
            Gl.glBufferData(Gl.GL_ARRAY_BUFFER, (IntPtr)(sizeof(float) * verticesWithUV.Length), verticesWithUV, Gl.GL_STATIC_DRAW);

            Gl.glGenBuffers(1, out ib);
            Gl.glBindBuffer(Gl.GL_ELEMENT_ARRAY_BUFFER, ib);
            Gl.glBufferData(Gl.GL_ELEMENT_ARRAY_BUFFER, (IntPtr)(sizeof(ushort) * indices.Length), indices, Gl.GL_STATIC_DRAW);

            allocated = true;

            return true;
        }

        //Returns false if failed to free.
        public bool Free()
        {
            if (!allocated)
                return false;

            Gl.glDeleteBuffers(1, ref vb);
            Gl.glDeleteBuffers(1, ref ib);

            allocated = false;

            return true;
        }

        public static Mesh GetCurrentMesh()
        {
            return currentMesh;
        }
    }
}

