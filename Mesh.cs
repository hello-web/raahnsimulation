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

        //vertexCoordCount refers to the number of coords in a single vertex.
        private int vertexCoordCount;
        private int uvOffset;
        private int renderMode;
        private uint vb;
        private uint ib;
        private bool allocated;
        private bool usesUV;
        private float[] vertices;
        private ushort[] indices;

        //coordCount refers to the number of coords in a single vertex.
        public Mesh(int coordCount, int mode)
        {
            vertexCoordCount = coordCount;
            uvOffset = sizeof(float) * vertexCoordCount;
            renderMode = mode;

            vb = 0;
            ib = 0;

            allocated = false;
            usesUV = false;

            vertices = null;
            indices = null;
        }

        public void SetVertices(float[] coords, bool includesUV)
        {
            vertices = coords;
            usesUV = includesUV;
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

            if (usesUV)
            {
                Gl.glVertexPointer(vertexCoordCount, Gl.GL_FLOAT, Utils.TexturedVertexSize, IntPtr.Zero);
                Gl.glTexCoordPointer(UV_COORD_COUNT, Gl.GL_FLOAT, Utils.TexturedVertexSize, (IntPtr)uvOffset);
            }
            else
                Gl.glVertexPointer(vertexCoordCount, Gl.GL_FLOAT, Utils.VertexSize, IntPtr.Zero);

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
            return currentMesh == this;
        }

        //Returns false if failed to allocate.
        public bool Allocate(int usage)
        {
            if (vertices == null || indices == null || allocated)
                return false;

            Gl.glGenBuffers(1, out vb);
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, vb);
            Gl.glBufferData(Gl.GL_ARRAY_BUFFER, (IntPtr)(sizeof(float) * vertices.Length), vertices, usage);

            Gl.glGenBuffers(1, out ib);
            Gl.glBindBuffer(Gl.GL_ELEMENT_ARRAY_BUFFER, ib);
            Gl.glBufferData(Gl.GL_ELEMENT_ARRAY_BUFFER, (IntPtr)(sizeof(ushort) * indices.Length), indices, usage);

            allocated = true;

            //Invalidate the mesh. It should
            //probably never be equal to this
            //if used properly.
            if (currentMesh != this)
                currentMesh = null;

            return true;
        }

        public bool AllocateEmpty(int vboSize, int iboSize, int usage)
        {
            if (allocated)
                return false;

            Gl.glGenBuffers(1, out vb);
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, vb);
            Gl.glBufferData(Gl.GL_ARRAY_BUFFER, (IntPtr)(sizeof(float) * vboSize), IntPtr.Zero, usage);

            Gl.glGenBuffers(1, out ib);
            Gl.glBindBuffer(Gl.GL_ELEMENT_ARRAY_BUFFER, ib);
            Gl.glBufferData(Gl.GL_ELEMENT_ARRAY_BUFFER, (IntPtr)(sizeof(ushort) * iboSize), IntPtr.Zero, usage);

            allocated = true;

            //Invalidate the mesh. It should
            //probably never be equal to this
            //if used properly.
            if (currentMesh != this)
                currentMesh = null;

            return true;
        }

        public void Update()
        {
            Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, vb);
            Gl.glBufferSubData(Gl.GL_ARRAY_BUFFER, IntPtr.Zero, (IntPtr)(sizeof(float) * vertices.Length), vertices);

            Gl.glBindBuffer(Gl.GL_ELEMENT_ARRAY_BUFFER, ib);
            Gl.glBufferSubData(Gl.GL_ELEMENT_ARRAY_BUFFER, IntPtr.Zero, (IntPtr)(sizeof(ushort) * indices.Length), indices);

            //A new VBO and IBO are bound, but glVertexPointer and
            //glTexCoordPointer still have wrong information.
            currentMesh = null;
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

