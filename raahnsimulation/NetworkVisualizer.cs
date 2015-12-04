using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Raahn;

namespace RaahnSimulation
{
    public class NetworkVisualizer
    {
        private class NeuronGroupDescription
        {
            public int id;
            public uint neuronCount;
            public double y;
            public NeuralNetwork.NeuronGroup.Type type;
            //Neuron values.
            public List<double> values;
            public List<double> xValues;
            public List<ConnectionDescription> connectionsGroups;

            public NeuronGroupDescription()
            {
                values = new List<double>();
                xValues = new List<double>();
                connectionsGroups = new List<ConnectionDescription>();
            }
        }

        private class ConnectionDescription
        {
            public NeuronGroupDescription toGroup;
            public List<double> weights;
            public List<NeuronGroupDescription> toLayer;

            public ConnectionDescription()
            {
                weights = new List<double>();
            }
        }

        private delegate void DrawFunction();

        private const int INPUT_INDEX = (int)NeuralNetwork.NeuronGroup.Type.INPUT;
        private const int MODULATION_ITEM_MIN = 0;
        private const int MODULATION_ITEM_STEP = 1;
        private const int MODULATION_FRAME_PADDING = 5;
        private const int MODULATION_PADDING = 10;
        private const float MAX_NEURON_SIZE = 25.0f;
        private const float MAX_CONNECTION_WIDTH = 10.0f;
        private const double MIN_CONNECTION_STRENGTH = 0.00001;
        private const double MAX_INPUT_NEURON_VALUE = 1.0;
        private const double MIN_NEURON_VALUE = 0.00001;
        private const double COLOR_THRESHOLD = 0.0;
        private const string DEFAULT_ERROR = "0.0";

        //The widget itself stores its width and height as ints.
        private int glWidgetWidth;
        private int glWidgetHeight;
        private uint visualizerWidth;
        private uint visualizerHeight;
        private uint modSigCount;
        private double weightCap;
        private double maxNeuronValue;
        private double activationUpperBound;
        private double activationLowerBound;
        private double xSpacing;
        private double ySpacing;
        private float[] lineVertices = { 0.0f, 0.0f, 0.0f, 0.0f };
        private Gtk.Window visualizerWindow;
        private GLWidget visualizerGLWidget;
        private Gtk.Label modulationDesc;
        private Gtk.Label modulationDisplay;
        private Gtk.Label errorDesc;
        private Gtk.Label errorDisplay;
        private Gtk.SpinButton modulationItem;
        private NeuralNetwork network;
        private Mesh point;
        private Mesh line;
        private List<List<NeuronGroupDescription>> neuronLayers;
        private DrawFunction connectionDrawFunction;

        public NetworkVisualizer()
        {
            glWidgetWidth = 0;
            glWidgetHeight = 0;

            visualizerWidth = 0;
            visualizerHeight = 0;

            //Default to sigmoid bounds.
            activationUpperBound = 1.0;
            activationLowerBound = 0.0;

            modSigCount = 0;

            visualizerWindow = null;
            network = null;

            point = new Mesh(2, BeginMode.Points);

            float[] pointVertex = { 0.0f, 0.0f };
            ushort[] pointIndex = { 0 };

            point.SetVertices(pointVertex, false);
            point.SetIndices(pointIndex);

            line = new Mesh(2, BeginMode.Lines);

            ushort[] lineIndices = { 0, 1 };

            line.SetVertices(lineVertices, false);
            line.SetIndices(lineIndices);

            neuronLayers = new List<List<NeuronGroupDescription>>();

            connectionDrawFunction = DrawConnectionsUncapped;
        }

