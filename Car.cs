using System;
using System.Collections.Generic;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class Car : Entity
	{
        private const int RANGE_FINDER_COUNT = 11;
        //Relative to the car's length.
        private const double RELATIVE_RANGE_FINDER_LENGTH = 1.25f;
        //1.0f for a line.
        private const double RANGE_FINDER_HEIGHT = 1.0f;
        private const double RANGE_FINDER_HIGHEST_ANGLE = 75.0f;
        private const double RANGE_FINDER_ANGLE_SPACING = 15.0f;
        private const double RANGE_FINDER_COLOR_R = 1.0f;
        private const double RANGE_FINDER_COLOR_G = 0.0f;
        private const double RANGE_FINDER_COLOR_B = 0.0f;
        private const double RANGE_FINDER_COLOR_T = 1.0f;
		private const double CAR_SPEED_X_PERCENTAGE = 0.25f;
		private const double CAR_SPEED_Y_PERCENTAGE = 0.25f;
		//120 degrees per second.
		private const double CAR_ROTATE_SPEED = 120.0f;

        private static Mesh line = null;

        public List<Entity> entitiesHovering;
        private double rangeFinderLength;
        private double[] rangeFinderLengths;
        private double[] rangeFinderActivations;
        private List<Entity.EntityType>[] entitiesToDetect;
        private QuadTree quadTree;
        private Camera camera;

	    public Car(Simulator sim, QuadTree tree) : base(sim)
	    {
	        texture = TextureManager.TextureType.CAR;

            quadTree = tree;
            camera = context.GetCamera();

            //The first car to use line initializes it.
            if (line == null)
            {
                line = new Mesh(2, Gl.GL_LINES);

                float[] vertices = new float[]
                {
                    0.0f, 0.0f, 0.0f, 0.0f,
                    1.0f, 0.0f, 1.0f, 0.0f
                };

                ushort[] indices =
                {
                    0, 1
                };

                line.SetVerticesWithUV(vertices);
                line.SetIndices(indices);
                line.Allocate();
            }

            rangeFinderLengths = new double[RANGE_FINDER_COUNT];
            rangeFinderActivations = new double[RANGE_FINDER_COUNT];
            entitiesToDetect = new List<Entity.EntityType>[RANGE_FINDER_COUNT];

            //Initialize to 0, SetWidth takes care of its actual length.
            rangeFinderLength = 0.0f;

            for (int i = 0; i < RANGE_FINDER_COUNT; i++)
            {
                rangeFinderLengths[i] = 0.0f;
                rangeFinderActivations[i] = 0.0f;

                entitiesToDetect[i] = new List<Entity.EntityType>();
                //Detect roads for now, until walls are added.
                entitiesToDetect[i].Add(Entity.EntityType.ROAD);
            }

	        speed.x = (double)context.GetWindowWidth() * CAR_SPEED_X_PERCENTAGE;
	        speed.y = (double)context.GetWindowHeight() * CAR_SPEED_Y_PERCENTAGE;

            entitiesHovering = new List<Entity>();
	    }

        public override void SetWidth(double w)
        {
            base.SetWidth(w);

            rangeFinderLength = w * RELATIVE_RANGE_FINDER_LENGTH;
            for (int i = 0; i < RANGE_FINDER_COUNT; i++)
                rangeFinderLengths[i] = rangeFinderLength;
        }

	    public override void Update()
	    {
	        if (Keyboard.IsKeyPressed(Keyboard.Key.Left))
	            angle += CAR_ROTATE_SPEED * context.GetDeltaTime();
	        if (Keyboard.IsKeyPressed(Keyboard.Key.Right))
	            angle -= CAR_ROTATE_SPEED * context.GetDeltaTime();

	        if (Keyboard.IsKeyPressed(Keyboard.Key.Up))
	        {
	            worldPos.x += velocity.x * context.GetDeltaTime();
	            worldPos.y += velocity.y * context.GetDeltaTime();
	        }
	        if (Keyboard.IsKeyPressed(Keyboard.Key.Down))
	        {
	            worldPos.x -= velocity.x * context.GetDeltaTime();
	            worldPos.y -= velocity.y * context.GetDeltaTime();
	        }

            base.Update();

            UpdateRangeFinders();
	    }

        public override void UpdateEvent(Event e)
        {
            base.UpdateEvent(e);
        }

        public void UpdateRangeFinders()
        {
            Utils.Vector2 lowerLeft = camera.WindowToWorld(0.0f, 0.0f);
            Utils.Vector2 upperRight = camera.WindowToWorld((double)context.GetWindowWidth(), (double)context.GetWindowHeight());

            AABB viewBounds = new AABB(upperRight.x - lowerLeft.x, upperRight.y - lowerLeft.y);
            viewBounds.Translate(lowerLeft.x, lowerLeft.y);

            List<Entity> entitiesInBounds = quadTree.Query(viewBounds);

            for (int i = 0; i < RANGE_FINDER_COUNT; i++)
            {
                //If no intersections are found reset to rangeFinderLength.
                double nearestEntityDistance = rangeFinderLength;

                for (int j = 0; j < entitiesInBounds.Count; j++)
                {
                    if (entitiesToDetect[i].Contains(entitiesInBounds[j].GetEntityType()))
                    {
                        double rangeFinderAngle = angle + RANGE_FINDER_HIGHEST_ANGLE - (RANGE_FINDER_ANGLE_SPACING * i);
                        //Math uses doubles, convert to double.
                        double radians = Utils.DegToRad(rangeFinderAngle);

                        //Calculate end point with (direction * magnitude) + firstPoint.
                        double endPointX = (Math.Cos(radians) * rangeFinderLength) + center.x;
                        double endPointY = (Math.Sin(radians) * rangeFinderLength) + center.y;

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
                rangeFinderActivations[i] = (rangeFinderLength - rangeFinderLengths[i]) / rangeFinderLength;
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

            for (int i = 0; i < RANGE_FINDER_COUNT; i++)
            {
                Gl.glPushMatrix();

                RotateAroundCenter();

                double rangeFinderAngle = RANGE_FINDER_HIGHEST_ANGLE - (RANGE_FINDER_ANGLE_SPACING * i);

                Gl.glTranslated(center.x, center.y, Utils.DISCARD_Z_POS);
                Gl.glRotated(rangeFinderAngle, 0.0f, 0.0f, 1.0f);
                Gl.glTranslated(-center.x, -center.y, -Utils.DISCARD_Z_POS);

                Gl.glTranslated(center.x, center.y, Utils.DISCARD_Z_POS);
                Gl.glScaled(rangeFinderLengths[i], RANGE_FINDER_HEIGHT, Utils.DISCARD_Z_SCALE);

                Gl.glDrawElements(line.GetRenderMode(), line.GetIndexCount(), Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);

                Gl.glPopMatrix();
            }

            Gl.glColor4d(1.0f, 1.0f, 1.0f, 1.0f);

            Gl.glEnable(Gl.GL_TEXTURE_2D);
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
