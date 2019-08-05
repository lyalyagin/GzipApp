using GZipTest.Helpers;
using System;

namespace GZipTest
{
	internal class Program
	{
		static int Main(string[] args)
		{
			args = new string[3];
			args[0] = @"decompress";
			args[2] = @"F:\Test2.txt";
			args[1] = @"F:\result.gz";

			int result = -1;

			ConsoleArgumentValidator.Validate(args);

			IArchiver archiver = new Archiver();

			try
			{
				switch (args[0].ToLower())
				{
					case "compress":
						archiver.Compress(args[1], args[2]);
						break;
					case "decompress":
						archiver.Decompress(args[1], args[2]);
						break;
				}

				result = 1;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error has been occurred: {ex.Message}");

				result = 0;
			}

			return result;
		}
	}
}
