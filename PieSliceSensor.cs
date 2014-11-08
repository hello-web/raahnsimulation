using System;
using System.Collections.Generic;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
    public class PieSliceSensor
    {
        private const double DEFAULT_COLOR_R = 0.0;
        private const double DEFAULT_COLOR_G = 0.8;
        private const double DEFAULT_COLOR_B = 0.0;
        private const double DEFAULT_COLOR_T = 1.0;
        private const double CONTAINS_COLOR_R = 0.0;
        private const double CONTAINS_COLOR_G = 0.0;
        private const double CONTAINS_COLOR_B = 1.0;
        private const double CONTAINS_COLOR_T = 1.0;
        private const double MAX_ACTIVATION = 1.0;

        private int maxEntitiesToDetect;
        private double radius;
        private double angleBetween;
        private double offsetLength;
        private double angle;
        private double activation;
        private double transparency;
        private List<float> curvePoints;
        private List<ushort> indices;
        private List<Entity.EntityType> entitiesToDetect;
        private List<Entity> entitiesContained;
        private PieSliceSensorGroup sensorGroup;
        private Car robot;
        private Utils.Vector3 color;

        public PieSliceSensor(Simulator sim, PieSliceSensorGroup group, Car car)
        {
            robot = car;
            sensorGroup = group;

            maxEntitiesToDetect = 0;
            angleBetween = 0.0;
            radius = 0.0;
            offsetLength = 0.0;
            angle = 0.0;
            activation = 0.0;
            transparency = DEFAULT_COLOR_T;

            curvePoints = new List<float>(PieSliceSensorGroup.MAX_VBO_SIZE);
            indices = new List<ushort>(PieSliceSensorGroup.MAX_IBO_SIZE);
            entitiesToDetect = new List<Entity.EntityType>();
            entitiesContained = new List<Entity>();
            color = new Utils.Vector3(DEFAULT_COLOR_R, DEFAULT_COLOR_G, DEFAULT_COLOR_B);
        }

        public bool Contains(Entity entity)
        {
            Utils.Vector2 robotCenter = robot.GetCenter();

            if (!entitiesToDetect.Contains(entity.GetEntityType()))
                return false;
            else
            {
                Utils.Vector2 entityCenter = entity.GetCenter();

                double dist = Utils.GetDist(new Utils.Point2(robotCenter.x, robotCenter.y), 
                                            new Utils.Point2(entityCenter.x, entityCenter.y));

                //Check if the point is within any pie slice.
                if (dist < offsetLength || dist > radius)
                    return false;

                double xDifference = entityCenter.x - robotCenter.x;
                double yDifference = entityCenter.y - robotCenter.y;
                double entityAngle = Utils.RadToDeg(Math.Atan2(yDifference, xDifference));

                if (entityAngle < 0.0)
                    entityAngle += 360.0;

                double angleLowerBound = robot.angle + angle;

                //Set the bound to be within [0.0,360.0]
                while (angleLowerBound > 360.0)
                    angleLowerBound -= 360.0;
                while (angleLowerBound < 0.0)
                    angleLowerBound += 360.0;

                if (entityAngle >= angleLowerBound && entityAngle <= angleLowerBound + angleBetween)
                {
                    color.x = CONTAINS_COLOR_R;
                    color.y = CONTAINS_COLOR_G;
                    color.z = CONTAINS_COLOR_B;
                    transparency = CONTAINS_COLOR_T;

                    entitiesContained.Add(entity);

                    return true;
                }
                else
                    return false;
            }
        }

        public double GetActivation()
        {
            return activation;
        }

        public void Configure(int detectCount, double angleBetweenLines, double lineLength, double angleOffset, double lineOffset)
        {
            //Make sure the angle given is in [0, 360]
            if (angleBetweenLines < 0.0 || angleBetweenLines > 360.0)
                return;

            maxEntitiesToDetect = detectCount;
            angleBetween = angleBetweenLines;
            radius = lineLength;
            angle = angleOffset;
            offsetLength = lineOffset;

            //If this function has already been called, get rid of the old curve points.
            if (curvePoints.Count > 0)
                curvePoints.Clear();

            double normalizedOffsetLength = offsetLength / radius;

            curvePoints.Add((float)normalizedOffsetLength);
            curvePoints.Add(0.0f);

            int angleBetweeni = (int)angleBetween;

            //Generate the top curve points.
            for (int i = 0; i <= angleBetweeni; i++)
            {
                double radians = Utils.DegToRad((double)i);
                //lineLength is the radius of the partial circle.
                curvePoints.Add((float)(Math.Cos(radians)));
                curvePoints.Add((float)(Math.Sin(radians)));
            }

            //No reason to add the second curve if there is no line offset.
            if (offsetLength > 0.0)
            {
                double angleRadians = Utils.DegToRad(angleBetween);

                curvePoints.Add((float)(Math.Cos(angleRadians) * normalizedOffsetLength));
                curvePoints.Add((float)(Math.Sin(angleRadians) * normalizedOffsetLength));

                //Generate the bottom curve points.
                for (int i = angleBetweeni; i > 0; i--)
                {
                    double radians = Utils.DegToRad((double)i);
                    //lineLength is the radius of the partial circle.
                    curvePoints.Add((float)(Math.Cos(radians) * normalizedOffsetLength));
                    curvePoints.Add((float)(Math.Sin(radians) * normalizedOffsetLength));
                }
            }

            //Divide by 2 because there are two values for each point.
            //Skip the last point as there is no lastPointIndex + 1.
            ushort lastPointIndex = (ushort)((curvePoints.Count / 2) - 1);

            for (ushort i = 0; i < lastPointIndex; i++)
            {
                indices.Add(i);
                indices.Add((ushort)(i + 1));
            }
        }

        public void AddEntityToDetect(Entity.EntityType type)
        {
            entitiesToDetect.Add(type);
        }

        public void Reset()
        {
            color.x = DEFAULT_COLOR_R;
            color.y = DEFAULT_COLOR_G;
            color.z = DEFAULT_COLOR_B;
            transparency = DEFAULT_COLOR_T;

            entitiesContained.Clear();
        }

        public void Update()
        {
            if (entitiesContained.Count >= maxEntitiesToDetect)
                activation = MAX_ACTIVATION;
            else
                activation = (double)entitiesContained.Count / (double)maxEntitiesToDetect;
        }

        public void Draw()
        {
            Utils.Vector2 robotCenter = robot.GetCenter();

            sensorGroup.SetSharedMesh(curvePoints.ToArray(), indices.ToArray());
            sensorGroup.MakeSharedMeshCurrent();

            Gl.glDisable(Gl.GL_TEXTURE_2D);

            Gl.glColor4d(color.x, color.y, color.z, transparency);

            Gl.glPushMatrix();

            Gl.glTranslated(robotCenter.x, robotCenter.y, Utils.DISCARD_Z_POS);
            Gl.glRotated(robot.angle + angle, 0.0, 0.0, 1.0);
            Gl.glTranslated(-robotCenter.x, -robotCenter.y, -Utils.DISCARD_Z_POS);

            Gl.glTranslated(robotCenter.x, robotCenter.y, Utils.DISCARD_Z_POS);
            Gl.glScaled(radius, radius, Utils.DISCARD_Z_SCALE);

            Gl.glDrawElements(sensorGroup.GetSharedRenderMode(), sensorGroup.GetSharedIndexCount(), Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);

            Gl.glPopMatrix();

            Gl.glColor4d(1.0, 1.0, 1.0, 1.0);

            Gl.glEnable(Gl.GL_TEXTURE_2D);
        }
    }
}

