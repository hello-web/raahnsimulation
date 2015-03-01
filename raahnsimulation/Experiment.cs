using System.Xml.Serialization;

namespace RaahnSimulation
{
    public class Experiment
    {
        [XmlElement("MapFile")]
        public string mapFile;

        [XmlElement("SensorFile")]
        public string sensorFile;

        public Experiment()
        {
        }
    }
}

