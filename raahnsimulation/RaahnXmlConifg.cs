using System.Xml.Serialization;

namespace RaahnSimulation
{
    [XmlRoot("NeuralNetwork")]
    public class NeuralNetworkConfig
    {
        [XmlElement("LearningRate")]
        public double learningRate;

        [XmlElement("ControlScheme")]
        public string controlScheme;

        [XmlElement("ModulationScheme")]
        public string modulationScheme;

        [XmlElement("Parameter")]
        public string[] parameters;

        [XmlElement("NeuronGroup")]
        public NeuronGroupConfig[] neuronGroups;

        [XmlElement("ConnectionGroup")]
        public ConnectionConfig[] connectionGroups;
    }

    [XmlRoot("NeuronGroup")]
    public class NeuronGroupConfig
    {
        [XmlElement("Count")]
        public uint count;

        [XmlElement("Type")]
        public string type;
    }

    [XmlRoot("ConnectionGroup")]
    public class ConnectionConfig
    {
        [XmlElement("InputGroup")]
        public uint inputGroupIndex;

        [XmlElement("OutputGroup")]
        public uint outputGroupIndex;

        [XmlAttribute("UseModulation")]
        public bool useModulation;

        [XmlAttribute("UseBias")]
        public bool usebias;

        [XmlElement("TrainingMethod")]
        public string trainingMethod;

        public ConnectionConfig()
        {
            useModulation = false;
            usebias = false;
        }
    }
}
