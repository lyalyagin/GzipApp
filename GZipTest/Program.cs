using GZipTest.Helpers;
using System;

namespace GZipTest
{
	internal class Program
	{
		static int Main(string[] args)
		{
			int result = -1;

			ConsoleArgumentValidator.Validate(args);

			GZipArchiver archiver;

			try
			{
				switch (args[0].ToLower())
				{
					case "compress":
						archiver = new Compressor(args[1], args[2]);
						break;
					case "decompress":
						archiver = new Decompressor(args[1], args[2]);
						break;
					default: throw new ArgumentException("Action has not been provided");
				}

				var watch = System.Diagnostics.Stopwatch.StartNew();
				archiver.Process();
				watch.Stop();
				TimeSpan ts = watch.Elapsed;

				result = 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error has been occurred: {ex.Message}");

				result = 1;
			}

			return result;
		}
	}
}
