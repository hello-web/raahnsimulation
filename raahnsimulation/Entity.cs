using System;
using System.Xml.Serialization;
using Tao.OpenGl;

namespace RaahnSimulation
{
    [XmlRoot("Entity")]
    public class EntityConfig
    {
        [XmlElement("X")]
        public double x;

        [XmlElement("Y")]
        public double y;

        [XmlElement("Angle")]
        public double angle;

        [XmlElement("Type")]
        public string type;
    }

	public abstract class Entity
	{
        public enum EntityType
        {
            NONE = -2,
            GENERIC = -1,
            WALL = 0
        };

        public static readonly string[] ENTITY_TYPE_STRINGS = 
        {
            "Wall"
        };

		public const double ROTATE_SPEED = 90.0;
        public const double DEFAULT_COLOR_R = 1.0;
        public const double DEFAULT_COLOR_G = 1.0;
        public const double DEFAULT_COLOR_B = 1.0;
        public const double DEFAULT_COLOR_T = 1.0;

        public bool visible;
		public double angle;
		public Utils.Vector2 transformedWorldPos;
		public Utils.Vector2 worldPos;
		public Utils.Vector2 drawingVec;
        public AABB aabb;
        protected double width;
        protected double height;
        protected double transparency;
		protected Simulator context;
        protected Utils.Vector2 center;
		protected Utils.Vector2 velocity;
		protected Utils.Vector2 speed;
        protected Utils.Vector3 color;
        protected EntityType type;
		protected TextureManager.TextureType texture;
        protected Mesh mesh;
        private bool moved;
        private double previousAngle;
        private Utils.Vector2 previousPos;

		protected Entity()
		{

		}

	    protected Entity(Simulator sim)
	    {
            visible = true;

            type = EntityType.GENERIC;
	        texture = TextureManager.TextureType.NONE;
            //Default to quad mesh.
            mesh = Simulator.quad;
	        context = sim;
	        width = 1.0;
	        height = 1.0;
	        angle = 0.0;
            moved = false;
            previousAngle = 0.0;
            //Initially opaque.
            transparency = DEFAULT_COLOR_T;

            aabb = new AABB();
            aabb.SetSize(width, height);

	        worldPos = new Utils.Vector2(0.0, 0.0);
	        transformedWorldPos = new Utils.Vector2(0.0, 0.0);
            velocity = new Utils.Vector2(0.0, 0.0);
            speed = new Utils.Vector2(0.0, 0.0);
            center = new Utils.Vector2(0.0, 0.0);
            previousPos = new Utils.Vector2(0.0, 0.0);
            //Initially no change on color.
            color = new Utils.Vector3(DEFAULT_COLOR_R, DEFAULT_COLOR_G, DEFAULT_COLOR_B);

	        //Default drawing vector is transformedWorldPos.
	        drawingVec = transformedWorldPos;
	        velocity.x = 0.0;
	        velocity.y = 0.0;
	        speed.x = 1.0;
	        speed.y = 1.0;
	    }

	    ~Entity()
	    {

	    }

        public static string GetStringFromType(EntityType entityType)
        {
            int typeInt = (int)entityType;

            if (typeInt > 0 && typeInt < ENTITY_TYPE_STRINGS.Length)
                return ENTITY_TYPE_STRINGS[(int)entityType];
            else
                return null;
        }

        public static EntityType GetTypeFromString(string typeString)
        {
            for (int i = 0; i < ENTITY_TYPE_STRINGS.Length; i++)
            {
                if (typeString.Equals(ENTITY_TYPE_STRINGS[i]))
                    return (EntityType)i;
            }

            return EntityType.NONE;
        }

