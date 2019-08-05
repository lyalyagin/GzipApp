using System;
using System.Linq;

namespace GZipTest.Helpers
{
	public static class ConsoleArgumentValidator
	{
		private const int ARGS_MAX_COUNT = 3;
		internal static void Validate(string[] args)
		{
			if (args == null || !args.Any())
				throw new ArgumentException("Please provide arguments");

			if (args.Length > ARGS_MAX_COUNT)
				throw new ArgumentException("To many arguments");

			if (args[0].ToLower() != "compress" && args[0].ToLower() != "decompress")
				throw new ArgumentException("Please provide action");

			if (string.IsNullOrEmpty(args[1]))
				throw new ArgumentException("Please provide source file path.");

			if (string.IsNullOrEmpty(args[2]))
				throw new Exception("Please provide destination file path.");
		}
	}
}
