using System;
using System.IO;
using System.Xml.Serialization;
using Gtk;

namespace RaahnSimulation
{
    public class MenuState : State
    {
        private const int SPACING = 20;
        private const uint PADDING = 0;

        private static MenuState menuState = new MenuState();

        private MenuBar menuBar;
        private Gtk.Button startSimulation;
        private Gtk.Button startMap;
        private Gtk.Window mainWindow;

        public MenuState()
        {
            menuBar = null;
            startSimulation = null;
            startMap = null;
            mainWindow = null;
        }

        public override bool Init(Simulator sim)
        {
            if (!base.Init(sim))
                return false;

            mainWindow = context.GetWindow();

            mainContainer = new VBox();
            VBox mcVbox = (VBox)mainContainer;

            mcVbox.Spacing = SPACING;

            menuBar = new MenuBar();

            MenuItem helpOption = new MenuItem(Utils.MENU_HELP);
            Menu helpMenu = new Menu();
            helpOption.Submenu = helpMenu;

            MenuItem aboutItem = new MenuItem(Utils.MENU_ABOUT);
            aboutItem.Activated += delegate { context.DisplayAboutDialog(); };
            helpMenu.Append(aboutItem);

            menuBar.Append(helpOption);

            startSimulation = new Gtk.Button(Utils.START_SIM);
            startSimulation.Clicked += delegate { StartSimOnClick(); };

            startMap = new Gtk.Button(Utils.START_MAP);
            startMap.Clicked += delegate { StartMapOnClick(); };

            mcVbox.PackStart(menuBar, false, true, PADDING);
            mcVbox.PackStart(startSimulation, false, false, PADDING);
            mcVbox.PackStart(startMap, false, false, PADDING);

            mainWindow.Add(mainContainer);

            mainContainer.ShowAll();

            return true;
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

            context.RequestStateChange(Simulator.StateChangeType.PUSH, SimState.Instance());
        }

        public void StartMapOnClick()
        {
            context.RequestStateChange(Simulator.StateChangeType.PUSH, MapState.Instance());
        }
    }
}