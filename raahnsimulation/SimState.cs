using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace RaahnSimulation
{
    public class SimState : State
    {
        private const double CAR_WIDTH = 260.0;
        private const double CAR_HEIGHT = 160.0;
        private const double HIGHLIGHT_R = 0.0;
        private const double HIGHLIGHT_G = 1.0;
        private const double HIGHLIGHT_B = 0.0;
        private const double HIGHLIGHT_T = 1.0;

        private static SimState simState = new SimState();

        //Experiment must be initialized outside of SimState.
        public Experiment experiment;
        private bool panning;
        private QuadTree quadTree;
        private Camera camera;
        private Cursor cursor;
        private Car raahnCar;
        private EntityMap EntityMap;

        public SimState()
        {
            experiment = null;
            panning = false;
            experiment = null;
            quadTree = null;
            camera = null;
            raahnCar = null;
            EntityMap = null;
        }

        public override void Init(Simulator context)
        {
            base.Init(context);

            camera = context.GetCamera();

            quadTree = new QuadTree(new AABB(Simulator.WORLD_WINDOW_WIDTH, Simulator.WORLD_WINDOW_HEIGHT));

            Gtk.Window simWindow = context.GetWindow();

            simWindow.GdkWindow.Cursor = context.GetBlankCursor();

            uint newWinWidth = (uint)((double)simWindow.Screen.Width * Utils.DEFAULT_SCREEN_WIDTH_PERCENTAGE);
            uint newWinHeight = (uint)((double)simWindow.Screen.Height * Utils.DEFAULT_SCREEN_HEIGHT_PERCENTAGE);

            context.SetWindowSize(newWinWidth, newWinHeight);
            context.CenterWindow();

            context.SetGLVisible(true);
            context.ResizeGL(newWinWidth, newWinHeight - Simulator.MENU_OFFSET);

            cursor = new Cursor(context);

            raahnCar = new Car(context, quadTree);

            if (experiment != null)
                raahnCar.LoadConfig(Utils.SENSOR_FOLDER + experiment.sensorFile);

            raahnCar.SetWidth(CAR_WIDTH);
            raahnCar.SetHeight(CAR_HEIGHT);
            raahnCar.SetPosition(0.0, 0.0);
            raahnCar.Update();

            if (experiment != null)
            {
                string mapFilePath = Utils.MAP_FOLDER + experiment.mapFile;
                EntityMap = new EntityMap(context, 0, raahnCar, quadTree, mapFilePath);
            }
            else
                EntityMap = new EntityMap(context, 0, raahnCar, quadTree);

            AddEntity(raahnCar, 0);
            AddEntity(cursor, 1);

            quadTree.AddEntity(raahnCar);
        }

        public override void Update()
        {
            cursor.Update();

            int mouseX;
            int mouseY;

            context.GetWindow().GetPointer(out mouseX, out mouseY);

            if (mouseX < 0 || mouseY < 0)
                panning = false;
            else if (mouseX > context.GetWindowWidth() || mouseY > context.GetWindowHeight())
                panning = false;

            if (panning)
            {
                Utils.Vector2 deltaPos = cursor.GetDeltaPosition();
                camera.Pan(-deltaPos.x, -deltaPos.y);
            }

            base.Update();
            EntityMap.Update();
            quadTree.Update();

            Utils.Vector2 lowerLeft = camera.TransformWorld(0.0, 0.0);
            Utils.Vector2 upperRight = camera.TransformWorld(Simulator.WORLD_WINDOW_WIDTH, Simulator.WORLD_WINDOW_HEIGHT);

            AABB viewBounds = new AABB(upperRight.x - lowerLeft.x, upperRight.y - lowerLeft.y);
            viewBounds.Translate(lowerLeft.x, lowerLeft.y);

            List<Entity> entitiesInBounds = quadTree.Query(viewBounds);

            //We want to check if raahnCar intersects anything,
            //but we should not check if it intersects itself.
            if (entitiesInBounds.Contains(raahnCar))
                entitiesInBounds.Remove(raahnCar);

            //Reset the list of entities raahnCar collides with.
            raahnCar.entitiesHovering.Clear();

            for (int i = 0; i < entitiesInBounds.Count; i++)
            {
                //Only colorable entities are added to the quad tree,
                //so we can cast it to a colorable entity.
                Entity curEntity = (Entity)entitiesInBounds[i];

                if (raahnCar.aabb.Intersects(curEntity.aabb.GetBounds()))
                {
                    raahnCar.entitiesHovering.Add(curEntity);
                    curEntity.SetColor(HIGHLIGHT_R, HIGHLIGHT_G, HIGHLIGHT_B, HIGHLIGHT_T);
                }
                else
                    curEntity.SetColor(Entity.DEFAULT_COLOR_R, Entity.DEFAULT_COLOR_G, Entity.DEFAULT_COLOR_B, Entity.DEFAULT_COLOR_T);
            }
        }

        public override void UpdateEvent(Event e)
        {
            if (e.type == Gdk.EventType.Scroll)
            {
                double mouseX = (double)e.X;
                double mouseY = (double)(context.GetWindowHeight() - e.Y);
                Utils.Vector2 transform = camera.ProjectWindow(mouseX, mouseY);

                if (e.scrollDirection == Gdk.ScrollDirection.Up)
                    camera.ZoomTo(transform.x, transform.y, Camera.MOUSE_SCROLL_ZOOM);
                else if (e.scrollDirection == Gdk.ScrollDirection.Down)
                    camera.ZoomTo(transform.x, transform.y, (1.0 / Camera.MOUSE_SCROLL_ZOOM));
            }
            else if (e.type == Gdk.EventType.MotionNotify)
            {
                Gtk.Window simWindow = context.GetWindow();

                if (e.Y < Simulator.MENU_OFFSET)
                    simWindow.GdkWindow.Cursor = null;
                else
                    simWindow.GdkWindow.Cursor = context.GetBlankCursor();
            }
            else if (e.type == Gdk.EventType.ButtonPress)
            {
                if (e.button == Utils.GTK_BUTTON_LEFT)
                    panning = true;
            }
            else if (e.type == Gdk.EventType.ButtonRelease)
            {
                if (e.button == Utils.GTK_BUTTON_LEFT)
                    panning = false;
            }

            EntityMap.UpdateEvent(e);

            base.UpdateEvent(e);
        }

        public override void Draw()
        {
            base.Draw();

            if (context.debugging)
            {
                quadTree.DebugDraw();
            }
        }

        public override void Clean()
        {
            base.Clean();
        }

        public static SimState Instance()
        {
            return simState;
        }
    }
}