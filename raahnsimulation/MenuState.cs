using System;
using System.IO;
using System.Xml.Serialization;
using Gtk;

namespace RaahnSimulation
{
    public class MenuState : State
    {
        private static MenuState menuState = new MenuState();
        private VBox buttonMenu;
        private Gtk.Button startSimulation;
        private Gtk.Button startMap;

        public MenuState()
        {

        }

        public override void Init(Simulator sim)
        {
            base.Init(sim);

            context.SetGLVisible(false);

            buttonMenu = new VBox();
            buttonMenu.SetSizeRequest((int)context.GetWindowWidth(), (int)context.GetWindowHeight() - Simulator.MENU_OFFSET);
            buttonMenu.Spacing = 10;

            startSimulation = new Gtk.Button(Utils.START_SIM);
            startSimulation.Clicked += delegate { StartSimOnClick(); };

            startMap = new Gtk.Button(Utils.START_MAP);
            startMap.Clicked += delegate { StartMapOnClick(); };

            buttonMenu.Add(startSimulation);
            buttonMenu.Add(startMap);

            Gtk.Fixed mainContainer = context.GetMainContainer();
            mainContainer.Put(buttonMenu, 0, Simulator.MENU_OFFSET);

            buttonMenu.Show();
            startSimulation.Show();
            startMap.Show();
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

        public void StartSimOnClick()
        {
            bool configChoosen = true;

            Gtk.Window win = context.GetWindow();

            Gtk.FileChooserDialog expChooser = new Gtk.FileChooserDialog(Utils.CHOOSE_EXPERIMENT_FILE, win, Gtk.FileChooserAction.Open);
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

                    Gtk.MessageDialog errorDialog = new Gtk.MessageDialog(win, Gtk.DialogFlags.Modal, Gtk.MessageType.Error, 
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

            buttonMenu.Visible = false;
            startSimulation.Visible = false;
            startMap.Visible = false;

            context.RequestStateChange(Simulator.StateChangeType.PUSH, SimState.Instance());
        }

        public void StartMapOnClick()
        {
            buttonMenu.Visible = false;
            startSimulation.Visible = false;
            startMap.Visible = false;

            context.RequestStateChange(Simulator.StateChangeType.PUSH, MapState.Instance());
        }
    }
}