using System;
using SFML.Window;

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
            if (e.Type == EventType.MouseMoved)
            {
                x = (double)e.MouseMove.X;
                y = (double)context.GetWindowHeight() - (double)e.MouseMove.Y;
            }
            else if (e.Type == EventType.MouseButtonPressed)
            {
                x = (double)e.MouseButton.X;
                y = (double)context.GetWindowHeight() - (double)e.MouseButton.Y;
            }
            else
                return;

            Utils.Vector2 comparisonVec;
            Utils.Vector2 mousePosWindowd = new Utils.Vector2(x, y);
            comparisonVec = context.GetCamera().ProjectWindow(mousePosWindowd);

            if (drawingVec == transformedWorldPos)
                comparisonVec = context.GetCamera().TransformWorld(comparisonVec);

	        if (Intersects(comparisonVec.x, comparisonVec.y))
	        {
	            hovering = true;

	            if (e.Type == EventType.MouseButtonPressed && e.MouseButton.Button == Mouse.Button.Left && !pressed)
	            {
	                clicked = true;
	                if (hasListener)
	                    OnClick(context);
	            }

	            if (e.Type == EventType.MouseButtonPressed && e.MouseButton.Button == Mouse.Button.Left)
	                pressed = true;
	        }
	        else
	        {
	            pressed = false;
	            hovering = false;
	        }

            if (e.Type == EventType.MouseButtonReleased && e.MouseButton.Button == Mouse.Button.Left)
	            pressed = false;
	    }

	    public void SetOnClickListener(OnClickType listener)
	    {
	        OnClick = listener;
	        hasListener = true;
	    }
	}
}
