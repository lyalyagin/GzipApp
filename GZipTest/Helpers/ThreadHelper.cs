using System;
using System.Threading;

namespace GZipTest.Helpers
{
	public static class ThreadHelper
	{
		public static void SafeExecute(Action action, out Exception exception)
		{
			exception = null;

			try
			{
				action.Invoke();
			}
			catch (Exception ex)
			{
				exception = ex;
			}
		}
	}
}
