using System;
using SFML.System;
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

	    public override void Update(Nullable<Event> nEvent)
	    {
	        base.Update(nEvent);

            clicked = false;

	        if (nEvent == null)
	            return;

	        Vector2i mousePosWindowi = Mouse.GetPosition(context.GetWindow());

	        Utils.Vector2 mousePosWindowf = new Utils.Vector2((float)mousePosWindowi.X, (float)(context.GetWindowHeight()) - (float)mousePosWindowi.Y);
            Utils.Vector2 mousePosWorldf = new Utils.Vector2(0.0f, 0.0f);
            Entity.WindowToWorld(mousePosWindowf, mousePosWorldf, context.GetCamera());

	        if (Intersects(mousePosWorldf.x, mousePosWorldf.y))
	        {
	            hovering = true;

	            if (nEvent.Value.Type == EventType.MouseButtonPressed && nEvent.Value.MouseButton.Button == Mouse.Button.Left && !pressed)
	            {
	                clicked = true;
	                if (hasListener)
	                    OnClick(context);
	            }

	            if (nEvent.Value.Type == EventType.MouseButtonPressed && nEvent.Value.MouseButton.Button == Mouse.Button.Left)
	                pressed = true;
	        }
	        else
	        {
	            pressed = false;
	            hovering = false;
	        }
	        /*If the state is popped before this
	        like in OptionState, then an invalid
	        write of size 1 will occur, fix this
	        with a state request function which
	        changes the state after all updating.*/
            if (nEvent.Value.Type == EventType.MouseButtonReleased && nEvent.Value.MouseButton.Button == Mouse.Button.Left)
	            pressed = false;
	    }

	    public void SetOnClickListener(OnClickType listener)
	    {
	        OnClick = listener;
	        hasListener = true;
	    }
	}
}
