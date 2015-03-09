using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace RaahnSimulation
{
    public class AABB
    {
        //Angle and pos refer to the angle and position of the encapsulated entity.
        private double angle;
        private Utils.Vector2 pos;
        private Utils.Vector2 center;
        private Utils.Rect bounds;
        private Mesh mesh;

        public AABB()
        {
            Construct();
        }

        public AABB(double w, double h)
        {
            Construct();

            bounds.ll.x = 0.0;
            bounds.ll.y = 0.0;

            SetSize(w, h);
        }

        public bool Contains(Utils.Rect rect)
        {
            if (bounds.left < rect.left && bounds.right > rect.right)
            {
                if (bounds.bottom < rect.bottom && bounds.top > rect.top)
                    return true;
            }
            return false;
        }

        public bool Intersects(Utils.Rect r)
        {
            if (r.left >= bounds.right || r.right <= bounds.left
            || r.bottom >= bounds.top || r.top <= bounds.bottom)
                return false;
            else
                return true;
        }

        //Returns the x coordinates at which a line
        //intersects this rectangle. Does not use
        //the axis aligned version.
        //If an interval is found to be the intersection,
        //it returns the closest point to relative.
        public List<Utils.Point2> IntersectsLineAccurate(Utils.LineSegment line, Utils.Point2 relative)
        {
            //4 lines in a rectangle.
            Utils.LineSegment[] rectLines = new Utils.LineSegment[4];

            rectLines[0].SetUp(new Utils.Point2(bounds.ll.x, bounds.ll.y), new Utils.Point2(bounds.lr.x, bounds.lr.y));
            rectLines[1].SetUp(new Utils.Point2(bounds.lr.x, bounds.lr.y), new Utils.Point2(bounds.ur.x, bounds.ur.y));
            rectLines[2].SetUp(new Utils.Point2(bounds.ul.x, bounds.ul.y), new Utils.Point2(bounds.ur.x, bounds.ur.y));
            rectLines[3].SetUp(new Utils.Point2(bounds.ll.x, bounds.ll.y), new Utils.Point2(bounds.ul.x, bounds.ul.y));

            List<Utils.Point2> intersections = new List<Utils.Point2>(4);

            for (int i = 0; i < intersections.Capacity; i++)
            {
                List<Utils.Point2> currentIntersection = line.Intersects(rectLines[i]);

                //There shouldn't be more than 2 points, but just in case.
                if (currentIntersection.Count > 2)
                {
                    if (Utils.GetDist(currentIntersection[0], relative) <= Utils.GetDist(currentIntersection[1], relative))
                        intersections.Add(currentIntersection[0]);
                    else
                        intersections.Add(currentIntersection[1]);
                }
                else
                {
                    for (int j = 0; j < currentIntersection.Count; j++)
                        intersections.Add(currentIntersection[j]);
                }
            }

            return intersections;
        }

        public Utils.Rect GetBounds()
        {
            return bounds;
        }

        public double GetAngle()
        {
            return angle;
        }

        public Utils.Vector2 GetPos()
        {
            return pos;
        }

        public Utils.Vector2 GetCenter()
        {
            return center;
        }

        public void Copy(AABB aabb)
        {
            angle = aabb.GetAngle();
            pos.Copy(aabb.GetPos());
            center.Copy(aabb.GetCenter());
            bounds.Copy(aabb.GetBounds());
        }

        public void SetMesh(Mesh newMesh)
        {
            mesh = newMesh;
        }

        public void SetSize(double w, double h)
        {
            double currentAngle = angle;
            //Undo the rotation so we can scale.
            Rotate(-currentAngle);

            bounds.width = w;
            bounds.height = h;

            bounds.lr.x = bounds.ll.x + bounds.width;
            bounds.lr.y = bounds.ll.y;

            bounds.ul.x = bounds.ll.x;
            bounds.ul.y = bounds.ll.y + bounds.height;

            bounds.ur.x = bounds.ul.x + bounds.width;
            bounds.ur.y = bounds.ul.y;

            bounds.left = bounds.ll.x;
            bounds.right = bounds.lr.x;
            bounds.bottom = bounds.ll.y;
            bounds.top = bounds.ul.y;

            center.x = bounds.ll.x + (bounds.width / 2.0);
            center.y = bounds.ll.y + (bounds.height / 2.0);

            //Restore the pervious rotation.
            Rotate(currentAngle);

            Update();
        }

        public void Rotate(double rotationChange)
        {
            angle += rotationChange;

            double angleTransform = Utils.DegToRad(rotationChange);
            
            RotateVector2(bounds.ll, angleTransform);
            RotateVector2(bounds.lr, angleTransform);
            RotateVector2(bounds.ul, angleTransform);
            RotateVector2(bounds.ur, angleTransform);
            
            Update();
        }

        // Translates by a distance rather than an actual point.
        public void Translate(double xDist, double yDist)
        {
            pos.x += xDist;
            pos.y += yDist;

            bounds.ll.x += xDist;
            bounds.ll.y += yDist;

            bounds.lr.x += xDist;
            bounds.lr.y += yDist;

            bounds.ul.x += xDist;
            bounds.ul.y += yDist;

            bounds.ur.x += xDist;
            bounds.ur.y += yDist;

            center.x += xDist;
            center.y += yDist;

            Update();
        }

        public void DebugDraw()
        {
            if (!mesh.IsCurrent())
                mesh.MakeCurrent();

            GL.Translate(bounds.left, bounds.bottom, Utils.DISCARD_Z_POS);
            GL.Scale(bounds.width, bounds.height, Utils.DISCARD_Z_SCALE);

            GL.DrawElements(mesh.GetRenderMode(), mesh.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);
        }

        private void Construct()
        {
            angle = 0.0;

            pos = new Utils.Vector2(0.0, 0.0);
            center = new Utils.Vector2(0.0, 0.0);
            bounds = new Utils.Rect();

            //Default to quad.
            mesh = Simulator.quad;
        }

        private void RotateVector2(Utils.Vector2 vec, double angle)
        {
            double xTransform = center.x + (vec.x - center.x) * (double)Math.Cos(angle) - (vec.y - center.y) * (double)Math.Sin(angle);
            double yTransform = center.y + (vec.x - center.x) * (double)Math.Sin(angle) + (vec.y - center.y) * (double)Math.Cos(angle);

            vec.x = xTransform;
            vec.y = yTransform;
        }

        private void Update()
        {
            //Allows us to iterate through them.
            List<Utils.Vector2> vectors = new List<Utils.Vector2>();
            vectors.Add(bounds.ll);
            vectors.Add(bounds.lr);
            vectors.Add(bounds.ul);
            vectors.Add(bounds.ur);

            bounds.left = bounds.ll.x;
            bounds.right = bounds.ll.x;
            bounds.bottom = bounds.ll.y;
            bounds.top = bounds.ll.y;

            //Find the correct bounds, skip the first because it is default.
            for (int i = 1; i < vectors.Count; i++)
            {
                if (vectors[i].x < bounds.left)
                    bounds.left = vectors[i].x;
                else if (vectors[i].x > bounds.right)
                    bounds.right = vectors[i].x;

                if (vectors[i].y < bounds.bottom)
                    bounds.bottom = vectors[i].y;
                else if (vectors[i].y > bounds.top)
                    bounds.top = vectors[i].y;
            }

            bounds.width = bounds.right - bounds.left;
            bounds.height = bounds.top - bounds.bottom;
        }
    }
}