        public void Display()
        {
            if (visualizerWindow != null)
                return;

            visualizerWindow = new Gtk.Window(Gtk.WindowType.Toplevel);
            visualizerWindow.Title = Utils.WINDOW_VISUALIZER_TITLE;

            visualizerWidth = (uint)((double)visualizerWindow.Screen.Width * Utils.VISUALIZER_SCREEN_WIDTH_PERCENTAGE);
            visualizerHeight = (uint)((double)visualizerWindow.Screen.Height * Utils.VISUALIZER_SCREEN_HEIGHT_PERCENTAGE);

            visualizerWindow.Resize((int)visualizerWidth, (int)visualizerHeight);

            //Force a minimum width and height.
            Gdk.Geometry minDim = new Gdk.Geometry();
            minDim.MinWidth = (int)Simulator.MIN_WINDOW_WIDTH;
            minDim.MinHeight = (int)Simulator.MIN_WINDOW_HEIGHT;

            visualizerWindow.SetGeometryHints(visualizerWindow, minDim, Gdk.WindowHints.MinSize);

            visualizerWindow.ConfigureEvent += OnConfigure;
            visualizerWindow.DeleteEvent += Clean;

            Gtk.VBox mainContainer = new Gtk.VBox();

            visualizerGLWidget = new GLWidget(GraphicsMode.Default, InitGraphics, Draw);

            Gtk.Frame debugInfoFrame = new Gtk.Frame(Utils.DEBUG_FRAME);

            Gtk.VBox debugVBox = new Gtk.VBox();

            Gtk.HBox modulationHBox = new Gtk.HBox();

            //Get the max value for modulationItem.
            modSigCount = ModulationSignal.GetSignalCount();
            uint modItemMax = 0;

            //If there is at least one modulation signal, get the last index.
            if (modSigCount > 0)
                modItemMax = modSigCount - 1;

            modulationDesc = new Gtk.Label(Utils.MODULATION_DESCRIPTION);
            modulationItem = new Gtk.SpinButton(MODULATION_ITEM_MIN, modItemMax, MODULATION_ITEM_STEP);
            modulationDisplay = new Gtk.Label();

            modulationHBox.PackStart(modulationItem, false, false, Utils.NO_PADDING);
            modulationHBox.PackStart(modulationDesc, false, false, Utils.NO_PADDING);
            modulationHBox.PackStart(modulationDisplay, false, false, Utils.NO_PADDING);

            debugVBox.PackStart(modulationHBox, false, false, Utils.NO_PADDING);

            Gtk.HBox errorHBox = new Gtk.HBox();

            errorDesc = new Gtk.Label(Utils.ERROR_DESCRIPTION);
            errorDisplay = new Gtk.Label(DEFAULT_ERROR);

            errorHBox.PackStart(errorDesc, false, false, Utils.NO_PADDING);
            errorHBox.PackStart(errorDisplay, false, false, Utils.NO_PADDING);

            debugVBox.PackStart(errorHBox, false, false, Utils.NO_PADDING);

            debugInfoFrame.Add(debugVBox);

            mainContainer.PackStart(visualizerGLWidget, true, true, Utils.NO_PADDING);
            mainContainer.PackStart(debugInfoFrame, false, false, MODULATION_FRAME_PADDING);

            visualizerWindow.Add(mainContainer);

            visualizerWindow.ShowAll();
        }

        public void Update()
        {
            if (visualizerGLWidget != null)
            {
                if (visualizerGLWidget.Visible)
                {
                    UpdateUi();
                    UpdateVisualization();
                }
            }
        }

        public void Render()
        {
            if (visualizerGLWidget != null)
            {
                if (visualizerGLWidget.Visible)
                    visualizerGLWidget.RenderFrame();
            }
        }

        public void Close()
        {
            visualizerWindow.ProcessEvent(Gdk.EventHelper.New(Gdk.EventType.Delete));
        }

        public bool IsOpen()
        {
            return visualizerWindow != null;
        }

