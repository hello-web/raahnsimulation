using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Tao.OpenGl;

namespace RaahnSimulation
{
    [XmlRoot("Robot")]
    public class CarConfig
    {
        [XmlElement("X")]
        public double x;

        [XmlElement("Y")]
        public double y;

        [XmlElement("Angle")]
        public double angle;
    }

	public class Car : Entity
	{
        //Number of controls the network has.
        private const uint CONTROL_COUNT = 3;
        private const double CONTROL_THRESHOLD = 0.5;
		private const double CAR_SPEED_X = 960.0;
		private const double CAR_SPEED_Y = 540.0;
		//120 degrees per second.
		private const double CAR_ROTATE_SPEED = 120.0;
        private const double LEARNING_RATE = 0.2;

        public List<Entity> entitiesHovering;
        private bool configLoaded;
        private uint rangeFinderCount;
        private uint pieSliceSensorCount;
        private uint rToHMod;
        private uint pToHMod;
        private uint hToOMod;
        private QuadTree quadTree;
        private List<RangeFinderGroup> rangeFinderGroups;
        private List<PieSliceSensorGroup> pieSliceSensorGroups;
        private Raahn.NeuralNetwork brain;
        private Raahn.NeuronGroup.Identifier rangeFinderNeuronsId;
        private Raahn.NeuronGroup.Identifier pieSliceSensorNeuronsId;
        private Raahn.NeuronGroup.Identifier hiddenNeuronsId;
        private Raahn.NeuronGroup.Identifier outputNeuronsId;

	    public Car(Simulator sim, QuadTree tree) : base(sim)
	    {
	        texture = TextureManager.TextureType.CAR;

            quadTree = tree;

            rangeFinderGroups = new List<RangeFinderGroup>();
            pieSliceSensorGroups = new List<PieSliceSensorGroup>();

	        speed.x = CAR_SPEED_X;
	        speed.y = CAR_SPEED_Y;

            entitiesHovering = new List<Entity>();

            brain = new Raahn.NeuralNetwork(LEARNING_RATE);
	    }

        //Should be called only once after setting up the sensors.
        private void InitBrain()
        {
            rangeFinderNeuronsId.type = Raahn.NeuronGroup.Type.INPUT;
            pieSliceSensorNeuronsId.type = Raahn.NeuronGroup.Type.INPUT;
            hiddenNeuronsId.type = Raahn.NeuronGroup.Type.HIDDEN;
            outputNeuronsId.type = Raahn.NeuronGroup.Type.OUTPUT;

            //Add modulation signals.
            rToHMod = Raahn.ModulationSignal.AddSignal();
            pToHMod = Raahn.ModulationSignal.AddSignal();
            hToOMod = Raahn.ModulationSignal.AddSignal();

            //Just a guess at the sufficient number of hidden neurons for now.
            uint hiddenCount = (rangeFinderCount + pieSliceSensorCount) / 2;

            //Input groups.
            //Range finder input neuron group.
            rangeFinderNeuronsId.index = brain.AddNeuronGroup(rangeFinderCount, rangeFinderNeuronsId.type);
            //Pie slice sensor input neuron group.
            pieSliceSensorNeuronsId.index = brain.AddNeuronGroup(pieSliceSensorCount, pieSliceSensorNeuronsId.type);

            //Hidden neuron group.
            hiddenNeuronsId.index = brain.AddNeuronGroup(hiddenCount, hiddenNeuronsId.type);

            //Output neuron group.
            outputNeuronsId.index = brain.AddNeuronGroup(CONTROL_COUNT, outputNeuronsId.type);

            brain.ConnectGroups(rangeFinderNeuronsId, hiddenNeuronsId, Raahn.TrainingMethod.HebbianTrain, rToHMod, true);
            brain.ConnectGroups(pieSliceSensorNeuronsId, hiddenNeuronsId, Raahn.TrainingMethod.HebbianTrain, pToHMod, false);
            brain.ConnectGroups(hiddenNeuronsId, outputNeuronsId, Raahn.TrainingMethod.HebbianTrain, hToOMod, true);
        }

