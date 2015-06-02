using System.Xml.Serialization;

namespace RaahnSimulation
{
    public class Experiment
    {
        [XmlElement("TicksPerRun")]
        public uint ticksPerRun;

        [XmlElement("RunCount")]
        public uint runCount;

        [XmlElement("MapFile")]
        public string mapFile;

        [XmlElement("SensorFile")]
        public string sensorFile;

        [XmlElement("NetworkFile")]
        public string networkFile;

        [XmlElement("PerformanceMeasurement")]
        public string performanceMeasurement;

        public Experiment()
        {
        }
    }
}

