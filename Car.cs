using System;
using System.Collections.Generic;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class Car : Entity
	{
        //Some pie slice sensor and range finder constants are for debugging.
        //They will be loaded from a file later on.

        private const int PIE_SLICE_SENSOR_COUNT = 9;
        private const int PIE_SLICE_SENSOR_MAX_DETECT_COUNT = 3;
        //1.0 for a line.
        public const double LINE_HEIGHT = 1.0;
        private const double RANGE_FINDER_LENGTH = 420.0;
        private const double PIE_SLICE_SENSOR_MIN_ANGLE = -90.0;
        private const double PIE_SLICE_SENSOR_ANGLE = 20.0;
        private const double PIE_SLICE_SENSOR_LENGTH = 400.0;
        private const double PIE_SLICE_SENSOR_OFFSET = 0.0;
        //Relative to the car's length.
        private const double RANGE_FINDER_HIGHEST_ANGLE = 75.0;
        private const double RANGE_FINDER_ANGLE_SPACING = 15.0;
        private const double RANGE_FINDER_COLOR_R = 1.0;
        private const double RANGE_FINDER_COLOR_G = 0.0;
        private const double RANGE_FINDER_COLOR_B = 0.0;
        private const double RANGE_FINDER_COLOR_T = 1.0;
		private const double CAR_SPEED_X = 960.0;
		private const double CAR_SPEED_Y = 540.0;
		//120 degrees per second.
		private const double CAR_ROTATE_SPEED = 120.0;

        private static Mesh line = null;

        public List<Entity> entitiesHovering;
        private int rangeFinderCount;
        private double[] rangeFinderLengths;
        private double[] rangeFinderActivations;
        private List<Entity.EntityType>[] entitiesToDetect;
        private QuadTree quadTree;
        private Camera camera;
        private PieSliceSensorGroup pieSliceSensors;

	    public Car(Simulator sim, QuadTree tree) : base(sim)
	    {
	        texture = TextureManager.TextureType.CAR;

            quadTree = tree;
            camera = context.GetCamera();

            //The first car to use line initializes it.
            if (line == null)
            {
                line = new Mesh(2, Gl.GL_LINES);

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
                line.Allocate(Gl.GL_STATIC_DRAW);
            }

            rangeFinderCount = 11;

            rangeFinderLengths = new double[rangeFinderCount];
            rangeFinderActivations = new double[rangeFinderCount];
            entitiesToDetect = new List<Entity.EntityType>[rangeFinderCount];

            for (int i = 0; i < rangeFinderCount; i++)
            {
                rangeFinderLengths[i] = 0.0;
                rangeFinderActivations[i] = 0.0;

                entitiesToDetect[i] = new List<Entity.EntityType>();
                //Detect roads for now, until walls are added.
                entitiesToDetect[i].Add(Entity.EntityType.ROAD);
            }

            pieSliceSensors = new PieSliceSensorGroup(context, this, quadTree);
            pieSliceSensors.AddSensors(PIE_SLICE_SENSOR_COUNT);
            //For debugging purposes, hard code the number of pie slice sensors for now.
            double sensorAngle = PIE_SLICE_SENSOR_MIN_ANGLE;
            for (int i = 0; i < PIE_SLICE_SENSOR_COUNT; i++)
            {
                pieSliceSensors.ConfigureSensor(i, PIE_SLICE_SENSOR_MAX_DETECT_COUNT, PIE_SLICE_SENSOR_ANGLE, 
                                                PIE_SLICE_SENSOR_LENGTH, sensorAngle, PIE_SLICE_SENSOR_OFFSET);
                //Hard code to road for debugging.
                pieSliceSensors.AddEntityToDetect(i, Entity.EntityType.ROAD);
                sensorAngle += PIE_SLICE_SENSOR_ANGLE;
            }

	        speed.x = CAR_SPEED_X;
	        speed.y = CAR_SPEED_Y;

            entitiesHovering = new List<Entity>();
	    }

        public override void SetWidth(double w)
        {
            base.SetWidth(w);

            for (int i = 0; i < rangeFinderCount; i++)
                rangeFinderLengths[i] = RANGE_FINDER_LENGTH;
        }

	    public override void Update()
	    {
	        if (Keyboard.IsKeyPressed(Keyboard.Key.Left))
	            angle += CAR_ROTATE_SPEED * context.GetDeltaTime();
	        if (Keyboard.IsKeyPressed(Keyboard.Key.Right))
	            angle -= CAR_ROTATE_SPEED * context.GetDeltaTime();

	        if (Keyboard.IsKeyPressed(Keyboard.Key.Up))
	        {
	            transformedWorldPos.x += velocity.x * context.GetDeltaTime();
	            transformedWorldPos.y += velocity.y * context.GetDeltaTime();
	        }
	        if (Keyboard.IsKeyPressed(Keyboard.Key.Down))
	        {
	            transformedWorldPos.x -= velocity.x * context.GetDeltaTime();
	            transformedWorldPos.y -= velocity.y * context.GetDeltaTime();
	        }

            base.Update();

            UpdateRangeFinders();
            pieSliceSensors.Update();
	    }

        public override void UpdateEvent(Event e)
        {
            base.UpdateEvent(e);
        }

        public void UpdateRangeFinders()
        {
            Utils.Vector2 lowerLeft = camera.TransformWorld(0.0, 0.0);
            Utils.Vector2 upperRight = camera.TransformWorld(Simulator.WORLD_WINDOW_WIDTH, Simulator.WORLD_WINDOW_HEIGHT);

            AABB viewBounds = new AABB(upperRight.x - lowerLeft.x, upperRight.y - lowerLeft.y);
            viewBounds.Translate(lowerLeft.x, lowerLeft.y);

            List<Entity> entitiesInBounds = quadTree.Query(viewBounds);

            for (int i = 0; i < rangeFinderCount; i++)
            {
                //If no intersections are found reset to rangeFinderLength.
                double nearestEntityDistance = RANGE_FINDER_LENGTH;

                for (int j = 0; j < entitiesInBounds.Count; j++)
                {
                    if (entitiesToDetect[i].Contains(entitiesInBounds[j].GetEntityType()))
                    {
                        double rangeFinderAngle = angle + RANGE_FINDER_HIGHEST_ANGLE - (RANGE_FINDER_ANGLE_SPACING * i);
                        //Math uses doubles, convert to double.
                        double radians = Utils.DegToRad(rangeFinderAngle);

                        //Calculate end point with (direction * magnitude) + firstPoint.
                        double endPointX = (Math.Cos(radians) * RANGE_FINDER_LENGTH) + center.x;
                        double endPointY = (Math.Sin(radians) * RANGE_FINDER_LENGTH) + center.y;

                        Utils.LineSegment rangeFinderLine = new Utils.LineSegment();
                        //All of the range finders are drawn from the center of the car.
                        rangeFinderLine.SetUp(new Utils.Point2(center.x, center.y), new Utils.Point2(endPointX, endPointY));

                        List<Utils.Point2> intersections = entitiesInBounds[j].aabb.IntersectsLineAccurate(rangeFinderLine, new Utils.Point2(center.x, center.y));

                        //Make sure there is an intersection.
                        if (intersections.Count > 0)
                        {
                            Utils.Point2 nearest = GetNearestIntersection(intersections);

                            Utils.Point2 centerPoint = new Utils.Point2(center.x, center.y);

                            double distance = Utils.GetDist(nearest, centerPoint);

                            //Check to make sure this entity is closer than the last.
                            if (distance < nearestEntityDistance)
                                nearestEntityDistance = distance;
                        }
                    }
                }

                rangeFinderLengths[i] = nearestEntityDistance;
                rangeFinderActivations[i] = (RANGE_FINDER_LENGTH - rangeFinderLengths[i]) / RANGE_FINDER_LENGTH;
            }
        }

		public override void Draw()
	    {
	        base.Draw();

            Gl.glPushMatrix();

	        RotateAroundCenter();

	        Gl.glTranslated(drawingVec.x, drawingVec.y, Utils.DISCARD_Z_POS);
	        Gl.glScaled(width, height, Utils.DISCARD_Z_SCALE);

	        Gl.glDrawElements(mesh.GetRenderMode(), mesh.GetIndexCount(), Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);

            line.MakeCurrent();

            Gl.glPopMatrix();

            Gl.glDisable(Gl.GL_TEXTURE_2D);

            Gl.glColor4d(RANGE_FINDER_COLOR_R, RANGE_FINDER_COLOR_G, RANGE_FINDER_COLOR_B, RANGE_FINDER_COLOR_T);

            for (int i = 0; i < rangeFinderCount; i++)
            {
                Gl.glPushMatrix();

                double rangeFinderAngle = RANGE_FINDER_HIGHEST_ANGLE - (RANGE_FINDER_ANGLE_SPACING * i);

                Gl.glTranslated(center.x, center.y, Utils.DISCARD_Z_POS);
                Gl.glRotated(angle + rangeFinderAngle, 0.0, 0.0, 1.0);
                Gl.glTranslated(-center.x, -center.y, -Utils.DISCARD_Z_POS);

                Gl.glTranslated(center.x, center.y, Utils.DISCARD_Z_POS);
                Gl.glScaled(rangeFinderLengths[i], LINE_HEIGHT, Utils.DISCARD_Z_SCALE);

                Gl.glDrawElements(line.GetRenderMode(), line.GetIndexCount(), Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);

                Gl.glPopMatrix();
            }

            Gl.glColor4d(1.0, 1.0, 1.0, 1.0);

            Gl.glEnable(Gl.GL_TEXTURE_2D);

            pieSliceSensors.Draw();
	    }

        public override void DebugDraw()
        {
            base.DebugDraw();
        }

        public override void Clean()
        {
            line.Free();
        }

        private Utils.Point2 GetNearestIntersection(List<Utils.Point2> intersections)
        {
            Utils.Point2 nearest = intersections[0];

            for (int x = 1; x < intersections.Count; x++)
            {
                Utils.Point2 currentIntersection = intersections[x];
                Utils.Point2 centerPoint = new Utils.Point2(center.x, center.y);

                if (Utils.GetDist(nearest, centerPoint) > Utils.GetDist(currentIntersection, centerPoint))
                    nearest = intersections[x];
            }

            return nearest;
        }
	}
}
