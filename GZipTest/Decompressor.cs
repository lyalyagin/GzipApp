using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace GZipTest
{
	public class Decompressor : GZipArchiver
	{
		public Decompressor(string inputFilePath, string outputFilePath) : base(inputFilePath, outputFilePath)
		{ }

		protected override void ProcessBlock(CustomConcurentQueue outputBlocks, KeyValuePair<int, byte[]> block)
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

		protected override void Read(CustomConcurentQueue inputBlocks, string inputFilePath)
		{
			int blockIndex = 0;

			using (FileStream _compressedFile = new FileStream(inputFilePath, FileMode.Open))
			{
				while (_compressedFile.Position < _compressedFile.Length)
				{
					if (inputBlocks.Count > _safeElementsCounts)
						continue;

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

			_isReadFinished = true;
		}

		protected override void Write(CustomConcurentQueue outputBlocks, string outputFilePath, object i)
		{
			int lastBlockIndex = 0;

			doneEvents[(int)i] = new ManualResetEvent(false);

			using (var writer = new BinaryWriter(File.Open(outputFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)))
			{
				while (outputBlocks.Count > 0 || !_isProcessed)
				{
					if (outputBlocks.Count == 0)
						continue;

					writer.BaseStream.Seek(0, SeekOrigin.End);
					var block = outputBlocks.GetByIndex(lastBlockIndex);

					if (block.Equals(default(KeyValuePair<int, byte[]>)))
						continue;

					writer.Write(block.Value);
					Interlocked.Increment(ref lastBlockIndex);
				}
			}

			doneEvents[(int)i].Set();
		}
	}
}
