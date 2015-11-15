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
            private const double RESET_MODULATION = 0.0;
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

            private static double lastAngleBetween = RESET_MODULATION;
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

                Wall nearestWall = null;
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
                                nearestWall = currentWall;
                            }
                        }
                    }
                }

                if (nearestWall == null)
                {
                    lastAngleBetween = RESET_MODULATION;
                    lastWallInRange = null;

                    for (int i = 0; i < modSigs.Count; i++)
                        ModulationSignal.SetSignal(modSigs[i], 0.0);

                    return;
                }

                Utils.Vector2 viewVector = new Utils.Vector2(viewEndPoint.x - car.center.x, viewEndPoint.y - car.center.y);
                Utils.Vector2 lineVector = new Utils.Vector2(nearestWall.GetRelativeX(), nearestWall.GetRelativeY());

                double dotProduct = viewVector.DotProduct(lineVector);
                double magnitudeProduct = viewVector.GetMagnitude() * lineVector.GetMagnitude();

                double angleBetween = Utils.RadToDeg(Math.Acos(dotProduct / magnitudeProduct));

                //If this is the first time the angle between this wall was calculated, don't modulate.
                //The delta between the angleBetween and lastAngleBetween won't be accurate.
                if (lastAngleBetween == RESET_MODULATION || lastWallInRange != nearestWall)
                {
                    lastAngleBetween = angleBetween;
                    lastWallInRange = nearestWall;
                    return;
                }

                double delta = angleBetween - lastAngleBetween;
                double modulation = MODULATION_STENGTH;

                if (angleBetween > PERPENDICULAR)
                    modulation *= delta / Car.ROTATE_SPEED;
                else
                    modulation *= -delta / Car.ROTATE_SPEED;

                for (int i = 0; i < modSigs.Count; i++)
                    ModulationSignal.SetSignal(modSigs[i], modulation);

                lastAngleBetween = angleBetween;
            }
        }
    }
}