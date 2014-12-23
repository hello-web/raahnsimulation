using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public class Car : Entity
	{
		private const double CAR_SPEED_X = 960.0;
		private const double CAR_SPEED_Y = 540.0;
		//120 degrees per second.
		private const double CAR_ROTATE_SPEED = 120.0;

        public List<Entity> entitiesHovering;
        private bool configLoaded;
        private QuadTree quadTree;
        private List<RangeFinderGroup> rangeFinderGroups;
        private List<PieSliceSensorGroup> pieSliceSensorGroups;

	    public Car(Simulator sim, QuadTree tree) : base(sim)
	    {
	        texture = TextureManager.TextureType.CAR;

            quadTree = tree;

            rangeFinderGroups = new List<RangeFinderGroup>();
            pieSliceSensorGroups = new List<PieSliceSensorGroup>();

	        speed.x = CAR_SPEED_X;
	        speed.y = CAR_SPEED_Y;

            entitiesHovering = new List<Entity>();
	    }

	    public override void Update()
	    {
	        if (Keyboard.IsKeyPressed(Keyboard.Key.Left))
	            angle += CAR_ROTATE_SPEED * context.GetDeltaTime();
	        if (Keyboard.IsKeyPressed(Keyboard.Key.Right))
	            angle -= CAR_ROTATE_SPEED * context.GetDeltaTime();

	        if (Keyboard.IsKeyPressed(Keyboard.Key.Up))
	        {
	            transformedWorldPos.x += velocity.x * context.GetDeltaTime();
	            transformedWorldPos.y += velocity.y * context.GetDeltaTime();
	        }
	        if (Keyboard.IsKeyPressed(Keyboard.Key.Down))
	        {
	            transformedWorldPos.x -= velocity.x * context.GetDeltaTime();
	            transformedWorldPos.y -= velocity.y * context.GetDeltaTime();
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
            SensorConfig config = null;

            try
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(SensorConfig));
                config = (SensorConfig)deserializer.Deserialize(configReader);
            }
            catch (Exception e)
            {
                Console.WriteLine(Utils.XML_READ_ERROR);
                Console.WriteLine(Utils.CONFIG_LOAD_ERROR);
                Console.WriteLine(e.Message);

                return false;
            }
            finally
            {
                configReader.Close();
            }

            if (config.rangeFinderGroups != null)
            {
                for (int i = 0; i < config.rangeFinderGroups.Length; i++)
                {
                    RangeFinderGroupConfig current = config.rangeFinderGroups[i];

                    if (current == null)
                        continue;

                    RangeFinderGroup rfg = new RangeFinderGroup(context, this, quadTree, config.rangeFinderGroups[i].count);
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

            if (config.pieSliceSensorGroups != null)
            {
                for (int i = 0; i < config.pieSliceSensorGroups.Length; i++)
                {
                    PieSliceSensorGroupConfig current = config.pieSliceSensorGroups[i];

                    if (current == null)
                        continue;

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

            configLoaded = true;

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
