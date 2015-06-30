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
            public NeuronGroup.Type type;
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

        private const int INPUT_INDEX = (int)NeuronGroup.Type.INPUT;
        private const float MAX_NEURON_SIZE = 25.0f;
        private const float MAX_CONNECTION_WIDTH = 10.0f;
        private const float ARROW_WIDTH = 5.0f;
        private const float ARROW_MID_X = 3600.0f;
        private const float ARROW_MID_Y_1 = 1000.0f;
        private const float ARROW_MID_Y_2 = 1500.0f;
        private const float ARROW_LEFT_X = 3500.0f;
        private const float ARROW_RIGHT_X = 3700.0f;
        private const float ARROW_LEFT_RIGHT_Y = 1250.0f;
        private const double MIN_CONNECTION_STRENGTH = 0.00001;
        private const double MAX_INPUT_NEURON_VALUE = 1.0;
        private const double MIN_NEURON_VALUE = 0.00001;
        private const double COLOR_THRESHOLD = 0.0;

        //The widget itself stores its width and height as ints.
        private int glWidgetWidth;
        private int glWidgetHeight;
        private uint visualizerWidth;
        private uint visualizerHeight;
        private double weightCap;
        private double maxNeuronValue;
        private double activationUpperBound;
        private double activationLowerBound;
        private double xSpacing;
        private double ySpacing;
        private float[] lineVertices = { 0.0f, 0.0f, 0.0f, 0.0f };
        private Gtk.Window visualizerWindow;
        private GLWidget visualizerGLWidget;
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

            visualizerGLWidget = new GLWidget(GraphicsMode.Default, InitGraphics, Draw);

            visualizerWindow.Add(visualizerGLWidget);

            visualizerWindow.ShowAll();
        }

        public void UpdateWindow()
        {
            if (visualizerGLWidget != null)
            {
                if (visualizerGLWidget.Visible)
                {
                    Update();
                    visualizerGLWidget.RenderFrame();
                }
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
                NeuronGroup.Identifier ident;
                ident.type = NeuronGroup.Type.INPUT;
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
                    if (neuronLayers[x][y].type == NeuronGroup.Type.OUTPUT)
                        continue;

                    NeuronGroup.Identifier groupFrom;
                    groupFrom.type = neuronLayers[x][y].type;
                    groupFrom.index = neuronLayers[x][y].id;
                    
                    List<NeuronGroup.Identifier> groupsConnected = network.GetGroupsConnected(groupFrom);

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
                    NeuronGroup.Identifier groupFrom;
                    groupFrom.type = neuronLayers[x][y].type;
                    groupFrom.index = neuronLayers[x][y].id;

                    List<NeuronGroup.Identifier> groupsConnected = network.GetGroupsConnected(groupFrom);

                    for (int a = 0; a < neuronLayers.Count; a++)
                    {
                        for (int b = 0; b < neuronLayers[a].Count; b++)
                        {
                            //Skip connections to self.
                            if (a == x && b == y)
                                continue;

                            NeuronGroup.Identifier toGroup;
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
            Update();

            return true;
        }

        private void Update()
        {
            if (network == null)
                return;

            for (int x = 0; x < neuronLayers.Count; x++)
            {
                for (int y = 0; y < neuronLayers[x].Count; y++)
                {
                    NeuronGroup.Identifier fromGroup;
                    fromGroup.type = neuronLayers[x][y].type;
                    fromGroup.index = neuronLayers[x][y].id;

                    List<double> neuronValues = network.GetNeuronValues(fromGroup);

                    for (int i = 0; i < neuronValues.Count; i++)
                        neuronLayers[x][y].values[i] = neuronValues[i];

                    for (int a = 0; a < neuronLayers[x][y].connectionsGroups.Count; a++)
                    {
                        NeuronGroup.Identifier toGroup;
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

            DrawArrow();
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

                                int weightIndex = b * (int)neuronLayers[x][y].neuronCount + a;
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

                                int weightIndex = b * (int)neuronLayers[x][y].neuronCount + a;
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

        private void DrawArrow()
        {
            GL.Color4(0.0f, 0.0f, 1.0f, 1.0f);

            line.MakeCurrent();

            GL.LineWidth(ARROW_WIDTH);

            //Middle part.
            lineVertices[0] = ARROW_MID_X;
            lineVertices[1] = ARROW_MID_Y_1;
            lineVertices[2] = ARROW_MID_X;
            lineVertices[3] = ARROW_MID_Y_2;

            line.Update();

            GL.DrawElements(line.GetRenderMode(), line.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);

            //Left part.
            lineVertices[0] = ARROW_MID_X;
            lineVertices[1] = ARROW_MID_Y_2;
            lineVertices[2] = ARROW_LEFT_X;
            lineVertices[3] = ARROW_LEFT_RIGHT_Y;

            line.Update();

            GL.DrawElements(line.GetRenderMode(), line.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);

            //Right part.
            lineVertices[0] = ARROW_MID_X;
            lineVertices[1] = ARROW_MID_Y_2;
            lineVertices[2] = ARROW_RIGHT_X;
            lineVertices[3] = ARROW_LEFT_RIGHT_Y;

            line.Update();

            GL.DrawElements(line.GetRenderMode(), line.GetIndexCount(), DrawElementsType.UnsignedShort, IntPtr.Zero);
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
        private bool LayerContains(NeuronGroup.Identifier ident, List<NeuronGroupDescription> layer)
        {
            for (int i = 0; i < layer.Count; i++)
            {
                if (layer[i].id == ident.index && layer[i].type == ident.type)
                    return true;
            }

            return false;
        }

        //Used to reposition a group into a new layer.
        private NeuronGroupDescription RemoveRepeat(NeuronGroup.Identifier ident, List<List<NeuronGroupDescription>> layers)
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

