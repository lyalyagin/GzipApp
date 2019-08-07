using GZipTest.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;

namespace GZipTest
{
	public abstract class GZipArchiver
	{
		protected readonly int _blockSize;
		protected readonly int _safeElementsCounts;
		protected const int DEFAULT_BLOCK_SIZE = 1000000;
		protected const int DEFAULT_SAFE_ELEMENTCOUNT_SIZE = 1000;
		protected bool _isReadFinished = false;
		protected bool _isProcessed = false;
		protected readonly int _threadCount;
		protected ManualResetEvent[] doneEvents;
		protected CustomConcurentQueue _inputBlocks;
		protected CustomConcurentQueue _outputBlocks;
		protected string _inputFilePath;
		protected string _outputFilePath;

		protected GZipArchiver(string inputFilePath, string outputFilePath)
		{
			if (int.TryParse(ConfigurationManager.AppSettings["bufferSize"], out int blockSize))
			{
				_blockSize = blockSize;
			}
			else
			{
				_blockSize = DEFAULT_BLOCK_SIZE;
			}

			if (int.TryParse(ConfigurationManager.AppSettings["safeElementsCount"], out int safeElementsCount))
			{
				_safeElementsCounts = safeElementsCount;
			}
			else
			{
				_blockSize = DEFAULT_SAFE_ELEMENTCOUNT_SIZE;
			}

			_inputFilePath = inputFilePath;
			_outputFilePath = outputFilePath;
			_threadCount = Environment.ProcessorCount - 1; // one for read action

			_inputBlocks = new CustomConcurentQueue();
			_outputBlocks = new CustomConcurentQueue();

			doneEvents = new ManualResetEvent[_threadCount];
		}

		public virtual void Process()
		{
			Exception exeption = null;

			// read thread
			new Thread(() => ThreadHelper.SafeExecute(() => Read(_inputBlocks, _inputFilePath), out exeption)).Start();

			// write thread
			new Thread(new ParameterizedThreadStart((index) => ThreadHelper.SafeExecute(() => Write(_outputBlocks, _outputFilePath, index), out exeption))).Start(_threadCount - 1);

			for (int i = 0; i < _threadCount; i++)
			{
				doneEvents[i] = new ManualResetEvent(false);

				new Thread(new ParameterizedThreadStart((index) => ThreadHelper.SafeExecute(() =>
				{
					while (!_isReadFinished || _inputBlocks.Count > 0)
					{
						var block = _inputBlocks.GetNext();

						if (block.Equals(default(KeyValuePair<int, byte[]>)))
							continue;

						ProcessBlock(_outputBlocks, block);
					}

					doneEvents[(int)index].Set();

				}, out exeption))).Start(i);


				if (exeption != null)
					throw new Exception(exeption.Message);
			}

			WaitHandle.WaitAll(doneEvents);

			_isProcessed = true;

			if (exeption != null)
				throw new Exception(exeption.Message);
		}

		protected abstract void ProcessBlock(CustomConcurentQueue outputBlocks, KeyValuePair<int, byte[]> block);
		protected abstract void Read(CustomConcurentQueue inputBlocks, string inputFilePath);
		protected abstract void Write(CustomConcurentQueue outputBlocks, string outputFilePath, object i);
	}
}
