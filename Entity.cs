using System;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public abstract class Entity : Updateable
	{
        public enum EntityType
        {
            NONE = -2,
            GENERIC = -1,
            ROAD = 0
        };

		public const float ROTATE_SPEED = 90.0f;
        public const float DEFAULT_COLOR_R = 1.0f;
        public const float DEFAULT_COLOR_G = 1.0f;
        public const float DEFAULT_COLOR_B = 1.0f;
        public const float DEFAULT_COLOR_T = 1.0f;

        public bool visible;
		public float angle;
		public Utils.Vector2 worldPos;
		public Utils.Vector2 windowPos;
		public Utils.Vector2 drawingVec;
        public AABB aabb;
        protected float width;
        protected float height;
        protected float transparency;
		protected Simulator context;
        protected Utils.Vector2 center;
		protected Utils.Vector2 velocity;
		protected Utils.Vector2 speed;
        protected Utils.Vector3 color;
        protected EntityType type;
		protected TextureManager.TextureType texture;
        protected Mesh mesh;
        private bool moved;
        private float previousAngle;
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
	        width = 1.0f;
	        height = 1.0f;
	        angle = 0.0f;
            moved = false;
            previousAngle = 0.0f;
            //Initially opaque.
            transparency = DEFAULT_COLOR_T;

            aabb = new AABB();
            aabb.SetSize(width, height);
	        windowPos = new Utils.Vector2(0.0f, 0.0f);
	        worldPos = new Utils.Vector2(0.0f, 0.0f);
            velocity = new Utils.Vector2(0.0f, 0.0f);
            speed = new Utils.Vector2(0.0f, 0.0f);
            center = new Utils.Vector2(0.0f, 0.0f);
            previousPos = new Utils.Vector2(0.0f, 0.0f);
            //Initially no change on color.
            color = new Utils.Vector3(DEFAULT_COLOR_R, DEFAULT_COLOR_G, DEFAULT_COLOR_B);

	        //Default drawing vector is worldPos.
	        drawingVec = worldPos;
	        velocity.x = 0.0f;
	        velocity.y = 0.0f;
	        speed.x = 1.0f;
	        speed.y = 1.0f;
	    }

	    ~Entity()
	    {

	    }

	    public virtual void Update()
	    {
	        Camera cam = context.GetCamera();
            if (drawingVec == windowPos)
            {
                Utils.Vector2 transform = cam.WindowToWorld(windowPos);
                worldPos.x = transform.x;
                worldPos.y = transform.y;
            }
            else
            {
                Utils.Vector2 transform = cam.WorldToWindow(worldPos);
                windowPos.x = transform.x;
                windowPos.y = transform.y;
            }

            float deltaAngle = angle - previousAngle;

            if (deltaAngle != 0.0f)
            {
                aabb.Rotate(deltaAngle);
                previousAngle = angle;
            }

            float deltaX = drawingVec.x - previousPos.x;
            float deltaY = drawingVec.y - previousPos.y;

            if (deltaX != 0.0f || deltaY != 0.0f)
            {
                aabb.Translate(deltaX, deltaY);
                previousPos.x = drawingVec.x;
                previousPos.y = drawingVec.y;
                moved = true;
            }
            else
                moved = false;

	        center.x = drawingVec.x + (width / 2.0f);
	        center.y = drawingVec.y + (height / 2.0f);
	        //OpenGL uses degress, standard math uses radians.
	        velocity.x = (float)Math.Cos(Utils.DegToRad(angle)) * speed.x;
	        velocity.y = (float)Math.Sin(Utils.DegToRad(angle)) * speed.y;
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

        public bool Moved()
        {
            return moved;
        }

        public float GetWidth()
        {
            return width;
        }

        public float GetHeight()
        {
            return height;
        }

        public float GetTransparency()
        {
            return transparency;
        }

        public TextureManager.TextureType GetTexture()
        {
            return texture;
        }

        public Utils.Vector3 GetColor()
        {
            return color;
        }

        public EntityType GetEntityType()
        {
            return type;
        }

        public virtual void SetWidth(float w)
        {
            width = w;
            aabb.SetSize(width, height);
        }

        public virtual void SetHeight(float h)
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

		public void SetWindowAsDrawingVec(bool window)
		{
			if (window)
				drawingVec = windowPos;
			else
				drawingVec = worldPos;
		}

		public virtual bool Intersects(float x, float y)
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
            Simulator.lineSquare.MakeCurrent();
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
	        Gl.glTranslatef(center.x, center.y, Utils.DISCARD_Z_POS);
	        Gl.glRotatef(angle, 0.0f, 0.0f, 1.0f);
	        Gl.glTranslatef(-center.x, -center.y, -Utils.DISCARD_Z_POS);
	    }
	}
}
