using System;

namespace RaahnSimulation
{
	public abstract class ClickableEntity : Entity
	{
		public delegate void OnClickType(Simulator sim);

		protected bool hasListener;
		protected bool pressed;
		protected bool hovering;
		protected bool clicked;
		private OnClickType OnClick;

		protected ClickableEntity() {}

	    protected ClickableEntity(Simulator sim) : base(sim)
	    {
	        pressed = false;
	        hovering = false;
	        clicked = false;
	        hasListener = false;
	    }

        public override void Update()
        {
            base.Update();
            //Clicked should only be used by other
            //objects when updating with events.
            clicked = false;
        }

	    public override void UpdateEvent(Event e)
	    {
            base.UpdateEvent(e);

            double x;
            double y;

            if (e.type == Gdk.EventType.MotionNotify)
            {
                x = (double)e.X;
                y = (double)context.GetWindowHeight() - (double)e.Y;
            }
            else if (e.type == Gdk.EventType.ButtonPress)
            {
                x = (double)e.X;
                y = (double)context.GetWindowHeight() - (double)e.Y;
            }
            else
                return;

            Utils.Vector2 comparisonVec;
            Utils.Vector2 mousePosWindowd = new Utils.Vector2(x, y);
            comparisonVec = context.GetCamera().ProjectWindow(mousePosWindowd);

            if (UsesTransform())
                comparisonVec = context.GetCamera().TransformWorld(comparisonVec);

	        if (aabb.Intersects(comparisonVec.x, comparisonVec.y))
	        {
	            hovering = true;

	            if (e.type == Gdk.EventType.ButtonPress)
	            {
                    if (e.button == Utils.GTK_BUTTON_LEFT && !pressed)
                    {
                        if (!pressed)
                        {
                            clicked = true;
                            if (hasListener)
                                OnClick(context);
                        }
                        else
                            pressed = true;
                    }
	            }
	        }
	        else
	        {
	            pressed = false;
	            hovering = false;
	        }

            if (e.type == Gdk.EventType.ButtonRelease)
            {
                if (e.button == Utils.GTK_BUTTON_LEFT)
                    pressed = false;
            }
	    }

	    public void SetOnClickListener(OnClickType listener)
	    {
	        OnClick = listener;
	        hasListener = true;
	    }
	}
}
