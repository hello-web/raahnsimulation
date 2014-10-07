using System;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public abstract class Entity : Updateable
	{
		public const float ROTATE_SPEED = 90.0f;

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
		protected Utils.Vector2 velocity;
		protected Utils.Vector2 speed;
        protected Utils.Vector3 color;
		protected TextureManager.TextureType texture;
        private float previousAngle;
		private Utils.Vector2 center;
        private Utils.Vector2 previousPos;

		protected Entity()
		{

		}

	    protected Entity(Simulator sim)
	    {
            visible = true;

	        texture = TextureManager.TextureType.NONE;
	        context = sim;
	        width = 1.0f;
	        height = 1.0f;
	        angle = 0.0f;
            previousAngle = 0.0f;
            //Initially opaque.
            transparency = 1.0f;

            aabb = new AABB();
            aabb.SetSize(width, height);
	        windowPos = new Utils.Vector2(0.0f, 0.0f);
	        worldPos = new Utils.Vector2(0.0f, 0.0f);
            velocity = new Utils.Vector2(0.0f, 0.0f);
            speed = new Utils.Vector2(0.0f, 0.0f);
            center = new Utils.Vector2(0.0f, 0.0f);
            previousPos = new Utils.Vector2(0.0f, 0.0f);
            //Initially no change on color.
            color = new Utils.Vector3(1.0f, 1.0f, 1.0f);

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
                Utils.Vector2 transform = Entity.WindowToWorld(windowPos, cam);
                worldPos.x = transform.x;
                worldPos.y = transform.y;
            }
            else
            {
                Utils.Vector2 transform = Entity.WorldToWindow(worldPos, cam);
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
            }

	        center.x = drawingVec.x + (width / 2.0f);
	        center.y = drawingVec.y + (height / 2.0f);
	        //OpenGL uses degress, standard math uses radians.
	        velocity.x = (float)Math.Cos(Utils.DegToRad(angle)) * speed.x;
	        velocity.y = (float)Math.Sin(Utils.DegToRad(angle)) * speed.y;
	    }

        public virtual void UpdateEvent(Event e)
        {
            Update();
        }

	    public virtual void Draw()
	    {
	        if (context.GetTexMan().GetCurrentTexture() != texture)
	            context.GetTexMan().SetTexture(texture);
	    }

        public static Utils.Vector2 WorldToWindow(float worldX, float worldY, Camera cam)
        {
            Utils.Vector2 camPos = cam.GetPosition();
            return new Utils.Vector2(worldX - camPos.x, worldY - camPos.y);
        }

        public static Utils.Vector2 WindowToWorld(float windowX, float windowY, Camera cam)
        {
            Utils.Vector2 camPos = cam.GetPosition();
            return new Utils.Vector2(windowX + camPos.x, windowY + camPos.y);
        }

		public static Utils.Vector2 WorldToWindow(Utils.Vector2 world, Camera cam)
		{
			Utils.Vector2 camPos = cam.GetPosition();
            return new Utils.Vector2(world.x - camPos.x, world.y - camPos.y);
		}

		public static Utils.Vector2 WindowToWorld(Utils.Vector2 window, Camera cam)
		{
			Utils.Vector2 camPos = cam.GetPosition();
            return new Utils.Vector2(window.x + camPos.x, window.y + camPos.y);
		}

        //Only transforms bounding properties.
        public static Utils.Rect WorldToWindow(Utils.Rect world, Camera cam)
        {
            Utils.Vector2 camPos = cam.GetPosition();
            Utils.Rect transform = new Utils.Rect();

            transform.left = world.left - camPos.x;
            transform.right = world.right - camPos.x;
            transform.bottom = world.bottom - camPos.y;
            transform.top = world.top - camPos.y;

            return transform;
        }

        //Only transforms bounding properties.
        public static Utils.Rect WindowToWorld(Utils.Rect window, Camera cam)
        {
            Utils.Vector2 camPos = cam.GetPosition();
            Utils.Rect transform = new Utils.Rect();

            transform.left = window.left + camPos.x;
            transform.right = window.right + camPos.x;
            transform.bottom = window.bottom + camPos.y;
            transform.top = window.top + camPos.y;

            return transform;
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

        public Utils.Vector3 GetColor()
        {
            return color;
        }

        public void SetWidth(float w)
        {
            width = w;
            aabb.SetSize(width, height);
        }

        public void SetHeight(float h)
        {
            height = h;
            aabb.SetSize(width, height);
        }

		public void SetTexture(TextureManager.TextureType t)
		{
			texture = t;
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
			if (x > aabb.GetBounds().left && x < aabb.GetBounds().right)
			{
				if (y > aabb.GetBounds().bottom && y < aabb.GetBounds().top)
					return true;
				else
					return false;
			}
			else
				return false;
		}

		public virtual bool Intersects(Utils.Rect r)
		{
			if (r.left > aabb.GetBounds().right || r.right < aabb.GetBounds().left || r.bottom > aabb.GetBounds().top || r.top < aabb.GetBounds().bottom)
				return false;
			else
				return true;
		}

        public virtual void DebugDraw()
        {
            //Handle all generic debug drawing here.
            DrawAABB();
        }

	    protected void RotateAroundCenter()
	    {
	        Gl.glTranslatef(center.x, center.y, Utils.DISCARD_Z_POS);
	        Gl.glRotatef(angle, 0.0f, 0.0f, 1.0f);
	        Gl.glTranslatef(-center.x, -center.y, -Utils.DISCARD_Z_POS);
	    }

        private void DrawAABB()
        {
            Utils.Rect bounds = aabb.GetBounds();

            Gl.glTranslatef(bounds.left, bounds.bottom, Utils.DISCARD_Z_POS);
            Gl.glScalef(bounds.width, bounds.height, Utils.DISCARD_Z_SCALE);

            Gl.glDrawElements(Gl.GL_TRIANGLES, Utils.INDEX_COUNT, Gl.GL_UNSIGNED_SHORT, IntPtr.Zero);
        }
	}
}
