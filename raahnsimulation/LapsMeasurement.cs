using System;
using System.Collections.Generic;

namespace RaahnSimulation
{
    public class LapsMeasurement : PerformanceMeasurement
    {
        //When the angle loops back from 180 to negative values.
        //Somewhat abitrary, the agent should not jump more than 
        //90 degrees around the center point.
        private const double ANGLE_RESET_CHANGE = 90.0;
        private const double ANGLE_FULL_CIRCLE = 360.0;
        private const double DEFAULT_SCORE = 0.0;

        private double lastAngle;
        private double angleTotal;
        private Utils.Point2 centerPoint;
        private Car raahnCar;

        public LapsMeasurement(Car agent) : base(agent)
        {
            raahnCar = agent;

            centerPoint = new Utils.Point2(0.0, 0.0);

            lastAngle = 0.0;
            angleTotal = 0.0;

            scores.Add(DEFAULT_SCORE);
        }

        public override void Update()
        {
            double xDifference = centerPoint.x - raahnCar.GetTransformedX();
            double yDifference = centerPoint.y - raahnCar.GetTransformedY();

            double newAngle = Utils.RadToDeg(Math.Atan2(yDifference, xDifference));
            double deltaAngle = newAngle - lastAngle;
            double absDeltaAngle = Math.Abs(deltaAngle);

            //Check if the angle looped back.
            if (absDeltaAngle > ANGLE_RESET_CHANGE)
            {
                double change = ANGLE_FULL_CIRCLE - absDeltaAngle;

                //Angle increase
                if (deltaAngle < 0.0)
                    angleTotal += change;
                //Angle decrease.
                else
                    angleTotal -= change;
            }
            else
                angleTotal += deltaAngle;

            scores[0] = angleTotal / ANGLE_FULL_CIRCLE;

            lastAngle = newAngle;
        }

        public override void Reset()
        {
            double xDifference = centerPoint.x - raahnCar.GetTransformedX();
            double yDifference = centerPoint.y - raahnCar.GetTransformedY();

            lastAngle = Utils.RadToDeg(Math.Atan2(yDifference, xDifference));

            angleTotal = 0.0;
            scores[0] = 0.0;
        }

        public override void InterpretPOIs(List<Point> pointsOfInterest)
        {
            if (pointsOfInterest.Count > 0)
            {
                centerPoint.x = pointsOfInterest[0].GetTransformedX();
                centerPoint.y = pointsOfInterest[0].GetTransformedY();

                double xDifference = centerPoint.x - raahnCar.GetTransformedX();
                double yDifference = centerPoint.y - raahnCar.GetTransformedY();

                lastAngle = Utils.RadToDeg(Math.Atan2(yDifference, xDifference));
            }
        }
    }
}
