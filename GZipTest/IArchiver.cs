namespace GZipTest
{
	public interface IArchiver
	{
		void Compress(string inputFilePath, string outputFilePath);
		void Decompress(string inputFilePath, string outputFilePath);
	}
}
