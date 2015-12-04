using System;
using System.Collections.Generic;
using Raahn;

namespace RaahnSimulation
{
    public partial class Car
    {
        public class ModulationScheme
        {
            public enum Scheme
            {
                NONE = -1,
                WALL_AVOIDANCE = 0
            }

            private const uint DEFAULT_SCHEME_INDEX = 0;
            private const uint WALL_AVOIDANCE_PARAMETER_COUNT = 1;
            private const double MODULATION_STENGTH = 1.0;
            private const double MODULATION_RESET = 0.0;
            private const double MODULATION_NOT_RESET = -1.0;
            private const double PERPENDICULAR = 90.0;

            public delegate void SchemeFunction(Car car, List<Entity> entitiesInBounds, List<uint> modSigs);

            public static readonly SchemeFunction[] SCHEMES = 
            {
                WallAvoidance
            };

            public static readonly string[] SCHEME_STRINGS = 
            {
                "WallAvoidance"
            };

            private static double lastAngleBetween = MODULATION_RESET;
            private static double viewDistance = 0.0;
            private static Wall lastWallInRange = null;

            public static void InterpretParameters(string[] parameters, Scheme scheme)
            {
                //Interpret parameters specific to each scheme, if any.
                switch (scheme)
                {
                    case Scheme.WALL_AVOIDANCE:
                    {
                        if (parameters.Length >= WALL_AVOIDANCE_PARAMETER_COUNT)
                            viewDistance = double.Parse(parameters[0]);

                        break;
                    }
                }
            }

            //Resets global variables used by schemes.
            public static void Reset()
            {
                lastAngleBetween = MODULATION_RESET;
                lastWallInRange = null;
            }

            public static Scheme GetSchemeFromString(string schemeString)
            {
                for (int i = 0; i < SCHEME_STRINGS.Length; i++)
                {
                    if (schemeString.Equals(SCHEME_STRINGS[i]))
                        return (Scheme)i;
                }

                return Scheme.NONE;
            }

            public static SchemeFunction GetSchemeFunction(Scheme scheme)
            {
                int schemei = (int)scheme;

                if (schemei >= 0 && schemei < SCHEMES.Length)
                    return SCHEMES[schemei];
                else
                    return null;
            }

            public static void WallAvoidance(Car car, List<Entity> entitiesInBounds, List<uint> modSigs)
            {
                double xViewDist = car.center.x + Math.Cos(Utils.DegToRad(car.angle)) * viewDistance;
                double yViewDist = car.center.y + Math.Sin(Utils.DegToRad(car.angle)) * viewDistance;

                Utils.Point2 original = new Utils.Point2(car.center.x, car.center.y);
                Utils.Point2 viewEndPoint = new Utils.Point2(xViewDist, yViewDist);

                Utils.LineSegment viewLine = new Utils.LineSegment();
                viewLine.SetUp(original, viewEndPoint);

                Wall compareWall = null;
                double nearestDist = Utils.GetDist(original, viewEndPoint);

                //Get the nearest wall in the view distance if any.
                for (int i = 0; i < entitiesInBounds.Count; i++)
                {
                    if (entitiesInBounds[i].GetEntityType() == Entity.EntityType.WALL)
                    {
                        Wall currentWall = (Wall)entitiesInBounds[i];

                        Utils.LineSegment compare = currentWall.GetLineSegment();
                        List<Utils.Point2> intersections = viewLine.Intersects(compare);

                        if (intersections.Count > 0)
                        {
                            //The first index is always the nearest.
                            double dist = Utils.GetDist(original, intersections[0]);

                            if (dist < nearestDist)
                            {
                                nearestDist = dist;
                                compareWall = currentWall;
                            }
                        }
                    }
                }

                //The angle to use for modulation. Should never be zero when the angle delta is calculated.
                //If it is there must be a bug.
                double angleBetween = 0.0;
                double newLastAngle = MODULATION_NOT_RESET;

                Utils.Vector2 viewVector = new Utils.Vector2(viewEndPoint.x - car.center.x, viewEndPoint.y - car.center.y);

                //If there is no nearest wall.
                if (compareWall == null)
                {
                    //If there is no previous wall, set the modulation to zero and reset the last angle.
                    if (lastWallInRange == null)
                    {
                        if (lastAngleBetween != MODULATION_RESET)
                        {
                            lastAngleBetween = MODULATION_RESET;

                            for (int i = 0; i < modSigs.Count; i++)
                                ModulationSignal.SetSignal(modSigs[i], ModulationSignal.NO_MODULATION);
                        }

                        //Nothing to modulate and nothing to save. Don't continue.
                        return;
                    }
                    //Just left a wall.
                    else
                    {
                        Utils.Vector2 lastWallVector = new Utils.Vector2(lastWallInRange.GetRelativeX(), lastWallInRange.GetRelativeY());

                        angleBetween = viewVector.AngleBetween(lastWallVector);
                    }
                }
                //If the wall changed.
                else if (compareWall != lastWallInRange)
                {
                    //There was a last wall that is different from the current wall.
                    if (lastWallInRange != null)
                    {
                        Utils.Vector2 compareWallVector = new Utils.Vector2(compareWall.GetRelativeX(), compareWall.GetRelativeY());
                        Utils.Vector2 lastWallVector = new Utils.Vector2(lastWallInRange.GetRelativeX(), lastWallInRange.GetRelativeY());

                        angleBetween = viewVector.AngleBetween(lastWallVector);

                        newLastAngle = viewVector.AngleBetween(compareWallVector);
                    }
                    //It is the first time any wall was hit, don't continue.
                    //Save the angle between and current wall.
                    else
                    {
                        Utils.Vector2 compareWallVector = new Utils.Vector2(compareWall.GetRelativeX(), compareWall.GetRelativeY());

                        lastAngleBetween = angleBetween = viewVector.AngleBetween(compareWallVector);
                        lastWallInRange = compareWall;
                        return;
                    }
                }
                //The usual case, the last wall is equal to the current wall.
                else
                {
                    Utils.Vector2 compareWallVector = new Utils.Vector2(compareWall.GetRelativeX(), compareWall.GetRelativeY());
                    newLastAngle = angleBetween = viewVector.AngleBetween(compareWallVector);
                }

                double delta = angleBetween - lastAngleBetween;
                double modulation = MODULATION_STENGTH;

                if (angleBetween > PERPENDICULAR)
                    modulation *= delta / Car.ROTATE_SPEED;
                else
                    modulation *= -delta / Car.ROTATE_SPEED;

                for (int i = 0; i < modSigs.Count; i++)
                    ModulationSignal.SetSignal(modSigs[i], modulation);

                lastAngleBetween = newLastAngle;
                lastWallInRange = compareWall;
            }
        }
    }
}