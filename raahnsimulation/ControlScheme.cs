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
                SENSOR_BASED_TURN = 0
            }

            private const uint DEFAULT_SCHEME_INDEX = 0;
            private const uint RANGE_FINDER_GROUP_INDEX = 0;
            private const uint PIE_SLICE_SENSOR_GROUP_INDEX = 0;

            public delegate void SchemeFunction(Car car);

            public static readonly SchemeFunction[] SCHEMES = 
            {
                SensorBasedTurn
            };

            public static readonly string[] SCHEME_STRINGS = 
            {
                "SensorBasedTurn"
            };

            public static void InterpretParameters(string[] parameters, Scheme scheme)
            {
                //Interpret parameters specific to each scheme, if any.
                switch (scheme)
                {
                    case Scheme.SENSOR_BASED_TURN:
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

            public static void SensorBasedTurn(Car car)
            {
                //Set the inputs.
                List<double> rInputs = new List<double>((int)car.rangeFinderCount);
                List<double> pInputs = new List<double>((int)car.pieSliceSensorCount);

                for (uint x = 0; x < car.rangeFinderGroups.Count; x++)
                {
                    uint currentGroupLength = car.rangeFinderGroups[(int)x].GetRangeFinderCount();

                    for (uint y = 0; y < currentGroupLength; y++)
                        rInputs.Add(car.rangeFinderGroups[(int)x].GetRangeFinderValue(y));
                }

                for (uint x = 0; x < car.pieSliceSensorGroups.Count; x++)
                {
                    uint currentGroupLength = car.pieSliceSensorGroups[(int)x].GetPieSliceSensorCount();

                    for (uint y = 0; y < currentGroupLength; y++)
                        pInputs.Add(car.pieSliceSensorGroups[(int)x].GetPieSliceSensorValue(y));
                }

                car.brain.SetInputs(RANGE_FINDER_GROUP_INDEX, rInputs.ToArray());
                car.brain.SetInputs(PIE_SLICE_SENSOR_GROUP_INDEX, pInputs.ToArray());

                car.brain.PropagateSignal();

                double output = car.brain.GetOutputValue(0, 0);
                Console.WriteLine(Utils.OUTPUT_VERBOSE, output);

                //If the left or right arrow key is down, use user control.
                if (car.context.GetLeftKeyDown())
                    car.angle += ROTATE_SPEED;
                else if (car.context.GetRightKeyDown())
                    car.angle -= ROTATE_SPEED;
                else
                    car.angle += (output * ROTATE_RANGE) - ROTATE_SPEED;
            }
        }
    }
}