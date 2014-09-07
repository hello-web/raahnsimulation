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
        public Utils.AABB aabb;
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

            aabb = new Utils.AABB();
            aabb.UpdateSize(width, height);
	        windowPos = new Utils.Vector2(0.0f, 0.0f);
	        worldPos = new Utils.Vector2(0.0f, 0.0f);
            velocity = new Utils.Vector2(0.0f, 0.0f);
            speed = new Utils.Vector2(0.0f, 0.0f);
            center = new Utils.Vector2(0.0f, 0.0f);

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

            aabb.Rotate(Utils.DegToRad(angle));
            aabb.Translate(worldPos.x, worldPos.y);
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

	    protected void RotateAroundCenter()
	    {
	        Gl.glTranslatef(center.x, center.y, Utils.DISCARD_Z_POS);
	        Gl.glRotatef(angle, 0.0f, 0.0f, 1.0f);
	        Gl.glTranslatef(-center.x, -center.y, -Utils.DISCARD_Z_POS);
	    }
	}
}
