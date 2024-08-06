
namespace MegapixelHelios
{
	public static class MegapixelHeliosDebug
	{
		public static uint Trace { get; private set; }
		public static uint Notice { get; private set; }
		public static uint Verbose { get; private set; }

		public static void ResetDebugLevels()
		{
			Trace = 0;
			Notice = 1;
			Verbose = 2;
		}

		public static void SetDebugLevels(uint level)
		{
			Trace = level;
			Notice = level;
			Verbose = level;
		}
	}
}