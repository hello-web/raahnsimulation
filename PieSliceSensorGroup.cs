using System;
using System.Collections.Generic;
using Tao.OpenGl;

namespace RaahnSimulation
{
    public class PieSliceSensorGroup
    {
        //360 degree maximum, 2 coords per degree, plus 2 coords for the center of the sensor.
        public const int MAX_VBO_SIZE = 722;
        //Enough indices for a line between each vertex.
        public const int MAX_IBO_SIZE = 722;

        private List<PieSliceSensor> sensors;
        private Simulator context;
        private Car robot;
        //Shared mesh is used for drawing the curve points of each pie slice.
        private Mesh sharedMesh;
        private QuadTree quadTree;

        public PieSliceSensorGroup(Simulator sim, Car car, QuadTree tree)
        {
            context = sim;
            robot = car;
            quadTree = tree;

            sharedMesh = new Mesh(2, Gl.GL_LINE_LOOP);
            sharedMesh.AllocateEmpty(MAX_VBO_SIZE, MAX_IBO_SIZE, Gl.GL_DYNAMIC_DRAW);

            sensors = new List<PieSliceSensor>();
        }

        public int GetSensorCount()
        {
            return sensors.Count;
        }

        public int GetSharedRenderMode()
        {
            return sharedMesh.GetRenderMode();
        }

        public int GetSharedIndexCount()
        {
            return sharedMesh.GetIndexCount();
        }

        public void AddSensors(int sensorCount)
        {
            for (int i = 0; i < sensorCount; i++)
                sensors.Add(new PieSliceSensor(context, this, robot));
        }

        public void SetSharedMesh(float[] vertices, ushort[] indices)
        {
            sharedMesh.SetVertices(vertices, false);
            sharedMesh.SetIndices(indices);
            sharedMesh.Update();
        }

        public void MakeSharedMeshCurrent()
        {
            sharedMesh.MakeCurrent();
        }

        public void ConfigureSensor(int sensorIndex, int detectCount, double angleBetweenLines, double lengthOfLine, double angleOffset, double lineOffset)
        {
            //Make sure the sensor index is valid.
            if (sensors.Count - 1 < sensorIndex)
                return;

            sensors[sensorIndex].Configure(detectCount, angleBetweenLines, lengthOfLine, angleOffset, lineOffset);
        }

        public void AddEntityToDetect(int sensorIndex, Entity.EntityType type)
        {
            sensors[sensorIndex].AddEntityToDetect(type);
        }

        public void Update()
        {
            Utils.Vector2 lowerLeft = context.GetCamera().TransformWorld(0.0, 0.0);
            Utils.Vector2 upperRight = context.GetCamera().TransformWorld(Simulator.WORLD_WINDOW_WIDTH, Simulator.WORLD_WINDOW_HEIGHT);

            AABB viewBounds = new AABB(upperRight.x - lowerLeft.x, upperRight.y - lowerLeft.y);
            viewBounds.Translate(lowerLeft.x, lowerLeft.y);

            List<Entity> entitiesInBounds = quadTree.Query(viewBounds);

            for (int i = 0; i < sensors.Count; i++)
                sensors[i].Reset();

            for (int x = 0; x < entitiesInBounds.Count; x++)
            {
                //Go through each sensor until we find one that
                //contains the current entity (if any).
                for (int y = 0; y < sensors.Count; y++)
                {
                    if (sensors[y].Contains(entitiesInBounds[x]))
                        break;
                }
            }

            for (int i = 0; i < sensors.Count; i++)
                sensors[i].Update();
        }

        public void Draw()
        {
            for (int i = 0; i < sensors.Count; i++)
                sensors[i].Draw();
        }
    }
}