        public void UpdateBrain()
        {
            List<double> rInputs = new List<double>((int)rangeFinderCount);
            List<double> pInputs = new List<double>((int)pieSliceSensorCount);

            for (uint x = 0; x < rangeFinderGroups.Count; x++)
            {
                uint currentGroupLength = rangeFinderGroups[(int)x].GetRangeFinderCount();

                for (uint y = 0; y < currentGroupLength; y++)
                    rInputs.Add(rangeFinderGroups[(int)x].GetRangeFinderValue(y));
            }

            for (uint x = 0; x < pieSliceSensorGroups.Count; x++)
            {
                uint currentGroupLength = pieSliceSensorGroups[(int)x].GetPieSliceSensorCount();

                for (uint y = 0; y < currentGroupLength; y++)
                    pInputs.Add(pieSliceSensorGroups[(int)x].GetPieSliceSensorValue(y));
            }

            brain.SetInputs((uint)rangeFinderNeuronsId.index, rInputs.ToArray());
            brain.SetInputs((uint)pieSliceSensorNeuronsId.index, pInputs.ToArray());

            brain.PropagateSignal();
        }

	    public override void Update()
	    {
            //UpdateBrain();

            /*double output = brain.GetNeuronValue(outputNeuronsId, 0);
            Console.WriteLine("Ouput 0: {0:0.000000}", output);

            if (output > CONTROL_THRESHOLD)
                angle += CAR_ROTATE_SPEED * deltaTime;

            output = brain.GetNeuronValue(outputNeuronsId, 1);
            Console.WriteLine("Ouput 1: {0:0.000000}", output);

            if (output > CONTROL_THRESHOLD)
                angle -= CAR_ROTATE_SPEED * deltaTime;

            output = brain.GetNeuronValue(outputNeuronsId, 2);
            Console.WriteLine("Ouput 2: {0:0.000000}", output);

            if (output < CONTROL_THRESHOLD)
            {
                transformedWorldPos.x += velocity.x * deltaTime;
                transformedWorldPos.y += velocity.y * deltaTime;
            }*/

            double deltaTime = context.GetDeltaTime();

	        if (context.GetLeftKeyDown())
	            angle += CAR_ROTATE_SPEED * deltaTime;
	        if (context.GetRightKeyDown())
	            angle -= CAR_ROTATE_SPEED * deltaTime;

	        if (context.GetUpKeyDown())
	        {
	            transformedWorldPos.x += velocity.x * deltaTime;
	            transformedWorldPos.y += velocity.y * deltaTime;
	        }
	        if (context.GetDownKeyDown())
	        {
	            transformedWorldPos.x -= velocity.x * deltaTime;
	            transformedWorldPos.y -= velocity.y * deltaTime;
	        }

            base.Update();

            for (int i = 0; i < rangeFinderGroups.Count; i++)
                rangeFinderGroups[i].Update();

            for (int i = 0; i < pieSliceSensorGroups.Count; i++)
                pieSliceSensorGroups[i].Update();
	    }

        public override void UpdateEvent(Event e)
        {
            base.UpdateEvent(e);
        }

		public override void Draw()
	    {
	        base.Draw();

            Gl.glPushMatrix();

	        RotateAroundCenter();

	        Gl.glTranslated(drawingVec.x, drawingVec.y, Utils.DISCARD_Z_POS);
	        Gl.glScaled(width, height, Utils.DISCARD_Z_SCALE);

	        Gl.glDrawElements(mesh.GetRenderMode(), mesh.GetIndexCount(), Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);

            Gl.glPopMatrix();

            for (int i = 0; i < rangeFinderGroups.Count; i++)
                rangeFinderGroups[i].Draw();

            for (int i = 0; i < pieSliceSensorGroups.Count; i++)
                pieSliceSensorGroups[i].Draw();
	    }

        public override void DebugDraw()
        {
            base.DebugDraw();
        }

        public override void Clean()
        {
            RangeFinderGroup.Clean();
            PieSliceSensorGroup.Clean();
        }

