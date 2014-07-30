using System;
using Tao.OpenGl;
using SFML.Window;

namespace RaahnSimulation
{
	public abstract class Entity
	{
		public const float ENTITY_ROTATE_SPEED = 90.0f;

		public float width;
		public float height;
		public float angle;
		public Utils.Vector2 worldPos;
		public Utils.Vector2 windowPos;
		public Utils.Vector2 drawingVec;
        public Utils.Rect bounds;
		protected Simulator context;
		protected Utils.Vector2 velocity;
		protected Utils.Vector2 speed;
		protected TextureManager.TextureType texture;
		private Utils.Vector2 center;

		protected Entity()
		{

		}

	    protected Entity(Simulator sim)
	    {
	        texture = TextureManager.TextureType.NONE;
	        context = sim;
	        width = 1.0f;
	        height = 1.0f;
	        angle = 0.0f;
	        windowPos = new Utils.Vector2(0.0f, 0.0f);
	        worldPos = new Utils.Vector2(0.0f, 0.0f);
            velocity = new Utils.Vector2(0.0f, 0.0f);
            speed = new Utils.Vector2(0.0f, 0.0f);
            center = new Utils.Vector2(0.0f, 0.0f);
            bounds = new Utils.Rect();
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

	    public virtual void Update(Nullable<Event> nEvent)
	    {
	        Camera cam = context.GetCamera();
	        if (ReferenceEquals(drawingVec, windowPos))
	            Entity.WindowToWorld(windowPos, worldPos, cam);
	        else
	            Entity.WorldToWindow(worldPos, windowPos, cam);

	        bounds.bottom = worldPos.y;
	        bounds.top = worldPos.y + height;
	        bounds.left = worldPos.x;
	        bounds.right = worldPos.x + width;
	        center.x = drawingVec.x + (width / 2.0f);
	        center.y = drawingVec.y + (height / 2.0f);
	        //OpenGL uses degress, standard math uses radians.
	        velocity.x = (float)Math.Cos(Utils.DegToRad(angle)) * speed.x;
	        velocity.y = (float)Math.Sin(Utils.DegToRad(angle)) * speed.y;
	    }

	    public virtual void Draw()
	    {
	        if (context.GetTexMan().GetCurrentTexture() != texture)
	            context.GetTexMan().SetTexture(texture);
	    }

		public static void WorldToWindow(Utils.Vector2 world, Utils.Vector2 pos, Camera cam)
		{
			Utils.Vector2 camPos = cam.GetPosition();
            pos.x = world.x - camPos.x;
            pos.y = world.y - camPos.y;
		}

		public static void WindowToWorld(Utils.Vector2 window, Utils.Vector2 pos, Camera cam)
		{
			Utils.Vector2 camPos = cam.GetPosition();
            pos.x = window.x + camPos.x;
            pos.y = window.y + camPos.y;
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
			if (x > bounds.left && x < bounds.right)
			{
				if (y > bounds.bottom && y < bounds.top)
					return true;
				else
					return false;
			}
			else
				return false;
		}

		public virtual bool Intersects(Utils.Rect r)
		{
			if (r.left > bounds.right || r.right < bounds.left || r.bottom > bounds.top || r.top < bounds.bottom)
				return false;
			else
				return true;
		}

	    protected void RotateAroundCenter()
	    {
	        Gl.glTranslatef(center.x, center.y, Utils.DISCARD_Z_POS);
	        Gl.glRotatef(angle, 0.0f, 0.0f, 1.0f);
	        Gl.glTranslatef(-center.x, -center.y, -Utils.DISCARD_Z_POS);
	    }
	}
}
