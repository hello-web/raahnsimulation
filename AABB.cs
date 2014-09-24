using System;
using System.Collections.Generic;

namespace RaahnSimulation
{
    public class AABB
    {
        //Angle and pos refer to the angle and position of the encapsulated entity.
        private float angle;
        private Utils.Vector2 pos;
        private Utils.Vector2 center;
        private Utils.Rect bounds;

        public AABB()
        {
            Construct();
        }

        public AABB(float w, float h)
        {
            Construct();

            bounds.ll.x = 0.0f;
            bounds.ll.y = 0.0f;

            SetSize(w, h);
        }

        public Utils.Rect GetBounds()
        {
            return bounds;
        }

        public float GetAngle()
        {
            return angle;
        }

        public Utils.Vector2 GetPos()
        {
            return pos;
        }

        public void SetSize(float w, float h)
        {
            float currentAngle = angle;
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

            center.x = bounds.ll.x + (bounds.width / 2.0f);
            center.y = bounds.ll.y + (bounds.height / 2.0f);

            //Restore the pervious rotation.
            Rotate(currentAngle);

            Update();
        }

        public void Rotate(float rotationChange)
        {
            angle += rotationChange;

            float angleTransform = Utils.DegToRad(rotationChange);
            
            RotateVector2(bounds.ll, angleTransform);
            RotateVector2(bounds.lr, angleTransform);
            RotateVector2(bounds.ul, angleTransform);
            RotateVector2(bounds.ur, angleTransform);
            
            Update();
        }

        // Translates by a distance rather than an actual point.
        public void Translate(float xDist, float yDist)
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

        private void Construct()
        {
            angle = 0.0f;

            pos = new Utils.Vector2(0.0f, 0.0f);
            center = new Utils.Vector2(0.0f, 0.0f);
            bounds = new Utils.Rect();
        }

        private void RotateVector2(Utils.Vector2 vec, float angle)
        {
            float xTransform = center.x + (vec.x - center.x) * (float)Math.Cos(angle) - (vec.y - center.y) * (float)Math.Sin(angle);
            float yTransform = center.y + (vec.x - center.x) * (float)Math.Sin(angle) + (vec.y - center.y) * (float)Math.Cos(angle);

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

