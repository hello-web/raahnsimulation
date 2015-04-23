using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
    [XmlRoot("PieSliceSensorGroup")]
    public class PieSliceSensorGroupConfig
    {
        [XmlElement("Count")]
        public uint count;

        [XmlElement("MaxDetection")]
        public int maxDetection;

        [XmlElement("AngleOffset")]
        public double angleOffset;

        [XmlElement("AngleBetween")]
        public double angleBetween;

        [XmlElement("OuterRadius")]
        public double outerRadius;

        [XmlElement("InnerRadius")]
        public double innerRadius;

        [XmlElement("DetectEntity")]
        public string[] entitiesToDetect;
    }

    public class PieSliceSensorGroup
    {
        //360 degree maximum, 2 coords per degree, plus 2 coords for the center of the sensor.
        public const uint MAX_VBO_SIZE = 722;
        //Enough indices for a line between each vertex.
        public const uint MAX_IBO_SIZE = 722;

        private List<PieSliceSensor> sensors;
        private Simulator context;
        private Car robot;
        //Shared mesh is used for drawing the curve points of each pie slice.
        private static Mesh sharedMesh = null;
        private QuadTree quadTree;
        private Camera camera;

        public PieSliceSensorGroup(Simulator sim, Car car, QuadTree tree)
        {
            context = sim;
            robot = car;
            quadTree = tree;

            camera = context.GetState().GetCamera();

            if (sharedMesh == null)
            {
                sharedMesh = new Mesh(2, BeginMode.LineLoop);
                sharedMesh.AllocateEmpty(MAX_VBO_SIZE, MAX_IBO_SIZE, BufferUsageHint.DynamicDraw);
            }

            sensors = new List<PieSliceSensor>();
        }

        public static void Clean()
        {
            if (sharedMesh != null)
            {
                if (sharedMesh.Allocated())
                    sharedMesh.Free();
            }
        }

        public static BeginMode GetSharedRenderMode()
        {
            return sharedMesh.GetRenderMode();
        }

        public static int GetSharedIndexCount()
        {
            return sharedMesh.GetIndexCount();
        }

        public static void SetSharedMesh(float[] vertices, ushort[] indices)
        {
            sharedMesh.SetVertices(vertices, false);
            sharedMesh.SetIndices(indices);
            sharedMesh.Update();
        }

        public static void MakeSharedMeshCurrent()
        {
            sharedMesh.MakeCurrent();
        }

        public int GetSensorCount()
        {
            return sensors.Count;
        }

        public void AddSensors(uint sensorCount)
        {
            for (uint i = 0; i < sensorCount; i++)
                sensors.Add(new PieSliceSensor(context, robot));
        }

        public void ConfigureSensors(int detectCount, double angleOffset, double angleBetweenLines, double lengthOfLine, double lineOffset)
        {
            double sensorAngleOffset = angleOffset;

            for (int i = 0; i < sensors.Count; i++)
            {
                sensors[i].Configure(detectCount, sensorAngleOffset, angleBetweenLines, lengthOfLine, lineOffset);
                sensorAngleOffset += angleBetweenLines;
            }
        }

        public void AddEntityToDetect(Entity.EntityType type)
        {
            for (int i = 0; i < sensors.Count; i++)
                sensors[i].AddEntityToDetect(type);
        }

        public void Update()
        {
            double robotWorldX = robot.GetWorldX();
            double robotWorldY = robot.GetWorldY();

            double llX = robotWorldX - Car.HALF_QUERY_WIDTH;
            double llY = robotWorldY - Car.HALF_QUERY_HEIGHT;

            double urX = robotWorldX + Car.HALF_QUERY_WIDTH;
            double urY = robotWorldY + Car.HALF_QUERY_HEIGHT;

            Utils.Vector2 lowerLeft = camera.TransformWorld(llX, llY);
            Utils.Vector2 upperRight = camera.TransformWorld(urX, urY);

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

        public uint GetPieSliceSensorCount()
        {
            return (uint)sensors.Count;
        }

        public double GetPieSliceSensorValue(uint index)
        {
            if (index >= sensors.Count)
                return Utils.INVALID_ACTIVATION;
            else
                return sensors[(int)index].GetValue();
        }
    }
}

