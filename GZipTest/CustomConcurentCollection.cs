using System;
using System.Collections.Generic;
using System.Linq;

namespace GZipTest
{
	public class CustomConcurentQueue
	{
		private readonly SortedDictionary<int, byte[]> _blocks;
		private readonly object _lock = new object();

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
				var blockKey = _blocks.Keys.FirstOrDefault();

				var block = _blocks.FirstOrDefault(b =>b.Key == blockKey);

				_blocks.Remove(blockKey);

				return block;
			}
		}

		public int Count()
		{
			lock (_lock)
			{
				return _blocks.Count;
			}
		}

		public void Clear()
		{
			lock (_lock)
			{
				_blocks.Clear();
			}
		}
	}
}
