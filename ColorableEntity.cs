namespace RaahnSimulation
{
    /* Colorable entities are entities that allow
    the user to change all aspects of the color of
    an entity. Both RGB and transparency. Some
    entities which are colorable entities still
    allow the user to change their colors, but
    not transparency.*/
    public class ColorableEntity : Entity
    {
        public ColorableEntity()
        {

        }

        public ColorableEntity(Simulator sim) : base(sim)
        {

        }

        public void SetColor(float r, float g, float b, float t)
        {
            color.x = r;
            color.y = g;
            color.z = b;
            transparency = t;
        }
    }
}

