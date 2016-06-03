using System;
using System.Collections.Generic;

namespace RaahnSimulation
{
    public partial class Car
    {
        public class ControlScheme
        {
            public enum Scheme
            {
                NONE = -1,
                SENSOR_CONTROL = 0,
                RANGE_FINDER_CONTROL = 1
            }

            private const uint DEFAULT_SCHEME_INDEX = 0;

            public delegate bool SchemeFunction(Car car);

            public static readonly SchemeFunction[] SCHEMES = 
            {
                SensorControl, RangeFinderControl
            };

            public static readonly string[] SCHEME_STRINGS = 
            {
                "SensorControl", "RangeFinderControl"
            };

            public static void InterpretParameters(string[] parameters, Scheme scheme)
            {
                //Interpret parameters specific to each scheme, if any.
                switch (scheme)
                {
                    case Scheme.SENSOR_CONTROL:
                    {
                        break;
                    }
                    case Scheme.RANGE_FINDER_CONTROL:
                    {
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

            //Uses both range finders and pie slice sensors.
            public static bool SensorControl(Car car)
            {
                bool userControl = false;

                //Set the inputs.
                List<double> inputs = new List<double>((int)(car.rangeFinderCount + car.pieSliceSensorCount));

                for (uint x = 0; x < car.rangeFinderGroups.Count; x++)
                {
                    uint currentGroupLength = car.rangeFinderGroups[(int)x].GetRangeFinderCount();

                    for (uint y = 0; y < currentGroupLength; y++)
                        inputs.Add(car.rangeFinderGroups[(int)x].GetRangeFinderValue(y));
                }

                for (uint x = 0; x < car.pieSliceSensorGroups.Count; x++)
                {
                    uint currentGroupLength = car.pieSliceSensorGroups[(int)x].GetPieSliceSensorCount();

                    for (uint y = 0; y < currentGroupLength; y++)
                        inputs.Add(car.pieSliceSensorGroups[(int)x].GetPieSliceSensorValue(y));
                }

                car.brain.AddExperience(inputs);

                car.brain.PropagateSignal();

                //If the left or right arrow key is down, use user control.
                if (car.context.GetLeftKeyDown())
                {
                    car.brain.SetOutput(0, 0, Car.MAX_ROTATE);
                    userControl = true;
                }
                else if (car.context.GetRightKeyDown())
                {
                    car.brain.SetOutput(0, 0, Car.MIN_ROTATE);
                    userControl = true;
                }

                double output = car.brain.GetOutputValue(0, 0);

                car.angle += (output * ROTATE_RANGE) - ROTATE_SPEED;

                return userControl;
            }

            public static bool RangeFinderControl(Car car)
            {
                bool userControl = false;

                //Set the inputs.
                List<double> inputs = new List<double>((int)car.rangeFinderCount);

                for (uint x = 0; x < car.rangeFinderGroups.Count; x++)
                {
                    uint currentGroupLength = car.rangeFinderGroups[(int)x].GetRangeFinderCount();

                    for (uint y = 0; y < currentGroupLength; y++)
                        inputs.Add(car.rangeFinderGroups[(int)x].GetRangeFinderValue(y));
                }

                car.brain.AddExperience(inputs);

                car.brain.PropagateSignal();

                //If the left or right arrow key is down, use user control.
                if (car.context.GetLeftKeyDown())
                {
                    car.brain.SetOutput(0, 0, Car.MAX_ROTATE);
                    userControl = true;
                }
                else if (car.context.GetRightKeyDown())
                {
                    car.brain.SetOutput(0, 0, Car.MIN_ROTATE);
                    userControl = true;
                }

                double output = car.brain.GetOutputValue(0, 0);

                car.angle += (output * ROTATE_RANGE) - ROTATE_SPEED;

                return userControl;
            }
        }
    }
}