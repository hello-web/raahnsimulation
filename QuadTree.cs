using System.Collections.Generic;
using Tao.OpenGl;

namespace RaahnSimulation
{
    public class QuadTree
    {
        private LinkedList<Entity> outsideTree;
        private AABB region;
        private QuadTreeNode rootNode;

        public QuadTree(AABB bounds)
        {
            region = bounds;
            rootNode = new QuadTreeNode(this, region);
            outsideTree = new LinkedList<Entity>();
        }

        ~QuadTree()
        {
            outsideTree.Clear();
        }

        public void AddEntity(Entity occupant)
        {
            if (region.Contains(occupant.aabb.GetBounds()))
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
            Gl.glDisable(Gl.GL_TEXTURE_2D);

            Gl.glColor4d(0.0, 0.0, 1.0, 0.5);

            Gl.glPushMatrix();

            rootNode.DebugDraw();

            Gl.glPopMatrix();

            Gl.glColor4d(1.0, 1.0, 1.0, 1.0);

            Gl.glEnable(Gl.GL_TEXTURE_2D);
        }
    }
}