	    public virtual void Update()
	    {
            UpdateCoordinates();

            double deltaAngle = angle - previousAngle;

            if (deltaAngle != 0.0)
            {
                aabb.Rotate(deltaAngle);
                previousAngle = angle;
            }

            double deltaX = drawingVec.x - previousPos.x;
            double deltaY = drawingVec.y - previousPos.y;

            if (deltaX != 0.0 || deltaY != 0.0)
            {
                aabb.Translate(deltaX, deltaY);
                previousPos.x = drawingVec.x;
                previousPos.y = drawingVec.y;
                moved = true;
            }
            else
                moved = false;
	    }

        public virtual void UpdateEvent(Event e)
        {

        }

	    public virtual void Draw()
	    {
	        if (context.GetTexMan().GetCurrentTexture() != texture)
	            context.GetTexMan().SetTexture(texture);

            if (!mesh.IsCurrent())
                mesh.MakeCurrent();
	    }

        public void UpdateCoordinates()
        {
            Camera cam = context.GetCamera();

            if (drawingVec == worldPos)
            {
                Utils.Vector2 transform = cam.TransformWorld(worldPos);
                transformedWorldPos.x = transform.x;
                transformedWorldPos.y = transform.y;
            }
            else
            {
                Utils.Vector2 transform = cam.UntransformWorld(transformedWorldPos);
                worldPos.x = transform.x;
                worldPos.y = transform.y;
            }

            center.x = drawingVec.x + (width / 2.0);
            center.y = drawingVec.y + (height / 2.0);

            //OpenGL uses degress, standard math uses radians.
            velocity.x = Math.Cos(Utils.DegToRad(angle)) * speed.x;
            velocity.y = Math.Sin(Utils.DegToRad(angle)) * speed.y;
        }

        public bool Moved()
        {
            return moved;
        }

        public double GetWidth()
        {
            return width;
        }

        public double GetHeight()
        {
            return height;
        }

        public double GetTransparency()
        {
            return transparency;
        }

        public TextureManager.TextureType GetTexture()
        {
            return texture;
        }

        public Utils.Vector2 GetCenter()
        {
            return center;
        }

        public Utils.Vector3 GetColor()
        {
            return color;
        }

        public EntityType GetEntityType()
        {
            return type;
        }

        public virtual void SetWidth(double w)
        {
            width = w;
            aabb.SetSize(width, height);
        }

        public virtual void SetHeight(double h)
        {
            height = h;
            aabb.SetSize(width, height);
        }

		public void SetTexture(TextureManager.TextureType t)
		{
			texture = t;
		}

        public void SetMesh(Mesh newMesh)
        {
            mesh = newMesh;
        }

		public void SetTransformUsage(bool usage)
		{
			if (usage)
				drawingVec = transformedWorldPos;
			else
				drawingVec = worldPos;
		}

		public virtual bool Intersects(double x, double y)
		{
			if (x >= aabb.GetBounds().left && x <= aabb.GetBounds().right)
			{
				if (y >= aabb.GetBounds().bottom && y <= aabb.GetBounds().top)
					return true;
			}
			return false;
		}

		public virtual bool Intersects(Utils.Rect r)
		{
			if (r.left >= aabb.GetBounds().right || r.right <= aabb.GetBounds().left
            || r.bottom >= aabb.GetBounds().top || r.top <= aabb.GetBounds().bottom)
				return false;
			else
				return true;
		}

        public virtual void DebugDraw()
        {
            //Handle all generic debug drawing here.
            Simulator.lineRect.MakeCurrent();
            aabb.DebugDraw();
            Simulator.quad.MakeCurrent();
        }

        //If any clean operations are needed,
        //they can be added by overriding Clean()
        //Not abstract to avoid being forced to
        //Override Clean().
        public virtual void Clean()
        {

        }

	    protected void RotateAroundCenter()
	    {
	        Gl.glTranslated(center.x, center.y, Utils.DISCARD_Z_POS);
	        Gl.glRotated(angle, 0.0, 0.0, 1.0);
	        Gl.glTranslated(-center.x, -center.y, -Utils.DISCARD_Z_POS);
	    }
	}
}