        public bool LoadNetwork(NeuralNetwork net, List<List<int>> groupIds)
        {
            network = net;

            weightCap = network.GetWeightCap();

            //To add new activation function bounds, add else if cases.
            if (network.activation == Activation.Logistic)
            {
                activationUpperBound = 1.0;
                activationLowerBound = 0.0;
            }

            maxNeuronValue = activationUpperBound - activationLowerBound;

            if (weightCap == double.MaxValue)
                connectionDrawFunction = DrawConnectionsUncapped;
            else
                connectionDrawFunction = DrawConnectionsCapped;

            List<NeuronGroupDescription> inputLayer = new List<NeuronGroupDescription>();

            for (int i = 0; i < groupIds[INPUT_INDEX].Count; i++)
            {
                NeuralNetwork.NeuronGroup.Identifier ident;
                ident.type = NeuralNetwork.NeuronGroup.Type.INPUT;
                ident.index = groupIds[INPUT_INDEX][i];

                NeuronGroupDescription nGroup = new NeuronGroupDescription();
                nGroup.id = ident.index;
                nGroup.neuronCount = network.GetGroupNeuronCount(ident);
                nGroup.type = ident.type;

                inputLayer.Add(nGroup);
            }

            neuronLayers.Add(inputLayer);

            //First place all groups into layers.
            for (int x = 0; x < neuronLayers.Count; x++)
            {
                List<NeuronGroupDescription> nextLayer = new List<NeuronGroupDescription>();

                for (int y = 0; y < neuronLayers[x].Count; y++)
                {
                    //Output layers do not have any outgoing connections.
                    if (neuronLayers[x][y].type == NeuralNetwork.NeuronGroup.Type.OUTPUT)
                        continue;

                    NeuralNetwork.NeuronGroup.Identifier groupFrom;
                    groupFrom.type = neuronLayers[x][y].type;
                    groupFrom.index = neuronLayers[x][y].id;
                    
                    List<NeuralNetwork.NeuronGroup.Identifier> groupsConnected = network.GetGroupsConnected(groupFrom);

                    for (int z = 0; z < groupsConnected.Count; z++)
                    {
                        //Do not add the same layer twice.
                        if (LayerContains(groupsConnected[z], nextLayer))
                            continue;

                        NeuronGroupDescription nextGroup;

                        //Reuse the group if already added to a preceeding layer.
                        nextGroup = RemoveRepeat(groupsConnected[z], neuronLayers);

                        if (nextGroup == null)
                        {
                            nextGroup = new NeuronGroupDescription();
                            nextGroup.id = groupsConnected[z].index;
                            nextGroup.neuronCount = network.GetGroupNeuronCount(groupsConnected[z]);
                            nextGroup.type = groupsConnected[z].type;
                        }

                        nextLayer.Add(nextGroup);
                    }
                }

                if (nextLayer.Count > 0)
                    neuronLayers.Add(nextLayer);
            }

            //Now add connections.
            for (int x = 0; x < neuronLayers.Count; x++)
            {
                for (int y = 0; y < neuronLayers[x].Count; y++)
                {
                    NeuralNetwork.NeuronGroup.Identifier groupFrom;
                    groupFrom.type = neuronLayers[x][y].type;
                    groupFrom.index = neuronLayers[x][y].id;

                    List<NeuralNetwork.NeuronGroup.Identifier> groupsConnected = network.GetGroupsConnected(groupFrom);

                    //Get the number of weights needed for all the groups connected to this one.
                    for (int a = 0; a < neuronLayers.Count; a++)
                    {
                        for (int b = 0; b < neuronLayers[a].Count; b++)
                        {
                            //Skip connections to self.
                            if (a == x && b == y)
                                continue;

                            NeuralNetwork.NeuronGroup.Identifier toGroup;
                            toGroup.type = neuronLayers[a][b].type;
                            toGroup.index = neuronLayers[a][b].id;

                            if (groupsConnected.Contains(toGroup))
                            {
                                ConnectionDescription cDescription = new ConnectionDescription();
                                cDescription.toGroup = neuronLayers[a][b];
                                cDescription.toLayer = neuronLayers[a];

                                uint weightCount = neuronLayers[x][y].neuronCount * neuronLayers[a][b].neuronCount;

                                for (int i = 0; i < weightCount; i++)
                                    cDescription.weights.Add(1.0);

                                neuronLayers[x][y].connectionsGroups.Add(cDescription);
                            }
                        }
                    }
                }
            }

            ySpacing = Simulator.WORLD_WINDOW_HEIGHT / (double)(neuronLayers.Count + 1);

            //Set up the neuron positions and add values.
            for (int x = 0; x < neuronLayers.Count; x++)
            {
                uint neuronsInLayer = 1;

                for (int y = 0; y < neuronLayers[x].Count; y++)
                    neuronsInLayer += neuronLayers[x][y].neuronCount;

                xSpacing = Simulator.WORLD_WINDOW_WIDTH / (double)neuronsInLayer;

                for (int y = 0; y < neuronLayers[x].Count; y++)
                {
                    neuronLayers[x][y].y = (double)(x + 1) * ySpacing;
                    neuronLayers[x][y].xValues.Capacity = (int)neuronLayers[x][y].neuronCount;

                    uint neuronsPassed = 0;

                    for (int i = 0; i < y; i++)
                        neuronsPassed += neuronLayers[x][i].neuronCount;

                    for (int z = 0; z < neuronLayers[x][y].neuronCount; z++)
                    {
                        neuronLayers[x][y].values.Add(1.0);

                        double xValue = (double)(neuronsPassed + z + 1) * xSpacing;
                        neuronLayers[x][y].xValues.Add(xValue);
                    }
                }
            }

            //Update the visualization with the initial state.
            UpdateVisualization();

            return true;
        }

