using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
    public class QuadTreeNode
    {
        private const int SUB_NODE_COUNT = 4;
        private const int MAX_ENTITY_COUNT = 3;
        private const int MIN_ENTITY_COUNT = 1;
        private const int MAX_DEPTH = 5;
        private const int BOTTOM_LEFT_INDEX = 0;
        private const int BOTTOM_RIGHT_INDEX = 1;
        private const int TOP_LEFT_INDEX = 2;
        private const int TOP_RIGHT_INDEX = 3;

        public AABB nodeBounds;
        private bool hasSubNodes;
        private QuadTree quadTree;
        private QuadTreeNode parentNode;
        private List<QuadTreeNode> subNodes;
        private LinkedList<Entity> occupants;

        public QuadTreeNode(QuadTree tree, AABB bounds, QuadTreeNode parent = null)
        {
            hasSubNodes = false;

            quadTree = tree;
            parentNode = parent;

            subNodes = new List<QuadTreeNode>(SUB_NODE_COUNT);
            occupants = new LinkedList<Entity>();
            nodeBounds = new AABB();
            nodeBounds.SetMesh(State.GetLineRect());

            nodeBounds.Copy(bounds);
        }

        public void AddEntity(Entity occupant)
        {
            Utils.Vector2 nodeCenter = nodeBounds.GetCenter();
            Utils.Rect occupantBounds = occupant.aabb.GetBounds();

            if (hasSubNodes)
            {
                //One of the quads on the left, or neither if it does not fit within the entire quad.
                if (occupantBounds.right < nodeCenter.x)
                {
                    //Bottom left
                    if (occupantBounds.top < nodeCenter.y)
                        subNodes[BOTTOM_LEFT_INDEX].AddEntity(occupant);
                    //Top left
                    else if (occupantBounds.bottom > nodeCenter.y)
                        subNodes[TOP_LEFT_INDEX].AddEntity(occupant);
                    else
                        occupants.AddLast(occupant);
                }
                //One of the quads on the right, or neither if it does not fit within the entire quad.
                else if (occupantBounds.left > nodeCenter.x)
                {
                    //Bottom right
                    if (occupantBounds.top < nodeCenter.y)
                        subNodes[BOTTOM_RIGHT_INDEX].AddEntity(occupant);
                    //Top right
                    else if (occupantBounds.bottom > nodeCenter.y)
                        subNodes[TOP_RIGHT_INDEX].AddEntity(occupant);
                    else
                        occupants.AddLast(occupant);
                }
                else
                    occupants.AddLast(occupant);

                if (GetOccupantCount() < MIN_ENTITY_COUNT)
                    Consolidate();
            }
            else
            {
                occupants.AddLast(occupant);

                //No reason to use GetOccupantCount because
                //we know there are no sub nodes in this node.
                if (occupants.Count > MAX_ENTITY_COUNT && GetDepth() < MAX_DEPTH)
                    SubDivide();
            }
        }

        public void Query(AABB region, List<Entity> occupantsInRegion)
        {
            if (nodeBounds.Intersects(region.GetBounds()))
            {
                foreach (Entity occupant in occupants)
                    occupantsInRegion.Add(occupant);

                if (hasSubNodes)
                {
                    for (int i = 0; i < subNodes.Count; i++)
                        subNodes[i].Query(region, occupantsInRegion);
                }
            }
        }

        public void SubDivide()
        {
            //In case this method is accidentally called when subnodes exist.
            if (hasSubNodes)
                return;

            Utils.Vector2 bCenter = nodeBounds.GetCenter();
            Utils.Rect bBox = nodeBounds.GetBounds();

            double subNodeWidth = bBox.width / 2.0;
            double subNodeHeight = bBox.height / 2.0;

            //Bottom left.
            AABB newNodeBounds = new AABB(subNodeWidth, subNodeHeight);
            newNodeBounds.Translate(bBox.left, bBox.bottom);
            subNodes.Add(new QuadTreeNode(quadTree, newNodeBounds, this));

            //Bottom right.
            newNodeBounds = new AABB(subNodeWidth, subNodeHeight);
            newNodeBounds.Translate(bCenter.x, bBox.bottom);
            subNodes.Add(new QuadTreeNode(quadTree, newNodeBounds, this));

            //Top left.
            newNodeBounds = new AABB(subNodeWidth, subNodeHeight);
            newNodeBounds.Translate(bBox.left, bCenter.y);
            subNodes.Add(new QuadTreeNode(quadTree, newNodeBounds, this));

            //Top right.
            newNodeBounds = new AABB(subNodeWidth, subNodeHeight);
            newNodeBounds.Translate(bCenter.x, bCenter.y);
            subNodes.Add(new QuadTreeNode(quadTree, newNodeBounds, this));

            List<Entity> toRemove = new List<Entity>();

            foreach (Entity occupant in occupants)
            {
                Utils.Vector2 nodeCenter = nodeBounds.GetCenter();
                Utils.Rect occupantBounds = occupant.aabb.GetBounds();

                if (occupantBounds.right < nodeCenter.x)
                {
                    //Bottom left
                    if (occupantBounds.top < nodeCenter.y)
                    {
                        subNodes[BOTTOM_LEFT_INDEX].AddEntity(occupant);
                        toRemove.Add(occupant);
                    }
                    //Top left
                    else if (occupantBounds.bottom > nodeCenter.y)
                    {
                        subNodes[TOP_LEFT_INDEX].AddEntity(occupant);
                        toRemove.Add(occupant);
                    }

                }
                //One of the quads on the right, or neither if it does not fit within the entire quad.
                else if (occupantBounds.left > nodeCenter.x)
                {
                    //Bottom right
                    if (occupantBounds.top < nodeCenter.y)
                    {
                        subNodes[BOTTOM_RIGHT_INDEX].AddEntity(occupant);
                        toRemove.Add(occupant);
                    }
                    //Top right
                    else if (occupantBounds.bottom > nodeCenter.y)
                    {
                        subNodes[TOP_RIGHT_INDEX].AddEntity(occupant);
                        toRemove.Add(occupant);
                    }
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
                occupants.Remove(toRemove[i]);

            hasSubNodes = true;
        }

        public void Consolidate()
        {
            //If there are no sub nodes, the node does not need to be consolidated.
            if (!hasSubNodes)
                return;

            for (int i = 0; i < subNodes.Count; i++)
            {
                //Consolidate subNodes first if needed.
                if (subNodes[i].HasSubNodes())
                    subNodes[i].Consolidate();

                LinkedList<Entity> subOccupants = subNodes[i].GetOccupants();

                foreach (Entity entity in subOccupants)
                    occupants.AddLast(entity);

                subOccupants.Clear();
            }
            subNodes.Clear();

            hasSubNodes = false;
        }

        public int GetOccupantCount()
        {
            int count = occupants.Count;

            for (int i = 0; i < subNodes.Count; i++)
                count += subNodes[i].GetOccupantCount();

            return count;
        }

        public int GetDepth()
        {
            int depth = 0;

            for (QuadTreeNode node = parentNode; node != null; node = node.GetParentNode())
                depth++;

            return depth;
        }

        public bool HasSubNodes()
        {
            return hasSubNodes;
        }

        public LinkedList<Entity> GetOccupants()
        {
            return occupants;
        }

        public QuadTreeNode GetParentNode()
        {
            return parentNode;
        }

        public void Update()
        {
            //Create a list for reinserting after removal.
            List<Entity> reinsertList = new List<Entity>();

            foreach (Entity occupant in occupants)
            {
                //If the entity moved, reinsert it.
                if (occupant.Moved())
                    reinsertList.Add(occupant);
            }

            for (int i = 0; i < reinsertList.Count; i++)
                Remove(reinsertList[i]);

            for (int i = 0; i < subNodes.Count; i++)
                subNodes[i].Update();

            for (int i = 0; i < reinsertList.Count; i++)
                quadTree.AddEntity(reinsertList[i]);
        }

        public void DebugDraw()
        {
            GL.PushMatrix();

            nodeBounds.DebugDraw();

            GL.PopMatrix();

            for (int i = 0; i < subNodes.Count; i++)
            {
                GL.PushMatrix();

                subNodes[i].DebugDraw();

                GL.PopMatrix();
            }
        }

        //Used internally to remove an element known in the node.
        private void Remove(Entity occupant)
        {
            occupants.Remove(occupant);

            //If enough of the occupants within the node move
            //that are not within sub nodes, then Consolidate
            //will be called anyway.
            if (GetOccupantCount() < MIN_ENTITY_COUNT && hasSubNodes)
                Consolidate();
        }
    }
}

