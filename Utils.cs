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
            public Rect()
            {
                top = 0.0f;
                bottom = 0.0f;
                left = 0.0f;
                right = 0.0f;
            }
        }

        public static readonly Nullable<Event> NULL_EVENT = null;

		public const int EXIT_S = 0;
		public const int EXIT_F = 1;
		public const int INDEX_COUNT = 6;

        public const float MIN_GL_VERSION = 1.5f;
		public const float WIDTH_PERCENTAGE = 0.6f;
		public const float HEIGHT_PERCENTAGE = 0.75f;
		public const float CHAR_WIDTH_PERCENTAGE = 0.03125f;
		public const float CHAR_HEIGHT_PERCENTAGE = 0.0625f;
		public const float DISCARD_Z_POS = 0.0f;
		public const float DISCARD_Z_SCALE = 1.0f;
		public const float DEG_TO_RAD = (float)3.1415926535 / 180.0f;
		public const float TEXTURE_CHAR_WIDTH = 0.166666667f;
		public const float TEXTURE_CHAR_HEIGHT = 0.142857143f;

		public const char FILE_COMMENT = '#';
		public const char FILE_VALUE_SEPERATOR = ' ';
		public const string WINDOW_TITLE = "RAAHN SIMULATION";
		public const string ROAD_FILE = "Data/Roads/default.rd";
		public const string START_SIM = "START RAAHN SIMULATION";
		public const string START_MAP = "CREATE A NEW MAP";
		public const string VERSION_STRING = "VERSION 1";
        //My computer uses es-CO, but our files use points for decimals, hard code culture for now.
        public static readonly CultureInfo EN_US = CultureInfo.CreateSpecificCulture("en-US");

		public static readonly Vector2[] LETTER_POSITIONS = {new Vector2(0.0f, 6.0f), new Vector2(1.0f, 6.0f), new Vector2(2.0f, 6.0f), new Vector2(3.0f, 6.0f),
			new Vector2(4.0f, 6.0f), new Vector2(5.0f, 6.0f), new Vector2(0.0f, 5.0f), new Vector2(1.0f, 5.0f),
			new Vector2(2.0f, 5.0f), new Vector2(3.0f, 5.0f), new Vector2(4.0f, 5.0f), new Vector2(5.0f, 5.0f),
			new Vector2(0.0f, 4.0f), new Vector2(1.0f, 4.0f), new Vector2(2.0f, 4.0f), new Vector2(3.0f, 4.0f),
			new Vector2(4.0f, 4.0f), new Vector2(0.0f, 3.0f), new Vector2(1.0f, 3.0f), new Vector2(2.0f, 3.0f),
			new Vector2(3.0f, 3.0f), new Vector2(0.0f, 2.0f), new Vector2(1.0f, 2.0f), new Vector2(2.0f, 2.0f),
			new Vector2(3.0f, 2.0f), new Vector2(4.0f, 2.0f)};
		public static readonly Vector2[] NUMBER_POSITIONS = {new Vector2(5.0f, 2.0f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(2.0f, 1.0f), new Vector2(3.0f, 1.0f),
			new Vector2(4.0f, 1.0f), new Vector2(5.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(2.0f, 0.0f)};

		public static float DegToRad(float deg)
		{
			return deg * DEG_TO_RAD;
		}
    }
}
