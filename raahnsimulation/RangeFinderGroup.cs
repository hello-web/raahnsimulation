using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
    [XmlRoot("RangeFinderGroup")]
    public class RangeFinderGroupConfig
    {
        [XmlElement("Count")]
        public uint count;

        [XmlElement("Length")]
        public double length;

        [XmlElement("AngleOffset")]
        public double angleOffset;

        [XmlElement("AngleBetween")]
        public double angleBetween;

        [XmlElement("DetectEntity")]
        public string[] entitiesToDetect;
    }

    public class RangeFinderGroup
    {
        private const double RANGE_FINDER_COLOR_R = 1.0;
        private const double RANGE_FINDER_COLOR_G = 0.0;
        private const double RANGE_FINDER_COLOR_B = 0.0;
        private const double RANGE_FINDER_COLOR_T = 1.0;
        //1.0 for a line.
        public const double LINE_HEIGHT = 1.0;

        private static Mesh line = null;

        private uint count;
        private double defaultLength;
        private double startAngle;
        private double angleSpacing;
        private double[] lengths;
        private double[] activations;
        private List<Entity.EntityType>[] entitiesToDetect;
        private Simulator context;
        private Car robot;
        private QuadTree quadTree;
        private Camera camera;

        public RangeFinderGroup(Simulator sim, Car car, QuadTree tree, uint size)
        {
            context = sim;
            robot = car;
            quadTree = tree;
            camera = context.GetCamera();

            count = size;
            defaultLength = 0.0;
            angleSpacing = 0.0;
            lengths = new double[count];
            activations = new double[count];
            entitiesToDetect = new List<Entity.EntityType>[count];

            for (int i = 0; i < count; i++)
            {
                lengths[i] = 0.0;
                activations[i] = 0.0;

                entitiesToDetect[i] = new List<Entity.EntityType>();
            }

            //The first RangeFinderGroup to use line initializes it.
            if (line == null)
            {
                line = new Mesh(2, BeginMode.Lines);

                float[] vertices = 
                {
                    0.0f, 0.0f,
                    1.0f, 0.0f
                };

                ushort[] indices =
                {
                    0, 1
                };

                line.SetVertices(vertices, false);
                line.SetIndices(indices);
                line.Allocate(BufferUsageHint.StaticDraw);
            }
        }

        public static void Clean()
        {
            if (line != null)
            {
                if (line.Allocated())
                    line.Free();
            }
        }

        public void Configure(double length, double angleOffset, double angleBetween)
        {
            defaultLength = length;
            startAngle = angleOffset;
            angleSpacing = angleBetween;

            for (int i = 0; i < count; i++)
                lengths[i] = length;
        }

        public void AddEntityToDetect(Entity.EntityType entityToDetect)
        {
            for (int i = 0; i < count; i++)
                entitiesToDetect[i].Add(entityToDetect);
        }

        public void Update()
        {
            Utils.Vector2 lowerLeft = camera.TransformWorld(0.0, 0.0);
            Utils.Vector2 upperRight = camera.TransformWorld(Simulator.WORLD_WINDOW_WIDTH, Simulator.WORLD_WINDOW_HEIGHT);

            AABB viewBounds = new AABB(upperRight.x - lowerLeft.x, upperRight.y - lowerLeft.y);
            viewBounds.Translate(lowerLeft.x, lowerLeft.y);

            List<Entity> entitiesInBounds = quadTree.Query(viewBounds);

            for (int i = 0; i < count; i++)
            {
                //If no intersections are found reset to rangeFinderLength.
                double nearestEntityDistance = defaultLength;

                for (int j = 0; j < entitiesInBounds.Count; j++)
                {
                    if (entitiesToDetect[i].Contains(entitiesInBounds[j].GetEntityType()))
                    {
                        double rangeFinderAngle = robot.angle + startAngle + (angleSpacing * i);
                        //Math uses doubles, convert to double.
                        double radians = Utils.DegToRad(rangeFinderAngle);

                        //Calculate end point with (direction * magnitude) + firstPoint.
                        Utils.Vector2 robotCenter = robot.GetCenter();
                        double endPointX = (Math.Cos(radians) * defaultLength) + robotCenter.x;
                        double endPointY = (Math.Sin(radians) * defaultLength) + robotCenter.y;

                        Utils.LineSegment rangeFinderLine = new Utils.LineSegment();
                        //All of the range finders are drawn from the center of the car.
                        rangeFinderLine.SetUp(new Utils.Point2(robotCenter.x, robotCenter.y), new Utils.Point2(endPointX, endPointY));

                        List<Utils.Point2> intersections = entitiesInBounds[j].aabb.IntersectsLineAccurate(rangeFinderLine, new Utils.Point2(robotCenter.x, robotCenter.y));

                        //Make sure there is an intersection.
                        if (intersections.Count > 0)
                        {
                            Utils.Point2 nearest = GetNearestIntersection(intersections);

                            Utils.Point2 centerPoint = new Utils.Point2(robotCenter.x, robotCenter.y);

                            double distance = Utils.GetDist(nearest, centerPoint);

                            //Check to make sure this entity is closer than the last.
                            if (distance < nearestEntityDistance)
                                nearestEntityDistance = distance;
                        }
                    }
                }

                lengths[i] = nearestEntityDistance;
                activations[i] = (defaultLength - lengths[i]) / defaultLength;
            }
        }

        public void Draw()
        {
            line.MakeCurrent();

            GL.Disable(EnableCap.Texture2D);

            GL.Color4(RANGE_FINDER_COLOR_R, RANGE_FINDER_COLOR_G, RANGE_FINDER_COLOR_B, RANGE_FINDER_COLOR_T);

            Utils.Vector2 robotCenter = robot.GetCenter();

            for (int i = 0; i < count; i++)
            {
                GL.PushMatrix();

                double rangeFinderAngle = startAngle + (angleSpacing * i);

                GL.Translate(robotCenter.x, robotCenter.y, Utils.DISCARD_Z_POS);
                GL.Rotate(robot.angle + rangeFinderAngle, 0.0, 0.0, 1.0);
                GL.Translate(-robotCenter.x, -robotCenter.y, -Utils.DISCARD_Z_POS);

                GL.Translate(robotCenter.x, robotCenter.y, Utils.DISCARD_Z_POS);
                GL.Scale(lengths[i], LINE_HEIGHT, Utils.DISCARD_Z_SCALE);

                GL.DrawElements(line.GetRenderMode(), line.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);

                GL.PopMatrix();
            }

            GL.Color4(1.0, 1.0, 1.0, 1.0);

            GL.Enable(EnableCap.Texture2D);
        }

        public uint GetRangeFinderCount()
        {
            return count;
        }

        public double GetRangeFinderValue(uint index)
        {
            if (index >= activations.Length)
                return Utils.INVALID_ACTIVATION;
            else
                return activations[(int)index];
        }

        private Utils.Point2 GetNearestIntersection(List<Utils.Point2> intersections)
        {
            Utils.Point2 nearest = intersections[0];

            for (int x = 1; x < intersections.Count; x++)
            {
                Utils.Point2 currentIntersection = intersections[x];
                Utils.Vector2 robotCenter = robot.GetCenter();
                Utils.Point2 centerPoint = new Utils.Point2(robotCenter.x, robotCenter.y);

                if (Utils.GetDist(nearest, centerPoint) > Utils.GetDist(currentIntersection, centerPoint))
                    nearest = intersections[x];
            }

            return nearest;
        }
    }
}

