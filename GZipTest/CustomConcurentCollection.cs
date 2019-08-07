using System;
using System.Collections.Generic;
using System.Linq;

namespace GZipTest
{
	public class CustomConcurentQueue
	{
		private readonly SortedDictionary<int, byte[]> _blocks;
		private readonly object _lock = new object();

		public int Count
		{
			get
			{
				lock (_lock)
				{
					return _blocks.Count;
				}
			}
		}

		public KeyValuePair<int, byte[]> GetByIndex(int index)
		{
			lock (_lock)
			{
				var block = _blocks.FirstOrDefault(b => b.Key == index);

				_blocks.Remove(block.Key);

				return block;
			}
			
		}

		public CustomConcurentQueue()
		{
			_blocks = new SortedDictionary<int, byte[]>();
		}

		public void Add(int index, byte[] bytes)
		{
			lock (_lock)
			{
				if (_blocks.ContainsKey(index))
					throw new ArgumentException($"Block with index: {index} already exists");

				_blocks.Add(index, bytes);
			}
		}

		public KeyValuePair<int, byte[]> GetNext()
		{
			lock (_lock)
			{
				var block = _blocks.FirstOrDefault();

				_blocks.Remove(block.Key);

				return block;
			}
		}
	}
}
