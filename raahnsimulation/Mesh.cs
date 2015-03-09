using System;
using OpenTK.Graphics.OpenGL;

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
        private uint vb;
        private uint ib;
        private bool allocated;
        private bool usesUV;
        private float[] vertices;
        private ushort[] indices;
        private BeginMode renderMode;

        //coordCount refers to the number of coords in a single vertex.
        public Mesh(int coordCount, BeginMode mode)
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

        public void SetRenderMode(BeginMode mode)
        {
            renderMode = mode;
        }

        public void MakeCurrent()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vb);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ib);

            if (usesUV)
            {
                GL.VertexPointer(vertexCoordCount, VertexPointerType.Float, Utils.TexturedVertexSize, IntPtr.Zero);
                GL.TexCoordPointer(UV_COORD_COUNT, TexCoordPointerType.Float, Utils.TexturedVertexSize, (IntPtr)uvOffset);
            }
            else
                GL.VertexPointer(vertexCoordCount, VertexPointerType.Float, Utils.VertexSize, IntPtr.Zero);

            currentMesh = this;
        }

        public BeginMode GetRenderMode()
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
        public bool Allocate(BufferUsageHint usage)
        {
            if (vertices == null || indices == null || allocated)
                return false;

            GL.GenBuffers(1, out vb);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vb);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * vertices.Length), vertices, usage);

            GL.GenBuffers(1, out ib);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ib);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(sizeof(ushort) * indices.Length), indices, usage);

            allocated = true;

            //Invalidate the mesh. It should
            //probably never be equal to this
            //if used properly.
            if (currentMesh != this)
                currentMesh = null;

            return true;
        }

        public bool AllocateEmpty(uint vboSize, uint iboSize, BufferUsageHint usage)
        {
            if (allocated)
                return false;

            GL.GenBuffers(1, out vb);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vb);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * vboSize), IntPtr.Zero, usage);

            GL.GenBuffers(1, out ib);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ib);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(sizeof(ushort) * iboSize), IntPtr.Zero, usage);

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
            GL.BindBuffer(BufferTarget.ArrayBuffer, vb);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, (IntPtr)(sizeof(float) * vertices.Length), vertices);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ib);
            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, (IntPtr)(sizeof(ushort) * indices.Length), indices);

            //A new VBO and IBO are bound, but glVertexPointer and
            //glTexCoordPointer still have wrong information.
            currentMesh = null;
        }

        public bool Allocated()
        {
            return allocated;
        }

        //Returns false if failed to free.
        public bool Free()
        {
            if (!allocated)
                return false;

            GL.DeleteBuffers(1, ref vb);
            GL.DeleteBuffers(1, ref ib);

            allocated = false;

            return true;
        }

        public static Mesh GetCurrentMesh()
        {
            return currentMesh;
        }
    }
}

