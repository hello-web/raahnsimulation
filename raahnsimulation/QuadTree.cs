using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
    public class QuadTree
    {
        private LinkedList<Entity> outsideTree;
        private AABB treeRegion;
        private QuadTreeNode rootNode;

        public QuadTree()
        {
            treeRegion = null;
            rootNode = null;
            outsideTree = null;
        }

        public QuadTree(AABB bounds)
        {
            SetBounds(bounds);
        }

        public void SetBounds(AABB bounds)
        {
            treeRegion = bounds;
            rootNode = new QuadTreeNode(this, treeRegion);
            outsideTree = new LinkedList<Entity>();
        }

        public void AddEntity(Entity occupant)
        {
            if (treeRegion.Contains(occupant.aabb.GetBounds()))
                rootNode.AddEntity(occupant);
            else
                outsideTree.AddLast(occupant);
        }

        public List<Entity> Query(AABB region)
        {
            List<Entity> occupantsInRegion = new List<Entity>();

            foreach (Entity outsideEntity in outsideTree)
                occupantsInRegion.Add(outsideEntity);

            rootNode.Query(region, occupantsInRegion);

            return occupantsInRegion;
        }

        public void Update()
        {
            List<Entity> reinsertList = new List<Entity>();

            foreach (Entity outsideEntity in outsideTree)
            {
                if (outsideEntity.Moved())
                    reinsertList.Add(outsideEntity);
            }

            for (int i = 0; i < reinsertList.Count; i++)
                outsideTree.Remove(reinsertList[i]);

            rootNode.Update();

            for (int i = 0; i < reinsertList.Count; i++)
                AddEntity(reinsertList[i]);
        }

        public void DebugDraw()
        {
            GL.Disable(EnableCap.Texture2D);

            GL.Color4(0.0, 0.0, 1.0, 0.5);

            GL.PushMatrix();

            rootNode.DebugDraw();

            GL.PopMatrix();

            GL.Color4(1.0, 1.0, 1.0, 1.0);

            GL.Enable(EnableCap.Texture2D);
        }
    }
}

