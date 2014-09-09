using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Globalization;
using SFML.Window;

namespace RaahnSimulation
{
	public class Utils
	{
        public class Vector2
        {
            public float x, y;
            public Vector2(float _x, float _y)
            {
                x = _x;
                y = _y;
            }
        }

        public class Vector3
        {
            public float x, y, z;
            public Vector3(float _x, float _y, float _z)
            {
                x = _x;
                y = _y;
                z = _z;
            }
        }

        public struct Vertex
        {
            public Vector3 xyz;
            public Vector2 uv;
            public static readonly int Size = Marshal.SizeOf(default(Vertex));
            public Vertex(Vector3 vecPos, Vector2 texVec)
            {
                xyz = vecPos;
                uv = texVec;
            }
        }

        public class Rect
        {
            public float top, bottom, left, right;
            public Vector2 ll, lr, ul, ur;
            public Rect()
            {
                ll = new Vector2(0.0f, 0.0f);
                lr = new Vector2(0.0f, 0.0f);
                ul = new Vector2(0.0f, 0.0f);
                ur = new Vector2(0.0f, 0.0f);

                top = 0.0f;
                bottom = 0.0f;
                left = 0.0f;
                right = 0.0f;
            }
        }

        public class AABB
        {
            public Rect GetBounds()
            {
                return bounds;
            }
            private float untransformedWidth;
            private float untransformedHeight;
            private Rect defaultBounds;
            private Rect bounds;
            private Vector2 center;

            public AABB()
            {
                untransformedWidth = 0.0f;
                untransformedHeight = 0.0f;
                defaultBounds = new Utils.Rect();
                bounds = new Utils.Rect();
                center = new Utils.Vector2(0.0f, 0.0f);
            }

            public AABB(float w, float h)
            {
                defaultBounds = new Utils.Rect();
                bounds = new Utils.Rect();
                center = new Utils.Vector2(0.0f, 0.0f);
                UpdateSize(w, h);
            }

            public void UpdateSize(float w, float h)
            {
                bounds.ll.x = defaultBounds.ll.x = 0.0f;
                bounds.ll.y = defaultBounds.ll.y = 0.0f;
                untransformedWidth = w;
                untransformedHeight = h;

                bounds.lr.x = defaultBounds.lr.x = defaultBounds.ll.x + untransformedWidth;
                bounds.lr.y = defaultBounds.lr.y = defaultBounds.ll.y;

                bounds.ul.x = defaultBounds.ul.x = defaultBounds.ll.x;
                bounds.ul.y = defaultBounds.ul.y = defaultBounds.ll.y + untransformedHeight;

                bounds.ur.x = defaultBounds.ur.x = defaultBounds.ul.x + untransformedWidth;
                bounds.ur.y = defaultBounds.ur.y = defaultBounds.ul.y;

                defaultBounds.left = defaultBounds.ll.x;
                defaultBounds.right = defaultBounds.lr.x;
                defaultBounds.bottom = defaultBounds.ll.y;
                defaultBounds.top = defaultBounds.ul.y;

                center.x = bounds.ll.x + untransformedWidth / 2.0f;
                center.y = bounds.ll.y + untransformedHeight / 2.0f;

                Update();
            }

            public void Rotate(float angle)
            {
                bounds.ll.x = center.x + (defaultBounds.ll.x - center.x) * (float)Math.Cos(angle) - (defaultBounds.ll.y - center.y) * (float)Math.Sin(angle);
                bounds.ll.y = center.y + (defaultBounds.ll.x - center.x) * (float)Math.Sin(angle) + (defaultBounds.ll.y - center.y) * (float)Math.Cos(angle);

                bounds.lr.x = center.x + (defaultBounds.lr.x - center.x) * (float)Math.Cos(angle) - (defaultBounds.lr.y - center.y) * (float)Math.Sin(angle);
                bounds.lr.y = center.y + (defaultBounds.lr.x - center.x) * (float)Math.Sin(angle) + (defaultBounds.lr.y - center.y) * (float)Math.Cos(angle);

                bounds.ul.x = center.x + (defaultBounds.ul.x - center.x) * (float)Math.Cos(angle) - (defaultBounds.ul.y - center.y) * (float)Math.Sin(angle);
                bounds.ul.y = center.y + (defaultBounds.ul.x - center.x) * (float)Math.Sin(angle) + (defaultBounds.ul.y - center.y) * (float)Math.Cos(angle);

                bounds.ur.x = center.x + (defaultBounds.ur.x - center.x) * (float)Math.Cos(angle) - (defaultBounds.ur.y - center.y) * (float)Math.Sin(angle);
                bounds.ur.y = center.y + (defaultBounds.ur.x - center.x) * (float)Math.Sin(angle) + (defaultBounds.ur.y - center.y) * (float)Math.Cos(angle);

                Update();
            }

            public void Translate(float x, float y)
            {
                Vector2 difference = new Vector2(0.0f, 0.0f);

                difference.x = x - defaultBounds.ll.x;
                difference.y = y - defaultBounds.ll.y;

                bounds.ll.x = x;
                bounds.ll.y = y;

                bounds.lr.x += difference.x;
                bounds.lr.y += difference.y;

                bounds.ul.x += difference.x;
                bounds.ul.y += difference.y;

                bounds.ur.x += difference.x;
                bounds.ur.y += difference.y;

                center.x += difference.x;
                center.y += difference.y;

                Update();
            }

            private void Update()
            {
                //Allows us to iterate through them.
                List<Vector2> vectors = new List<Vector2>();
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
            }
        }

		public const int EXIT_S = 0;
		public const int EXIT_F = 1;
		public const int INDEX_COUNT = 6;
        public const uint CHARACTER_TEX_COLUMN_COUNT = 11;
        public const uint CHARACTER_TEX_ROW_COUNT = 9;

        public const float MIN_GL_VERSION = 1.5f;
		public const float WIDTH_PERCENTAGE = 0.6f;
		public const float HEIGHT_PERCENTAGE = 0.75f;
		public const float CHAR_WIDTH_PERCENTAGE = 0.03125f;
		public const float CHAR_HEIGHT_PERCENTAGE = 0.0625f;
		public const float DISCARD_Z_POS = 0.0f;
		public const float DISCARD_Z_SCALE = 1.0f;
		public const float DEG_TO_RAD = (float)3.1415926535 / 180.0f;
        public const float TEXTURE_CHAR_WIDTH = 1 / (float)CHARACTER_TEX_COLUMN_COUNT;
        public const float TEXTURE_CHAR_HEIGHT = 1.0f / (float)CHARACTER_TEX_ROW_COUNT;

		public const char FILE_COMMENT = '#';
		public const char FILE_VALUE_SEPERATOR = ' ';
		public const string WINDOW_TITLE = "RAAHN Simulation";
		public const string ROAD_FILE = "Data/Roads/default.rd";
		public const string START_SIM = "Start RAAHN simulation";
		public const string START_MAP = "Create a new map";
		public const string VERSION_STRING = "Version 1.325";
        //My computer uses es-CO, but our files use points for decimals, hard code culture for now.
        public static readonly CultureInfo EN_US = CultureInfo.CreateSpecificCulture("en-US");

		public static float DegToRad(float deg)
		{
			return deg * DEG_TO_RAD;
		}
    }
}
