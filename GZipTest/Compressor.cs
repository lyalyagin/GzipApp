using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest
{
	public class Compressor : GZipArchiver
	{
		public Compressor(string inputFilePath, string outputFilePath) : base(inputFilePath, outputFilePath)
		{ }

		protected override void ProcessBlock(CustomConcurentQueue outputBlocks, KeyValuePair<int, byte[]> block)
		{
			using (MemoryStream _memoryStream = new MemoryStream())
			{
				using (GZipStream cs = new GZipStream(_memoryStream, CompressionMode.Compress))
				{
					cs.Write(block.Value, 0, block.Value.Length);
				}

				byte[] compressedData = _memoryStream.ToArray();
				outputBlocks.Add(block.Key, compressedData);
			}
		}

		protected override void Read(CustomConcurentQueue inputBlocks, string inputFilePath)
		{
			using (FileStream _fileForCompressing = new FileStream(inputFilePath, FileMode.Open))
			{
				int bytesRead;
				byte[] lastBuffer;
				int blockIndex = 0;

				while (_fileForCompressing.Position < _fileForCompressing.Length)
				{
					if (inputBlocks.Count > _safeElementsCounts)
						continue;

					if (_fileForCompressing.Length - _fileForCompressing.Position <= _blockSize)
					{
						bytesRead = (int)(_fileForCompressing.Length - _fileForCompressing.Position);
					}
					else
					{
						bytesRead = _blockSize;
					}

					lastBuffer = new byte[bytesRead];
					_fileForCompressing.Read(lastBuffer, 0, bytesRead);
					inputBlocks.Add(blockIndex, lastBuffer);
					blockIndex++;
				}
			}

			_isReadFinished = true;
		}

		protected override void Write(CustomConcurentQueue outputBlocks, string outputFilePath, object i)
		{
			int lastBlockIndex = 0;

			doneEvents[(int)i] = new ManualResetEvent(false);

			using (FileStream _fileCompressed = new FileStream(outputFilePath + ".gz", FileMode.Append))
			{
				while (!_isProcessed || outputBlocks.Count > 0)
				{
					if (outputBlocks.Count == 0)
						continue;

					var block = outputBlocks.GetByIndex(lastBlockIndex);

					if (block.Equals(default(KeyValuePair<int, byte[]>)))
						continue;

					BitConverter.GetBytes(block.Value.Length).CopyTo(block.Value, 4);
					_fileCompressed.Write(block.Value, 0, block.Value.Length);
					Interlocked.Increment(ref lastBlockIndex);
				}
			}

			doneEvents[(int)i].Set();
		}
	}
}
