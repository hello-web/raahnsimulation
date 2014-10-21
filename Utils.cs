using System;
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

            public void Copy(Utils.Vector2 copyVec)
            {
                x = copyVec.x;
                y = copyVec.y;
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

            public void Copy(Utils.Vector3 copyVec)
            {
                x = copyVec.x;
                y = copyVec.y;
                z = copyVec.z;
            }
        }

        public struct Vertex
        {
            public Vector3 xyz;
            public Vector2 uv;

            public Vertex(Vector3 vecPos, Vector2 texVec)
            {
                xyz = vecPos;
                uv = texVec;
            }
        }

        public class Rect
        {
            public float left, right, bottom, top;
            public float width, height;
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

            public void Copy(Utils.Rect copyRect)
            {
                left = copyRect.left;
                right = copyRect.right;
                bottom = copyRect.bottom;
                top = copyRect.top;

                width = copyRect.width;
                height = copyRect.height;

                ll.Copy(copyRect.ll);
                lr.Copy(copyRect.lr);
                ul.Copy(copyRect.ul);
                ur.Copy(copyRect.ur);
            }
        }

        public const int VertexSize = sizeof(float) * 4;
		public const int EXIT_S = 0;
		public const int EXIT_F = 1;
        public const uint CHARACTER_TEX_COLUMN_COUNT = 11;
        public const uint CHARACTER_TEX_ROW_COUNT = 9;

        public const float MIN_GL_VERSION = 1.5f;
		public const float WIDTH_PERCENTAGE = 0.6f;
		public const float HEIGHT_PERCENTAGE = 0.75f;
		public const float CHAR_WIDTH_PERCENTAGE = 0.03125f;
		public const float CHAR_HEIGHT_PERCENTAGE = 0.0625f;
		public const float DISCARD_Z_POS = 0.0f;
		public const float DISCARD_Z_SCALE = 1.0f;
        //Color value of background in common GTK apps.
        public const float BACKGROUND_COLOR_VALUE = 0.929411765f;
		public const float DEG_TO_RAD = (float)3.1415926535f / 180.0f;
        public const float TEXTURE_CHAR_WIDTH = 1.0f / (float)CHARACTER_TEX_COLUMN_COUNT;
        public const float TEXTURE_CHAR_HEIGHT = 1.0f / (float)CHARACTER_TEX_ROW_COUNT;

		public const char FILE_COMMENT = '#';
		public const char FILE_VALUE_SEPERATOR = ' ';
		public const string WINDOW_TITLE = "RAAHN Simulation";
		public const string ROAD_FILE = "Data/Roads/default.rd";
		public const string START_SIM = "Start RAAHN simulation";
		public const string START_MAP = "Create a new map";
		public const string VERSION_STRING = "Version 1.7";
        //Error strings.
        public const string TEXTURE_LOAD_FAILED = "Failed to load textures.";
        public const string GL_VERSION_UNSUPPORTED = "GL 1.5 not supported.";

        //My computer uses es-CO, but our files uses points for decimals, hard code culture for now.
        public static readonly CultureInfo EN_US = CultureInfo.CreateSpecificCulture("en-US");

		public static float DegToRad(float deg)
		{
			return deg * DEG_TO_RAD;
		}
    }
}
