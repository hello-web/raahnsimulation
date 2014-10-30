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

            public void Copy(Utils.Vector2 copyVec)
            {
                x = copyVec.x;
                y = copyVec.y;
            }

            public static Vector2 operator+(Vector2 u, Vector2 v)
            {
                return new Vector2(u.x + v.x, u.y + v.y);
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

        //Points are structs, vectors are classes.
        public struct Point2
        {
            public float x, y;
            public Point2(float _x, float _y)
            {
                x = _x;
                y = _y;
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

        public struct LineSegment
        {
            public bool vertical;
            public float yIntercept;
            public float slope;
            public float lowerBoundX;
            public float upperBoundX;
            //Vertical lines need y bounds explicitly specified.
            public float lowerBoundY;
            public float upperBoundY;

            public void SetUp(Point2 startPoint, Point2 endPoint)
            {
                float deltaX = endPoint.x - startPoint.x;

                //If the change in x is 0, the slope is undefined.
                if (Math.Abs(deltaX) <= EPSILON)
                {
                    vertical = true;

                    //If the line segment is just a point, it's x coordinate is stored in lowerBoundX.
                    lowerBoundX = startPoint.x;

                    if (endPoint.y > startPoint.y)
                    {
                        upperBoundY = endPoint.y;
                        lowerBoundY = startPoint.y;
                    }
                    else
                    {
                        lowerBoundY = endPoint.y;
                        upperBoundY = startPoint.y;
                    }
                }
                else
                {
                    vertical = false;

                    slope = (endPoint.y - startPoint.y) / deltaX;
                    yIntercept = startPoint.y - (slope * startPoint.x);

                    if (endPoint.x > startPoint.x)
                    {
                        upperBoundX = endPoint.x;
                        lowerBoundX = startPoint.x;
                    }
                    else
                    {
                        lowerBoundX = endPoint.x;
                        upperBoundX = startPoint.x;
                    }
                }
            }

            //Returns infinity if x or the computed y value is invalid.
            public float GetY(float x)
            {
                //Make sure x is in bounds and the line is not vertical.
                if (x >= lowerBoundX && x <= upperBoundX && !vertical)
                    return (slope * x) + yIntercept;//y = mx + b
                else
                    return float.PositiveInfinity;
            }

            //Returns the point of intersection or bounds of intersection within two points.
            public List<Point2> Intersects(LineSegment line)
            {
                List<Point2> intersection = new List<Point2>();

                bool bothVertical = vertical && line.vertical;
                bool parallel = slope == line.slope && !vertical && !line.vertical;

                //Both lines are vertical
                if (bothVertical || parallel)
                {
                    if ((bothVertical && lowerBoundX == line.lowerBoundX) || (parallel && yIntercept == line.yIntercept))
                    {
                        bool lowerInBounds = ValueInBounds(lowerBoundY, line.lowerBoundY, line.upperBoundY);
                        bool upperInBounds = ValueInBounds(upperBoundY, line.lowerBoundY, line.upperBoundY);
                        if (lowerInBounds && upperInBounds)
                        {
                            intersection.Add(new Point2(lowerBoundX, lowerBoundY));
                            intersection.Add(new Point2(lowerBoundX, upperBoundY));
                        }
                        else if (lowerInBounds)
                        {
                            intersection.Add(new Point2(lowerBoundX, lowerBoundY));
                            intersection.Add(new Point2(lowerBoundX, line.upperBoundY));
                        }
                        else if (upperInBounds)
                        {
                            intersection.Add(new Point2(lowerBoundX, line.lowerBoundY));
                            intersection.Add(new Point2(lowerBoundX, upperBoundY));
                        }
                        else if (ValueInBounds(line.lowerBoundY, lowerBoundY, upperBoundY) && ValueInBounds(line.upperBoundY, lowerBoundY, upperBoundY))
                        {
                            intersection.Add(new Point2(lowerBoundX, line.lowerBoundY));
                            intersection.Add(new Point2(lowerBoundX, line.upperBoundY));
                        }
                    }
                    return intersection;
                }
                //If both are not vertical or parallel, there is a single point of intersection.
                else if (vertical)
                {
                    float y = line.GetY(lowerBoundX);

                    //Make sure the returned y is valid.
                    //GetY not returning infinity makes sure that the point is in bounds of this line.
                    if (float.IsInfinity(y) || !line.ValueInBounds(y, lowerBoundY, upperBoundY))
                        return intersection;

                    intersection.Add(new Point2(lowerBoundX, y));

                    return intersection;
                }
                else if (line.vertical)
                {
                    float y = GetY(line.lowerBoundX);

                    //Make sure the returned y is valid.
                    //GetY not returning infinity makes sure that the point is in bounds of line.
                    if (float.IsInfinity(y) || !ValueInBounds(y, line.lowerBoundY, line.upperBoundY))
                        return intersection;

                    intersection.Add(new Point2(line.lowerBoundX, y));

                    return intersection;
                }
                else
                {
                    float intersectionX = (line.yIntercept - yIntercept) / (slope - line.slope);
                    //If the value is within the x bounds of both lines, we don't have to check if GetY returns an invalid number.
                    if (ValueInBounds(intersectionX, lowerBoundX, upperBoundX) && ValueInBounds(intersectionX, line.lowerBoundX, line.upperBoundX))
                        intersection.Add(new Point2(intersectionX, GetY(intersectionX)));

                    return intersection;
                }
            }

            //Checks whether a value is in bounds of two other values.
            public bool ValueInBounds(float value, float lowerBound, float upperBound)
            {
                if (value >= lowerBound && value <= upperBound)
                    return true;
                else
                    return false;
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
        //Chosen as it works for slope calculations for line segments.
        public const double EPSILON = 0.00009;

		public const char FILE_COMMENT = '#';
		public const char FILE_VALUE_SEPERATOR = ' ';
		public const string WINDOW_TITLE = "RAAHN Simulation";
        public const string ROAD_FILE = "Data/Maps/Default.xml";
        public const string DEFAULT_SAVE_FILE = "Data/Maps/OutMap.xml";
		public const string START_SIM = "Start RAAHN simulation";
		public const string START_MAP = "Create a new map";
		public const string VERSION_STRING = "Version 1.91";
        //Error strings.
        public const string TEXTURE_LOAD_FAILED = "Failed to load textures.";
        public const string GL_VERSION_UNSUPPORTED = "GL 1.5 not supported.";
        public const string MAP_ALREADY_LOADED = "Map already loaded.";
        public const string FILE_NOT_FOUND = "File: {0} not found.";
        public const string XML_READ_ERROR = "Error while reading XML. The map may not have been created correctly.";

		public static float DegToRad(float deg)
		{
			return deg * DEG_TO_RAD;
		}

        public static float GetDist(Point2 point0, Point2 point1)
        {
            return (float)Math.Sqrt(Math.Pow(point1.y - point0.y, 2) + Math.Pow(point1.x - point0.x, 2));
        }
    }
}