        private void UpdateVisualization()
        {
            if (network == null)
                return;

            for (int x = 0; x < neuronLayers.Count; x++)
            {
                for (int y = 0; y < neuronLayers[x].Count; y++)
                {
                    NeuralNetwork.NeuronGroup.Identifier fromGroup;
                    fromGroup.type = neuronLayers[x][y].type;
                    fromGroup.index = neuronLayers[x][y].id;

                    List<double> neuronValues = network.GetNeuronValues(fromGroup);

                    for (int i = 0; i < neuronValues.Count; i++)
                        neuronLayers[x][y].values[i] = neuronValues[i];

                    for (int a = 0; a < neuronLayers[x][y].connectionsGroups.Count; a++)
                    {
                        NeuralNetwork.NeuronGroup.Identifier toGroup;
                        toGroup.type = neuronLayers[x][y].connectionsGroups[a].toGroup.type;
                        toGroup.index = neuronLayers[x][y].connectionsGroups[a].toGroup.id;

                        List<double> weights = network.GetWeights(fromGroup, toGroup);

                        //In case an unsupported network is chosen, like two different
                        //connection groups coming from a group going to the same group.
                        if (weights.Count != neuronLayers[x][y].connectionsGroups[a].weights.Count)
                            continue;

                        for (int b = 0; b < neuronLayers[x][y].connectionsGroups[a].weights.Count; b++)
                            neuronLayers[x][y].connectionsGroups[a].weights[b] =  weights[b];
                    }
                }
            }
        }

        private void UpdateUi()
        {
            if (modSigCount > 0)
                modulationDisplay.Text = Raahn.ModulationSignal.GetSignal((int)modulationItem.Value).ToString();

            if (network == null)
                return;

            errorDisplay.Text = network.GetOnlineError().ToString();
        }

        private void Draw()
        {
            GL.Viewport(0, 0, glWidgetWidth, glWidgetHeight);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.MatrixMode(MatrixMode.Projection);

            GL.LoadIdentity();

            GL.Ortho(0.0, Simulator.WORLD_WINDOW_WIDTH, 0.0, Simulator.WORLD_WINDOW_HEIGHT, -1.0, 1.0);

            GL.MatrixMode(MatrixMode.Modelview);

            GL.LoadIdentity();

            connectionDrawFunction();

            DrawNeurons();
        }

