using System;
using System.IO;
using System.Collections.Generic;

namespace RaahnSimulation
{
    public abstract class PerformanceMeasurement
    {
        public enum Method
        {
            NONE = -1,
            LAP_PERFORMANCE = 0
        }

        public const double NULL_SCORE = 0.0;

        public static readonly string[] METHOD_STRINGS = 
        {
            "LapsPerformance"
        };

        protected List<double> scores;
        protected List<List<double>> scoreHistory;

        public PerformanceMeasurement(Car agent)
        {
            scores = new List<double>();
            scoreHistory = new List<List<double>>();
        }

        public abstract void Update();

        public abstract void Reset();

        public abstract void InterpretPOIs(List<Point> pointsOfInterest);

        //Records the current score to the score history.
        public void RecordScores()
        {
            //Should never be less than. Count is a signed int.
            if (scores.Count <= 0)
                return;

            List<double> currentScores = new List<double>(scores.Count);

            for (int i = 0; i < scores.Count; i++)
                currentScores.Add(scores[i]);

            scoreHistory.Add(currentScores);
        }

        public void LogScoreHistory()
        {
            TextWriter logWriter = null;

            try
            {
                if (!Directory.Exists(Utils.LOG_FOLDER))
                    Directory.CreateDirectory(Utils.LOG_FOLDER);

                logWriter = new StreamWriter(Utils.LOG_FOLDER + Utils.LOG_SCORE_FILE);
            }
            catch (Exception e)
            {
                logWriter.Close();
                Console.WriteLine(e.Message);

                return;
            }

            for (int x = 0; x < scoreHistory.Count; x++)
            {
                //Score history lists should not be empty.
                for (int y = 0; y < scoreHistory[x].Count - 1; y++)
                    logWriter.Write(Utils.LOG_SCORE_FORMAT, scoreHistory[x][y]);

                //Instead of the regular format for the last, end with an end line char.
                logWriter.WriteLine(scoreHistory[x][scoreHistory[x].Count - 1]);
            }

            logWriter.Close();
        }

        //Returns the first score.
        public double GetScore()
        {
            if (scores.Count > 0)
                return scores[0];
            else
                return NULL_SCORE;
        }

        public double GetScore(uint index)
        {
            if (index < scores.Count)
                return scores[(int)index];
            else
                return NULL_SCORE;
        }

        public List<double> GetScores()
        {
            return scores;
        }

        public static Method GetMethodFromString(string str)
        {
            for (int i = 0; i < METHOD_STRINGS.Length; i++)
            {
                if (str.Equals(METHOD_STRINGS[i]))
                    return (Method)i;
            }

            return Method.NONE;
        }

        public static PerformanceMeasurement CreateFromMethod(Method method, Car agent)
        {
            switch (method)
            {
                case Method.LAP_PERFORMANCE:
                    return new LapsMeasurement(agent);
                default:
                    return null;
            }
        }
    }
}
