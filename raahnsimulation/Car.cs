using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using OpenTK.Graphics.OpenGL;
using Raahn;

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

    public partial class Car : Entity
    {
        public const double HALF_QUERY_WIDTH = Simulator.WORLD_WINDOW_WIDTH / 2.0;
        public const double HALF_QUERY_HEIGHT = Simulator.WORLD_WINDOW_HEIGHT / 2.0;
        private const double CONTROL_THRESHOLD = 0.5;
        private const double ROTATE_SPEED = 2.0;
        private const double SPEED_X = 15.0;
        private const double SPEED_Y = 12.0;
        //Between 0 and twice ROTATE_SPEED
        private const double ROTATE_RANGE = 2.0 * ROTATE_SPEED;

        public List<Entity> entitiesHovering;
        private bool configLoaded;
        private uint rangeFinderCount;
        private uint pieSliceSensorCount;
        private QuadTree quadTree;
        private ModulationScheme.SchemeFunction modulationScheme;
        private ControlScheme.SchemeFunction controlScheme;
        private List<uint> modulationSignals;
        private List<RangeFinderGroup> rangeFinderGroups;
        private List<PieSliceSensorGroup> pieSliceSensorGroups;
        private NeuralNetwork brain;

        public Car(Simulator sim, QuadTree tree) : base(sim)
        {
            texture = TextureManager.TextureType.CAR;
            type = EntityType.CAR;

            quadTree = tree;

            modulationSignals = new List<uint>();
            rangeFinderGroups = new List<RangeFinderGroup>();
            pieSliceSensorGroups = new List<PieSliceSensorGroup>();

            speed.x = SPEED_X;
            speed.y = SPEED_Y;

            entitiesHovering = new List<Entity>();
        }

        public override void Update()
        {
            if (controlScheme != null)
                controlScheme(this);

            double worldX = GetWorldX();
            double worldY = GetWorldY();

            Utils.Vector2 lowerLeft = camera.TransformWorld(worldX - HALF_QUERY_WIDTH, worldY - HALF_QUERY_HEIGHT);
            Utils.Vector2 upperRight = camera.TransformWorld(worldX + HALF_QUERY_WIDTH, worldY + HALF_QUERY_HEIGHT);

            AABB viewBounds = new AABB(upperRight.x - lowerLeft.x, upperRight.y - lowerLeft.y);
            viewBounds.Translate(lowerLeft.x, lowerLeft.y);

            Utils.LineSegment collisionLine = new Utils.LineSegment();

            Utils.Point2 original = new Utils.Point2(center.x, center.y);
            Utils.Point2 projected = new Utils.Point2(center.x + velocity.x, center.y + velocity.y);

            collisionLine.SetUp(original, projected);

            List<Entity> entitiesInBounds = quadTree.Query(viewBounds);
            bool canMove = true;

            for (int i = 0; i < entitiesInBounds.Count; i++)
            {
                if (entitiesInBounds[i].GetEntityType() == EntityType.WALL)
                {
                    Utils.LineSegment compare = ((Wall)entitiesInBounds[i]).GetLineSegment();
                    List<Utils.Point2> intersections = collisionLine.Intersects(compare);

                    //If there is a collision, don't move.
                    if (intersections.Count > 0)
                    {
                        canMove = false;
                        break;
                    }
                }
            }

            if (canMove)
            {
                drawingVec.x += velocity.x;
                drawingVec.y += velocity.y;
            }

            if (modulationScheme != null)
                modulationScheme(this, entitiesInBounds);

            brain.Train();

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

            GL.PushMatrix();

            RotateAroundCenter();

            GL.Translate(drawingVec.x, drawingVec.y, Utils.DISCARD_Z_POS);
            GL.Scale(width, height, Utils.DISCARD_Z_SCALE);

            GL.DrawElements(mesh.GetRenderMode(), mesh.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);

            GL.PopMatrix();

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

        public bool LoadConfig(string sensorFile, string networkFile)
        {
            //Even if the XML is invalid, the brain must be initiaized.
            brain = new NeuralNetwork();

            //If a configuration was already loaded delete the
            //VBOs and IBOs used as new ones will be allocated.
            if (configLoaded)
            {
                RangeFinderGroup.Clean();
                PieSliceSensorGroup.Clean();
                configLoaded = false;
            }

            if (!string.IsNullOrEmpty(sensorFile))
            {
                if (!InitSensors(sensorFile))
                    return false;
            }

            if (!string.IsNullOrEmpty(networkFile))
            {
                if (!InitBrain(networkFile))
                    return false;
            }

            configLoaded = true;

            return true;
        }

        public uint GetRangeFinderCount()
        {
            return rangeFinderCount;
        }

        public uint GetPieSliceSensorCount()
        {
            return pieSliceSensorCount;
        }

        private bool InitSensors(string sensorFile)
        {
            if (!File.Exists(sensorFile))
            {
                Console.WriteLine(string.Format(Utils.FILE_NOT_FOUND, sensorFile));
                return false;
            }

            TextReader configReader = new StreamReader(sensorFile);
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

            return true;
        }

        private bool InitBrain(string networkFile)
        {
            if (!File.Exists(networkFile))
            {
                Console.WriteLine(Utils.FILE_NOT_FOUND, networkFile);
                return false;
            }

            TextReader configReader = new StreamReader(networkFile);
            NeuralNetworkConfig networkConfig = null;

            try
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(NeuralNetworkConfig));
                networkConfig = (NeuralNetworkConfig)deserializer.Deserialize(configReader);
            }
            catch (Exception e)
            {
                Console.WriteLine(Utils.XML_READ_ERROR);
                Console.WriteLine(Utils.NETWORK_LOAD_ERROR);
                Console.WriteLine(e.Message);

                return false;
            }
            finally
            {
                configReader.Close();
            }

            //No neuron groups, connection groups, control scheme, or modulation scheme
            //return true to continue without 
            if (networkConfig.neuronGroups == null)
            {
                Console.WriteLine(Utils.NO_NEURON_GROUPS);
                return true;
            }

            if (networkConfig.connectionGroups == null)
            {
                Console.WriteLine(Utils.NO_CONNECTION_GROUPS);
                return true;
            }

            if (networkConfig.controlScheme == null)
            {
                Console.WriteLine(Utils.NO_CONTROL_SCHEME);
                return true;
            }

            if (networkConfig.modulationScheme == null)
            {
                Console.WriteLine(Utils.NO_MODULATION_SCHEME);
                return true;
            }

            brain.learningRate = networkConfig.learningRate;

            ControlScheme.Scheme cSchemeDescriptor = ControlScheme.GetSchemeFromString(networkConfig.controlScheme);
            ModulationScheme.Scheme mSchemeDescriptor = ModulationScheme.GetSchemeFromString(networkConfig.modulationScheme);

            if (cSchemeDescriptor == ControlScheme.Scheme.NONE)
            {
                Console.WriteLine(Utils.NO_CONTROL_SCHEME);
                return true;
            }

            if (mSchemeDescriptor == ModulationScheme.Scheme.NONE)
            {
                Console.WriteLine(Utils.NO_MODULATION_SCHEME);
                return true;
            }

            controlScheme = ControlScheme.GetSchemeFunction(cSchemeDescriptor);
            modulationScheme = ModulationScheme.GetSchemeFunction(mSchemeDescriptor);

            if (networkConfig.parameters != null)
            {
                ControlScheme.InterpretParameters(networkConfig.parameters, cSchemeDescriptor);
                ModulationScheme.InterpretParameters(networkConfig.parameters, mSchemeDescriptor);
            }

            int[] neuronGroupIds = new int[networkConfig.neuronGroups.Length];

            //Add modulation signals.
            for (uint i = 0; i < networkConfig.neuronGroups.Length; i++)
            {
                NeuronGroupConfig nGroupConfig = networkConfig.neuronGroups[(int)i];

                NeuronGroup.Type type = Utils.GetGroupTypeFromString(nGroupConfig.type);

                neuronGroupIds[(int)i] = brain.AddNeuronGroup(nGroupConfig.count, type);
            }

            for (uint i = 0; i < networkConfig.connectionGroups.Length; i++)
            {
                ConnectionConfig cGroupConfig = networkConfig.connectionGroups[(int)i];

                if (cGroupConfig.inputGroupIndex < networkConfig.neuronGroups.Length
                    && cGroupConfig.outputGroupIndex < networkConfig.neuronGroups.Length)
                {
                    NeuronGroup.Identifier inputGroup;
                    inputGroup.index = neuronGroupIds[(int)cGroupConfig.inputGroupIndex];
                    string inputTypeString = networkConfig.neuronGroups[(int)cGroupConfig.inputGroupIndex].type;
                    inputGroup.type = Utils.GetGroupTypeFromString(inputTypeString);

                    NeuronGroup.Identifier outputGroup;
                    outputGroup.index = neuronGroupIds[(int)cGroupConfig.outputGroupIndex];
                    string outputTypeString = networkConfig.neuronGroups[(int)cGroupConfig.outputGroupIndex].type;
                    outputGroup.type = Utils.GetGroupTypeFromString(outputTypeString);

                    ConnectionGroup.TrainFunctionType trainMethod = Utils.GetMethodFromString(cGroupConfig.trainingMethod);

                    if (cGroupConfig.useModulation)
                    {
                        uint modSig = 0;

                        modSig = ModulationSignal.AddSignal();
                        modulationSignals.Add(modSig);

                        brain.ConnectGroups(inputGroup, outputGroup, trainMethod, (int)modSig, cGroupConfig.usebias);
                    }
                    else
                        brain.ConnectGroups(inputGroup, outputGroup, trainMethod, ModulationSignal.INVALID_INDEX, cGroupConfig.usebias);
                }
            }

            return true;
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
