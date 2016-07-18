using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Globalization;
using OpenTK;
using OpenTK.Graphics;
using Raahn;

namespace RaahnSimulation
{
    public class Utils
    {
        public class Vector2
        {
            public double x, y;

            public Vector2(double _x, double _y)
            {
                x = _x;
                y = _y;
            }

            public void Copy(Utils.Vector2 copyVec)
            {
                x = copyVec.x;
                y = copyVec.y;
            }

            public double GetMagnitude()
            {
                return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
            }

            public double DotProduct(Vector2 vec)
            {
                return (x * vec.x + y * vec.y);
            }

            public double AngleBetween(Utils.Vector2 vec)
            {
                double dotProduct = DotProduct(vec);
                double magnitudeProduct = GetMagnitude() * vec.GetMagnitude();

                return Utils.RadToDeg(Math.Acos(dotProduct / magnitudeProduct));
            }

            public static Vector2 operator+(Vector2 u, Vector2 v)
            {
                return new Vector2(u.x + v.x, u.y + v.y);
            }
        }

        public class Vector3
        {
            public double x, y, z;

            public Vector3(double _x, double _y, double _z)
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

            public double GetMagnitude()
            {
                return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));
            }

            public double DotProduct(Vector3 vec)
            {
                return (x * vec.x + y * vec.y + z * vec.z);
            }
        }

        public class Rect
        {
            public double left, right, bottom, top;
            public double width, height;
            public Vector2 ll, lr, ul, ur;

            public Rect()
            {
                ll = new Vector2(0.0, 0.0);
                lr = new Vector2(0.0, 0.0);
                ul = new Vector2(0.0, 0.0);
                ur = new Vector2(0.0, 0.0);

                top = 0.0;
                bottom = 0.0;
                left = 0.0;
                right = 0.0;
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

            public bool Intersects(double x, double y)
            {
                if (x >= left && x <= right)
                {
                    if (y >= bottom && y <= top)
                        return true;
                }
                return false;
            }

            public bool Intersects(Utils.Rect r)
            {
                if (r.left >= right || r.right <= left
                    || r.bottom >= top || r.top <= bottom)
                    return false;
                else
                    return true;
            }
        }

        public class Option
        {
            //Number of arguments the option has.
            public uint argCount;
            public string optString;

            public Option(uint aCount, string oString)
            {
                argCount = aCount;
                optString = oString;
            }
        }

        //Points are structs, vectors are classes.
        public struct Point2
        {
            public double x, y;
            public Point2(double _x, double _y)
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
            public double yIntercept;
            public double slope;
            public double lowerBoundX;
            public double upperBoundX;
            //Vertical lines need y bounds explicitly specified.
            public double lowerBoundY;
            public double upperBoundY;

            public void SetUp(Point2 startPoint, Point2 endPoint)
            {
                double deltaX = endPoint.x - startPoint.x;

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
            public double GetY(double x)
            {
                //Make sure x is in bounds and the line is not vertical.
                if (x >= lowerBoundX && x <= upperBoundX && !vertical)
                    return (slope * x) + yIntercept;//y = mx + b
                else
                    return double.PositiveInfinity;
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
                    double y = line.GetY(lowerBoundX);

                    //Make sure the returned y is valid.
                    //GetY not returning infinity makes sure that the point is in bounds of this line.
                    if (double.IsInfinity(y) || !line.ValueInBounds(y, lowerBoundY, upperBoundY))
                        return intersection;

                    intersection.Add(new Point2(lowerBoundX, y));

                    return intersection;
                }
                else if (line.vertical)
                {
                    double y = GetY(line.lowerBoundX);

                    //Make sure the returned y is valid.
                    //GetY not returning infinity makes sure that the point is in bounds of line.
                    if (double.IsInfinity(y) || !ValueInBounds(y, line.lowerBoundY, line.upperBoundY))
                        return intersection;

                    intersection.Add(new Point2(line.lowerBoundX, y));

                    return intersection;
                }
                else
                {
                    double intersectionX = (line.yIntercept - yIntercept) / (slope - line.slope);
                    //If the value is within the x bounds of both lines, we don't have to check if GetY returns an invalid number.
                    if (ValueInBounds(intersectionX, lowerBoundX, upperBoundX) && ValueInBounds(intersectionX, line.lowerBoundX, line.upperBoundX))
                        intersection.Add(new Point2(intersectionX, GetY(intersectionX)));

                    return intersection;
                }
            }

            //Checks whether a value is in bounds of two other values.
            public bool ValueInBounds(double value, double lowerBound, double upperBound)
            {
                if (value >= lowerBound && value <= upperBound)
                    return true;
                else
                    return false;
            }
        }

        public static Utils.Point2 GetNearestIntersection(List<Utils.Point2> intersections, Utils.Vector2 center)
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

        public const int TexturedVertexSize = sizeof(float) * 4;
        public const int VertexSize = sizeof(float) * 2;
        public const int EXIT_S = 0;
        public const int EXIT_F = 1;
        public const int GTK_BUTTON_LEFT = 1;
        public const int GTK_BUTTON_RIGHT = 3;
        public const int NO_PADDING = 0;
        public const uint CHARACTER_TEX_COLUMN_COUNT = 11;
        public const uint CHARACTER_TEX_ROW_COUNT = 9;

        //Color value of background in common GTK apps.
        public const float BACKGROUND_COLOR_VALUE = 0.929411765f;
        public const double INVALID_ACTIVATION = -1.0;
        public const double MIN_GL_VERSION = 1.5;
        public const double WINDOW_WIDTH_PERCENTAGE = 0.6;
        public const double WINDOW_HEIGHT_PERCENTAGE = 0.7;
        public const double MENU_WINDOW_WIDTH_PERCENTAGE = 0.4;
        public const double MENU_WINDOW_HEIGHT_PERCENTAGE = 0.2;
        public const double VISUALIZER_WINDOW_WIDTH_PERCENTAGE = 0.4;
        public const double VISUALIZER_WINDOW_HEIGHT_PERCENTAGE = 0.5;
        public const double DISCARD_Z_POS = 0.0;
        public const double DISCARD_Z_SCALE = 1.0;
        public const double DEG_TO_RAD = 3.1415926535 / 180.0;
        public const double TEXTURE_CHAR_WIDTH = 1.0 / CHARACTER_TEX_COLUMN_COUNT;
        public const double TEXTURE_CHAR_HEIGHT = 1.0 / CHARACTER_TEX_ROW_COUNT;
        //Chosen as it works for slope calculations for line segments.
        public const double EPSILON = 0.00000002;

        public const string WINDOW_TITLE = "RAAHN Simulation";
        public const string WINDOW_VISUALIZER_TITLE = "Network Visualizer";
        public const string START_SIM = "Start RAAHN simulation";
        public const string START_MAP = "Create a new map";
        public const string PEFORMANCE = "Peformance";
        public const string TICKS_ELAPSED = "Ticks Elapsed";
        public const string DELAY_DESCRIPTION = "Delay (Milli)";
        public const string SIMULATION_FRAME = "Simulation";
        public const string MAP_FRAME = "Map";
        public const string MAP_CONTROLS_TITLE = "Modes";
        public const string MENU_FILE = "File";
        public const string MENU_SAVE = "Save";
        public const string MENU_VIEW = "View";
        public const string MENU_VISUALIZER = "Network Visualizer";
        public const string MENU_HELP = "Help";
        public const string MENU_ABOUT = "About";
        public const string MODULATION_DESCRIPTION = "Index#, Modulation: ";
        public const string ERROR_DESCRIPTION = "Autoencoder error: ";
        public const string DEBUG_FRAME = "Modulation and Error";
        public const string HEBBIAN_TRAIN = "Hebbian";
        public const string AUTOENCODER_TRAIN = "Autoencoder";
        public const string POINT_ICON = "Data/Icons/Point.png";
        public const string LINE_ICON = "Data/Icons/Line.png";
        public const string SELECT_ICON = "Data/Icons/Select.png";
        public const string MAP_FOLDER = "Data/Maps/";
        public const string SENSOR_FOLDER = "Data/Sensors/";
        public const string NETWORK_FOLDER = "Data/Networks/";
        public const string EXPERIMENT_FOLDER = "Data/Experiments/";
        public const string LOG_FOLDER = "Data/Logs/";
        public const string TIME_ELAPSED = "Total Time Elapsed: {0}s";
        public const string VERSION_STRING = "Version 20160717";
        //Log strings.
        public const string LOG_SCORE_FILE = "Scores.txt";
        public const string LOG_SCORE_FORMAT = "{0} ";
        //Dialog strings.
        public const string SAVE_FILE = "Choose a file name and location.";
        public const string CHOOSE_EXPERIMENT_FILE = "Choose an experiment file.";
        public const string OPEN_BUTTON = "Open";
        public const string SAVE_BUTTON = "Save";
        public const string CANCEL_BUTTON = "Cancel";
        //Tooltips
        public const string WALL_TOOLTIP = "Wall";
        public const string POINT_TOOLTIP = "Point";
        public const string SELECT_TOOLTIP = "Select";
        //File extensions.
        public const string MAP_FILE_EXTENSION = ".xml";
        //Verbose strings.
        public const string VERBOSE_SIM_START = "RAAHNSimulation " + VERSION_STRING + "\n" +
            "Simulation started.";
        public const string VERBOSE_GL_VERSION = "GL Version ";
        public const string VERBOSE_HELP = 
            "--headless\t\tRun the simulation in headless mode. An experiment file must be specified.\n" +
                "--experiment\t\t[experimentfile.xml] Uses experimentfile.xml for headless mode runs.\n" +
                "--help\t\t\tDisplays this help message.";
        //Error strings.
        public const string TEXTURE_LOAD_FAILED = "Failed to load textures.";
        public const string GL_VERSION_UNSUPPORTED = "GL 1.5 not supported.";
        public const string MAP_ALREADY_LOADED = "Map already loaded.";
        public const string FILE_NOT_FOUND = "File: {0} not found.";
        public const string MAP_LOAD_ERROR = "The map may not have been created correctly.";
        public const string SENSOR_LOAD_ERROR = "The sensor configuration may not have been created correctly.";
        public const string NETWORK_LOAD_ERROR = "The neural network configuration may not have been created correctly.";
        public const string XML_READ_ERROR = "Error while reading XML.";
        public const string XML_WRITE_ERROR = "Error while writing XML.";
        public const string ENTITY_POOL_USED_UP = "No more entities of type {0}, available.";
        public const string STATE_CHANGE_ERROR = "Error changing state.";
        public const string NO_INPUT_LAYER = "No input layer specified.";
        public const string NO_NEURON_GROUPS = "No neuron groups specified.";
        public const string NO_CONNECTION_GROUPS = "No connection groups specified.";
        public const string NO_CONTROL_SCHEME = "No control scheme specified.";
        public const string NO_MODULATION_SCHEME = "No modulation scheme specified.";
        public const string NO_SENSOR_FILE = "No sensor file specified.";
        public const string NO_NETWORK_FILE = "No network file specified.";
        public const string NO_EXPERIMENT_FILE = "No experiment file specified.";
        public const string TOO_FEW_ARGS = "Too few arguments specified.";

        public static readonly string[] NEURON_GROUP_TYPES = 
        {
            "Input", "Hidden", "Output"
        };

        //Command line options.
        public static readonly Option[] OPTIONS = 
        {
            new Option(1, "--experiment"),
            new Option(0, "--headless"),
            new Option(0, "--help")
        };

        public static double DegToRad(double deg)
        {
            return deg * DEG_TO_RAD;
        }

        public static double RadToDeg(double deg)
        {
            return deg / DEG_TO_RAD;
        }

        public static double GetDist(Point2 point0, Point2 point1)
        {
            return Math.Sqrt(Math.Pow(point1.y - point0.y, 2) + Math.Pow(point1.x - point0.x, 2));
        }

        public static NeuralNetwork.NeuronGroup.Type GetGroupTypeFromString(string type)
        {
            for (int i = 0; i < NEURON_GROUP_TYPES.Length; i++)
            {
                if (type.Equals(NEURON_GROUP_TYPES[i]))
                    return (NeuralNetwork.NeuronGroup.Type)i;
            }

            return NeuralNetwork.NeuronGroup.Type.NONE;
        }

        public static NeuralNetwork.ConnectionGroup.TrainFunctionType GetMethodFromString(string method)
        {
            if (method.Equals(AUTOENCODER_TRAIN))
                return NeuralNetwork.TrainingMethod.AutoencoderTrain;
            //If hebbian or invalid, use hebbian.
            else
                return NeuralNetwork.TrainingMethod.HebbianTrain;
        }
    }
}