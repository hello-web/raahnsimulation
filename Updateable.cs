using SFML.Window;

namespace RaahnSimulation
{
    public interface Updateable
    {
        void Update();
        void UpdateEvent(Event e);
    }
}

