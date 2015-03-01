using System;
using System.IO;
using System.Xml.Serialization;

namespace RaahnSimulation
{
    public class MenuState : State
    {
        private static MenuState menuState = new MenuState();
        private Text title;
        private Text version;
        private Button startSim;
        private Button startMap;
        private ClickableEntity.OnClickType startSimOnClick;
        private ClickableEntity.OnClickType startMapOnClick;

        public MenuState()
        {
            startSimOnClick = StartSimOnClick;
            startMapOnClick = StartMapOnClick;
        }

        public override void Init(Simulator sim)
        {
            base.Init(sim);

            double charWidth = Text.CHAR_DEFAULT_WIDTH;
            double charHeight = Text.CHAR_DEFAULT_HEIGHT;

            title = new Text(context, Utils.WINDOW_TITLE);
            title.SetTransformUsage(false);
            title.SetCharBounds(Simulator.WORLD_WINDOW_WIDTH / 2.0, Simulator.WORLD_WINDOW_HEIGHT - charHeight, charWidth, charHeight, true);

            version = new Text(context, Utils.VERSION_STRING);
            version.SetTransformUsage(false);
            version.SetCharBounds(0.0, 0.0, charWidth, charHeight, false);

            double startSimWidth = charWidth * Utils.START_SIM.Length;

            startSim = new Button(context, Utils.START_SIM);
            startSim.SetTransformUsage(false);
            startSim.SetBounds(Simulator.WORLD_WINDOW_WIDTH / 2.0, title.worldPos.y - (2.0 * charHeight), startSimWidth, charHeight, true);
            startSim.SetOnClickListener(startSimOnClick);

            double startMapWidth = charWidth * Utils.START_MAP.Length;

            startMap = new Button(context, Utils.START_MAP);
            startMap.SetTransformUsage(false);
            startMap.SetBounds(Simulator.WORLD_WINDOW_WIDTH / 2.0, startSim.worldPos.y - (2.0 * charHeight), startMapWidth, charHeight, true);
            startMap.SetOnClickListener(startMapOnClick);

            AddEntity(title, 0);
            AddEntity(version, 0);
            AddEntity(startSim, 0);
            AddEntity(startMap, 0);
        }

        public override void Update()
        {
            base.Update();
        }

        public override void UpdateEvent(Event e)
        {
            base.UpdateEvent(e);
        }

        public override void Draw()
        {
            base.Draw();
        }

        public static MenuState Instance()
        {
            return menuState;
        }

        public static void StartSimOnClick(Simulator sim)
        {
            bool configChoosen = true;

            Gtk.FileChooserDialog expChooser = new Gtk.FileChooserDialog(Utils.CHOOSE_EXPERIMENT_FILE, null, Gtk.FileChooserAction.Open);
            expChooser.AddButton(Utils.OPEN_BUTTON, Gtk.ResponseType.Ok);
            expChooser.AddButton(Utils.CANCEL_BUTTON, Gtk.ResponseType.Cancel);
            expChooser.SetCurrentFolder(Utils.EXPERIMENT_FOLDER);

            if ((Gtk.ResponseType)expChooser.Run() == Gtk.ResponseType.Ok)
            {
                TextReader expReader = new StreamReader(expChooser.Filename);

                try
                {
                    XmlSerializer deserializer = new XmlSerializer(typeof(Experiment));
                    SimState.Instance().experiment = (Experiment)deserializer.Deserialize(expReader);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    configChoosen = false;

                    Gtk.MessageDialog errorDialog = new Gtk.MessageDialog(null, Gtk.DialogFlags.Modal, Gtk.MessageType.Error, 
                                                                          Gtk.ButtonsType.Ok, Utils.XML_READ_ERROR);
                    errorDialog.Run();
                    errorDialog.Destroy();
                }
                finally
                {
                    expReader.Close();
                }
            }
            else
                configChoosen = false;

            expChooser.Destroy();

            if (!configChoosen)
                return;

            sim.RequestStateChange(Simulator.StateChangeType.PUSH, SimState.Instance());
        }

        public static void StartMapOnClick(Simulator sim)
        {
            sim.RequestStateChange(Simulator.StateChangeType.PUSH, MapState.Instance());
        }
    }
}