namespace RaahnSimulation
{
    public class TextPool : EntityPool<Text>
    {
        public TextPool(Simulator sim, int size = DEFAULT_SIZE) : base(sim, size)
        {
            Text text;
            for (uint i = 0; i < size; i++)
            {
                text = new Text(sim, "");
                elements.Add(text);
            }
        }
    }
}

