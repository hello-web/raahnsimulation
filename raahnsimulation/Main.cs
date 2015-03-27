namespace RaahnSimulation
{
	static class Program
	{
		private static int Main(string[] argv)
		{
            Simulator sim = new Simulator();

		    if (argv.Length > 1)
		    {
				int strValue = 0;
				int.TryParse(argv[1], out strValue);
		        if (strValue == 1)
		            sim.SetHeadLess(true);
		    }

		    return sim.Execute();
		}
	}
}