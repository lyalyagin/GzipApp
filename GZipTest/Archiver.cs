using GZipTest.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace GZipTest
{

	public class Archiver : IArchiver
	{
		private readonly int _blockSize;
		private const int DEFAULT_BLOCK_SIZE = 1000000;

		public Archiver()
		{
			if (int.TryParse(ConfigurationManager.AppSettings["bufferSize"], out int blockSize))
			{
				_blockSize = blockSize;
			}
			else
			{
				_blockSize = DEFAULT_BLOCK_SIZE;
			}
		}

		public void Compress(string inputFilePath, string outputFilePath)
		{	
			Process(inputFilePath, outputFilePath, Read, WriteCompressed, CompressBlock);
		}

		public void Decompress(string inputFilePath, string outputFilePath)
		{
			Process(inputFilePath, outputFilePath, ReadCompressed, Write, DecompressBlock);
		}

		private void Process(string inputFilePath, string outputFilePath,
			Action<CustomConcurentQueue, string> readAction, Action<CustomConcurentQueue, string> writeAction,
			Action<CustomConcurentQueue, KeyValuePair<int, byte[]>> action)
		{
			CustomConcurentQueue inputBlocks = new CustomConcurentQueue();
			CustomConcurentQueue outputBlocks = new CustomConcurentQueue();

			readAction(inputBlocks, inputFilePath);

			while (inputBlocks.Count() > 0)
			{
				Exception exeption = null;

				for (int i = 0; i < Environment.ProcessorCount; i++)
				{
					Thread thread = new Thread(() => ThreadHelper.SafeExecute(() =>
					{
						var block = inputBlocks.GetNext();

						if (block.Equals(default(KeyValuePair<int, byte[]>)))
							return;

						action(outputBlocks, block);
					}, out exeption));

					thread.Start();

					thread.Join();

					if (exeption != null)
						throw new Exception();
				}
			}

			writeAction(outputBlocks, outputFilePath);
		}

		private void CompressBlock(CustomConcurentQueue outputBlocks, KeyValuePair<int, byte[]> block)
		{
			using (MemoryStream _memoryStream = new MemoryStream())
			{
				using (GZipStream cs = new GZipStream(_memoryStream, CompressionMode.Compress))
				{
					cs.Write(block.Value, 0, block.Value.Length);
				}

				byte[] compressedData = _memoryStream.ToArray();
				outputBlocks.Add(block.Key, compressedData);
				Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
			}
		}

		private void DecompressBlock(CustomConcurentQueue outputBlocks, KeyValuePair<int, byte[]> block)
		{
			using (MemoryStream ms = new MemoryStream(block.Value))
			{
				using (GZipStream _gz = new GZipStream(ms, CompressionMode.Decompress))
				{
					// The last 4 bytes of the gz file contains the length
					byte[] originalLengthByteArray = new byte[4];
					Array.Copy(block.Value, block.Value.Length - 4, originalLengthByteArray, 0, 4);
					int length = BitConverter.ToInt32(originalLengthByteArray, 0);

					byte[] outByte = new byte[length];
					_gz.Read(outByte, 0, outByte.Length);
					byte[] decompressedData = outByte.ToArray();

					outputBlocks.Add(block.Key, decompressedData);
				}
			}

		}

		private void Read(CustomConcurentQueue inputBlocks, string inputFilePath)
		{
			using (FileStream _fileToBeCompressed = new FileStream(inputFilePath, FileMode.Open))
			{
				int bytesRead;
				byte[] lastBuffer;
				int blockIndex = 0;

				while (_fileToBeCompressed.Position < _fileToBeCompressed.Length)
				{
					if (_fileToBeCompressed.Length - _fileToBeCompressed.Position <= _blockSize)
					{
						bytesRead = (int)(_fileToBeCompressed.Length - _fileToBeCompressed.Position);
					}
					else
					{
						bytesRead = _blockSize;
					}

					lastBuffer = new byte[bytesRead];
					_fileToBeCompressed.Read(lastBuffer, 0, bytesRead);
					inputBlocks.Add(blockIndex, lastBuffer);
					blockIndex++;
				}
			}
		}

		private void ReadCompressed(CustomConcurentQueue inputBlocks, string inputFilePath)
		{
			int blockIndex = 0;

			using (FileStream _compressedFile = new FileStream(inputFilePath, FileMode.Open))
			{
				while (_compressedFile.Position < _compressedFile.Length)
				{
					byte[] lengthBuffer = new byte[8];
					_compressedFile.Read(lengthBuffer, 0, lengthBuffer.Length);
					int blockLength = BitConverter.ToInt32(lengthBuffer, 4);
					byte[] compressedData = new byte[blockLength];
					lengthBuffer.CopyTo(compressedData, 0);

					_compressedFile.Read(compressedData, 8, blockLength - 8);

					inputBlocks.Add(blockIndex, compressedData);
					blockIndex++;

				}
			}
		}

		private void Write(CustomConcurentQueue outputBlocks, string outputFilePath)
		{
			using (var writer = new BinaryWriter(File.Open(outputFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)))
			{
				while (outputBlocks.Count() > 0)
				{
					writer.BaseStream.Seek(0, SeekOrigin.End);
					var bytes = outputBlocks.GetNext();
					writer.Write(bytes.Value);
				}
			}
		}

		private void WriteCompressed(CustomConcurentQueue outputBlocks, string outputFilePath)
		{
			using (FileStream _fileCompressed = new FileStream(outputFilePath + ".gz", FileMode.Append))
			{
				while (outputBlocks.Count() > 0)
				{
					var block = outputBlocks.GetNext();

					BitConverter.GetBytes(block.Value.Length).CopyTo(block.Value, 4);
					_fileCompressed.Write(block.Value, 0, block.Value.Length);
				}
			}
		}
	}
}
