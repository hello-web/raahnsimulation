using System.Xml.Serialization;

namespace RaahnSimulation
{
    public class SensorConfig
    {
        [XmlElement("RangeFinderGroup")]
        public RangeFinderGroupConfig[] rangeFinderGroups;

        [XmlElement("PieSliceSensorGroup")]
        public PieSliceSensorGroupConfig[] pieSliceSensorGroups;
    }
}