        private void DrawConnectionsUncapped()
        {
            for (int x = 0; x < neuronLayers.Count; x++)
            {
                for (int y = 0; y < neuronLayers[x].Count; y++)
                {
                    lineVertices[1] = (float)neuronLayers[x][y].y;

                    //Draw neurons.
                    for (int z = 0; z < neuronLayers[x][y].connectionsGroups.Count; z++)
                    {
                        NeuronGroupDescription toGroup = neuronLayers[x][y].connectionsGroups[z].toGroup;

                        lineVertices[3] = (float)toGroup.y;

                        for (int a = 0; a < neuronLayers[x][y].neuronCount; a++)
                        {
                            for (int b = 0; b < toGroup.neuronCount; b++)
                            {
                                lineVertices[0] = (float)neuronLayers[x][y].xValues[a];
                                lineVertices[2] = (float)toGroup.xValues[b];

                                line.Update();

                                line.MakeCurrent();

                                int weightIndex = a * (int)toGroup.neuronCount + b;
                                double weight = neuronLayers[x][y].connectionsGroups[z].weights[weightIndex];

                                double weightAbs = Math.Abs(weight);

                                //Do not draw connections very close to zero.
                                if (weightAbs < MIN_CONNECTION_STRENGTH)
                                    continue;

                                if (weight > COLOR_THRESHOLD)
                                    GL.Color4(0.0, 0.0, 1.0, 1.0);
                                else
                                    GL.Color4(1.0, 0.0, 0.0, 1.0);

                                float connectionWidth = (float)weightAbs;

                                if (connectionWidth > MAX_CONNECTION_WIDTH)
                                    GL.LineWidth(MAX_CONNECTION_WIDTH);
                                else
                                    GL.LineWidth(connectionWidth);

                                GL.DrawElements(line.GetRenderMode(), line.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);
                            }
                        }
                    }
                }
            }
        }

        private void DrawConnectionsCapped()
        {
            for (int x = 0; x < neuronLayers.Count; x++)
            {
                for (int y = 0; y < neuronLayers[x].Count; y++)
                {
                    lineVertices[1] = (float)neuronLayers[x][y].y;

                    //Draw neurons.
                    for (int z = 0; z < neuronLayers[x][y].connectionsGroups.Count; z++)
                    {
                        NeuronGroupDescription toGroup = neuronLayers[x][y].connectionsGroups[z].toGroup;

                        lineVertices[3] = (float)toGroup.y;

                        for (int a = 0; a < neuronLayers[x][y].neuronCount; a++)
                        {
                            for (int b = 0; b < toGroup.neuronCount; b++)
                            {
                                lineVertices[0] = (float)neuronLayers[x][y].xValues[a];
                                lineVertices[2] = (float)toGroup.xValues[b];

                                line.Update();

                                line.MakeCurrent();

                                int weightIndex = a * (int)toGroup.neuronCount + b;
                                double weight = neuronLayers[x][y].connectionsGroups[z].weights[weightIndex];

                                double weightAbs = Math.Abs(weight);

                                //Do not draw connections very close to zero.
                                if (weightAbs < MIN_CONNECTION_STRENGTH)
                                    continue;

                                if (weight > COLOR_THRESHOLD)
                                    GL.Color4(0.0, 0.0, 1.0, 1.0);
                                else
                                    GL.Color4(1.0, 0.0, 0.0, 1.0);

                                float connectionWidth = (float)(weightAbs / weightCap) * MAX_CONNECTION_WIDTH;

                                GL.LineWidth(connectionWidth);

                                GL.DrawElements(line.GetRenderMode(), line.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);
                            }
                        }
                    }
                }
            }
        }

