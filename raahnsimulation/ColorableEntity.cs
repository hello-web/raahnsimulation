namespace RaahnSimulation
{
    //Colorable entities are entities that allow
    //the user to change all aspects of the color of
    //an entity. Both RGB and transparency. Some
    //entities which are colorable entities still
    //allow the user to change their colors, but
    //not transparency.

    public class ColorableEntity : Entity
    {
        private bool modified;

        public ColorableEntity()
        {

        }

        public ColorableEntity(Simulator sim) : base(sim)
        {
            modified = false;
        }

        public void SetColor(double r, double g, double b, double t)
        {
            color.x = r;
            color.y = g;
            color.z = b;
            transparency = t;

            if (color.x != DEFAULT_COLOR_R || color.y != DEFAULT_COLOR_G
            || color.z != DEFAULT_COLOR_B || transparency != DEFAULT_COLOR_T)
                modified = true;
            else
                modified = false;
        }

        public bool Modified()
        {
            return modified;
        }
    }
}

