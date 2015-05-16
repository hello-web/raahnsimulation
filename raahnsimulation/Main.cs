namespace RaahnSimulation
{
	public static class Program
	{
		private static int Main(string[] argv)
		{
            Simulator sim = new Simulator();
		    return sim.Execute(argv);
		}
	}
}