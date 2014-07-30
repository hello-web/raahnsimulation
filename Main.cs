namespace RaahnSimulation
{
	static class Program
	{
		static void Main(string[] argv)
		{
		    Simulator sim = Simulator.Instance();
		    if (argv.Length > 1)
		    {
				int strValue = 0;
				int.TryParse(argv[1], out strValue);
		        if (strValue == 1)
		            sim.SetHeadLess(true);
		    }
		    sim.Execute();
		}
	}
}