        public bool LoadConfig(string fileName)
        {
            //If a configuration was already loaded delete the
            //VBOs and IBOs used as new ones will be allocated.
            if (configLoaded)
            {
                RangeFinderGroup.Clean();
                PieSliceSensorGroup.Clean();
                configLoaded = false;
            }

            if (!File.Exists(fileName))
            {
                Console.WriteLine(string.Format(Utils.FILE_NOT_FOUND, fileName));
                return false;
            }

            TextReader configReader = new StreamReader(fileName);
            SensorConfig sensorConfig = null;

            try
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(SensorConfig));
                sensorConfig = (SensorConfig)deserializer.Deserialize(configReader);
            }
            catch (Exception e)
            {
                Console.WriteLine(Utils.XML_READ_ERROR);
                Console.WriteLine(Utils.SENSOR_LOAD_ERROR);
                Console.WriteLine(e.Message);

                return false;
            }
            finally
            {
                configReader.Close();
            }

            if (sensorConfig.rangeFinderGroups != null)
            {
                for (int i = 0; i < sensorConfig.rangeFinderGroups.Length; i++)
                {
                    RangeFinderGroupConfig current = sensorConfig.rangeFinderGroups[i];

                    if (current == null)
                        continue;

                    rangeFinderCount += current.count;

                    RangeFinderGroup rfg = new RangeFinderGroup(context, this, quadTree, current.count);
                    rfg.Configure(current.length, current.angleOffset, current.angleBetween);

                    if (current.entitiesToDetect != null)
                    {
                        for (int n = 0; n < current.entitiesToDetect.Length; n++)
                        {
                            Entity.EntityType type = Entity.GetTypeFromString(current.entitiesToDetect[n]);

                            if (type != Entity.EntityType.NONE)
                                rfg.AddEntityToDetect(type);
                        }
                    }

                    rangeFinderGroups.Add(rfg);
                }
            }

            if (sensorConfig.pieSliceSensorGroups != null)
            {
                for (int i = 0; i < sensorConfig.pieSliceSensorGroups.Length; i++)
                {
                    PieSliceSensorGroupConfig current = sensorConfig.pieSliceSensorGroups[i];

                    if (current == null)
                        continue;

                    pieSliceSensorCount += current.count;

                    PieSliceSensorGroup pieGroup = new PieSliceSensorGroup(context, this, quadTree);
                    pieGroup.AddSensors(current.count);
                    pieGroup.ConfigureSensors(current.maxDetection, current.angleOffset, current.angleBetween, current.outerRadius, current.innerRadius);

                    if (current.entitiesToDetect != null)
                    {
                        for (int n = 0; n < current.entitiesToDetect.Length; n++)
                        {
                            Entity.EntityType type = Entity.GetTypeFromString(current.entitiesToDetect[n]);

                            if (type != Entity.EntityType.NONE)
                                pieGroup.AddEntityToDetect(type);
                        }
                    }

                    pieSliceSensorGroups.Add(pieGroup);
                }
            }

            InitBrain();

            configLoaded = true;

            return true;
        }

        public uint GetRangeFinderCount()
        {
            uint rangeFinderCount = 0;

            for (int i = 0; i < rangeFinderGroups.Count; i++)
                rangeFinderCount += rangeFinderGroups[i].GetRangeFinderCount();

            return rangeFinderCount;
        }

        public uint GetPieSliceSensorCount()
        {
            uint pieSliceSensorCount = 0;

            for (int i = 0; i < pieSliceSensorGroups.Count; i++)
                pieSliceSensorCount += pieSliceSensorGroups[i].GetPieSliceSensorCount();

            return pieSliceSensorCount;
        }

        private Utils.Point2 GetNearestIntersection(List<Utils.Point2> intersections)
        {
            Utils.Point2 nearest = intersections[0];

            for (int x = 1; x < intersections.Count; x++)
            {
                Utils.Point2 currentIntersection = intersections[x];
                Utils.Point2 centerPoint = new Utils.Point2(center.x, center.y);

                if (Utils.GetDist(nearest, centerPoint) > Utils.GetDist(currentIntersection, centerPoint))
                    nearest = intersections[x];
            }

            return nearest;
        }
	}
}
