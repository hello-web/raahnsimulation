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
        private const float RELATIVE_RANGE_FINDER_LENGTH = 1.25f;
        //1.0f for a line.
        private const float RANGE_FINDER_HEIGHT = 1.0f;
        private const float RANGE_FINDER_HIGHEST_ANGLE = 75.0f;
        private const float RANGE_FINDER_ANGLE_SPACING = 15.0f;
        private const float RANGE_FINDER_COLOR_R = 1.0f;
        private const float RANGE_FINDER_COLOR_G = 0.0f;
        private const float RANGE_FINDER_COLOR_B = 0.0f;
        private const float RANGE_FINDER_COLOR_T = 1.0f;
		private const float CAR_SPEED_X_PERCENTAGE = 0.25f;
		private const float CAR_SPEED_Y_PERCENTAGE = 0.25f;
		//120 degrees per second.
		private const float CAR_ROTATE_SPEED = 120.0f;

        public List<Entity> entitiesHovering;
        private float rangeFinderLength;
        private float[] rangeFinderLengths;
        private float[] rangeFinderActivations;
        private List<Entity.EntityType>[] entitiesToDetect;
        private QuadTree quadTree;
        private Camera camera;

	    public Car(Simulator sim, QuadTree tree) : base(sim)
	    {
	        texture = TextureManager.TextureType.CAR;

            quadTree = tree;
            camera = context.GetCamera();

            rangeFinderLengths = new float[RANGE_FINDER_COUNT];
            rangeFinderActivations = new float[RANGE_FINDER_COUNT];
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

	        speed.x = (float)context.GetWindowWidth() * CAR_SPEED_X_PERCENTAGE;
	        speed.y = (float)context.GetWindowHeight() * CAR_SPEED_Y_PERCENTAGE;

            entitiesHovering = new List<Entity>();
	    }

        public override void SetWidth(float w)
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
            Utils.Vector2 upperRight = camera.WindowToWorld((float)context.GetWindowWidth(), (float)context.GetWindowHeight());

            AABB viewBounds = new AABB(upperRight.x - lowerLeft.x, upperRight.y - lowerLeft.y);
            viewBounds.Translate(lowerLeft.x, lowerLeft.y);

            List<Entity> entitiesInBounds = quadTree.Query(viewBounds);

            for (int i = 0; i < RANGE_FINDER_COUNT; i++)
            {
                //If no intersections are found reset to rangeFinderLength.
                float nearestEntityDistance = rangeFinderLength;

                for (int j = 0; j < entitiesInBounds.Count; j++)
                {
                    if (entitiesToDetect[i].Contains(entitiesInBounds[j].GetEntityType()))
                    {
                        float rangeFinderAngle = angle + RANGE_FINDER_HIGHEST_ANGLE - (RANGE_FINDER_ANGLE_SPACING * i);
                        //Math uses doubles, convert to double.
                        double radians = (double)Utils.DegToRad(rangeFinderAngle);

                        //Calculate end point with (direction * magnitude) + firstPoint.
                        float endPointX = ((float)Math.Cos(radians) * rangeFinderLength) + center.x;
                        float endPointY = ((float)Math.Sin(radians) * rangeFinderLength) + center.y;

                        Utils.LineSegment rangeFinderLine = new Utils.LineSegment();
                        //All of the range finders are drawn from the center of the car.
                        rangeFinderLine.SetUp(new Utils.Point2(center.x, center.y), new Utils.Point2(endPointX, endPointY));

                        List<Utils.Point2> intersections = entitiesInBounds[j].aabb.IntersectsLineAccurate(rangeFinderLine, new Utils.Point2(center.x, center.y));

                        //Make sure there is an intersection.
                        if (intersections.Count > 0)
                        {
                            Utils.Point2 nearest = GetNearestIntersection(intersections);

                            Utils.Point2 centerPoint = new Utils.Point2(center.x, center.y);

                            float distance = Utils.GetDist(nearest, centerPoint);

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

	        Gl.glTranslatef(drawingVec.x, drawingVec.y, Utils.DISCARD_Z_POS);
	        Gl.glScalef(width, height, Utils.DISCARD_Z_SCALE);

	        Gl.glDrawElements(mesh.GetRenderMode(), mesh.GetIndexCount(), Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);

            Gl.glPopMatrix();

            Gl.glDisable(Gl.GL_TEXTURE_2D);

            Gl.glColor4f(RANGE_FINDER_COLOR_R, RANGE_FINDER_COLOR_G, RANGE_FINDER_COLOR_B, RANGE_FINDER_COLOR_T);

            for (int i = 0; i < RANGE_FINDER_COUNT; i++)
            {
                Gl.glPushMatrix();

                RotateAroundCenter();

                float rangeFinderAngle = RANGE_FINDER_HIGHEST_ANGLE - (RANGE_FINDER_ANGLE_SPACING * i);

                Gl.glTranslatef(center.x, center.y, Utils.DISCARD_Z_POS);
                Gl.glRotatef(rangeFinderAngle, 0.0f, 0.0f, 1.0f);
                Gl.glTranslatef(-center.x, -center.y, -Utils.DISCARD_Z_POS);

                Gl.glTranslatef(center.x, center.y, Utils.DISCARD_Z_POS);
                Gl.glScalef(rangeFinderLengths[i], RANGE_FINDER_HEIGHT, Utils.DISCARD_Z_SCALE);

                Gl.glDrawElements(mesh.GetRenderMode(), mesh.GetIndexCount(), Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);

                Gl.glPopMatrix();
            }

            Gl.glColor4f(1.0f, 1.0f, 1.0f, 1.0f);

            Gl.glEnable(Gl.GL_TEXTURE_2D);
	    }

        public override void DebugDraw()
        {
            base.DebugDraw();
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