        private void DrawNeurons()
        {
            //Now draw neurons.
            point.MakeCurrent();

            //Draw the input layer separately.
            for (int y = 0; y < neuronLayers[0].Count; y++)
            {
                //Draw neurons.
                for (int z = 0; z < neuronLayers[0][y].neuronCount; z++)
                {
                    double neuronValue = neuronLayers[0][y].values[z];

                    //Do not draw very weak neurons.
                    if (neuronValue < MIN_NEURON_VALUE)
                        continue;

                    if (neuronValue > COLOR_THRESHOLD)
                        GL.Color4(0.0, 0.0, 1.0, 1.0);
                    else
                        GL.Color4(1.0, 0.0, 0.0, 1.0);

                    float neuronSize = (float)(neuronValue / MAX_INPUT_NEURON_VALUE) * MAX_NEURON_SIZE;

                    GL.PointSize(neuronSize);

                    GL.PushMatrix();

                    GL.Translate(neuronLayers[0][y].xValues[z], neuronLayers[0][y].y, Utils.DISCARD_Z_POS);

                    GL.DrawElements(point.GetRenderMode(), point.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);

                    GL.PopMatrix();
                }
            }


            for (int x = 1; x < neuronLayers.Count; x++)
            {
                for (int y = 0; y < neuronLayers[x].Count; y++)
                {
                    //Draw neurons.
                    for (int z = 0; z < neuronLayers[x][y].neuronCount; z++)
                    {
                        double neuronValue = neuronLayers[x][y].values[z];

                        //Do not draw very weak neurons.
                        if (neuronValue < MIN_NEURON_VALUE)
                            continue;

                        if (neuronValue > COLOR_THRESHOLD)
                            GL.Color4(0.0, 0.0, 1.0, 1.0);
                        else
                            GL.Color4(1.0, 0.0, 0.0, 1.0);

                        neuronValue += Math.Abs(activationLowerBound);

                        float neuronSize = (float)(neuronValue / maxNeuronValue) * MAX_NEURON_SIZE;

                        GL.PointSize(neuronSize);

                        GL.PushMatrix();

                        GL.Translate(neuronLayers[x][y].xValues[z], neuronLayers[x][y].y, Utils.DISCARD_Z_POS);

                        GL.DrawElements(point.GetRenderMode(), point.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);

                        GL.PopMatrix();
                    }
                }
            }
        }

        private void InitGraphics()
        {
            GL.ClearColor(Utils.BACKGROUND_COLOR_VALUE, Utils.BACKGROUND_COLOR_VALUE, Utils.BACKGROUND_COLOR_VALUE, 1.0f);

            GL.PointSize(MAX_NEURON_SIZE);

            GL.EnableClientState(ArrayCap.VertexArray);

            point.Allocate(BufferUsageHint.StaticDraw);
            line.Allocate(BufferUsageHint.DynamicDraw);

            Gdk.Rectangle widgetBox = visualizerGLWidget.Allocation;

            glWidgetWidth = widgetBox.Width;
            glWidgetHeight = widgetBox.Height;

            GL.Viewport(0, 0, (int)glWidgetWidth, (int)glWidgetHeight);
        }

        private void Clean(object sender, Gtk.DeleteEventArgs args)
        {
            point.Free();
            line.Free();

            if (visualizerGLWidget.IsRealized)
                visualizerGLWidget.Dispose();

            visualizerWindow.Destroy();

            visualizerGLWidget = null;
            visualizerWindow = null;
        }

        //Window moved or resized. Must use GLib.ConnectBefore
        //to avoid an event terminating the cycle before this event.
        [GLib.ConnectBefore]
        private void OnConfigure(object sender, Gtk.ConfigureEventArgs args)
        {
            glWidgetWidth = args.Event.Width;
            glWidgetHeight = args.Event.Height;
        }

        //Used to prevent the same group from being added to a layer.
        private bool LayerContains(NeuralNetwork.NeuronGroup.Identifier ident, List<NeuronGroupDescription> layer)
        {
            for (int i = 0; i < layer.Count; i++)
            {
                if (layer[i].id == ident.index && layer[i].type == ident.type)
                    return true;
            }

            return false;
        }

        //Used to reposition a group into a new layer.
        private NeuronGroupDescription RemoveRepeat(NeuralNetwork.NeuronGroup.Identifier ident, List<List<NeuronGroupDescription>> layers)
        {
            for (int x = 0; x < layers.Count; x++)
            {
                for (int y = 0; y < layers[x].Count; y++)
                {
                    if (layers[x][y].id == ident.index && layers[x][y].type == ident.type)
                    {
                        NeuronGroupDescription repeat = layers[x][y];
                        neuronLayers[x].RemoveAt(y);

                        return repeat;
                    }
                }
            }

            return null;
        }
    }
}

