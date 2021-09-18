using System;
using System.Threading;
using System.Threading.Tasks;

namespace GravityTest
{
	static class ParallelRunner
	{
		private static int _index;
		private static int _count;
		private static Action<int> _task;
		private static readonly Task[] _tasks = new Task[ Environment.ProcessorCount ];
		private static SpinLock _locker = new SpinLock ();

		private static void threadTask ( object id )
		{
			while ( true )
			{
				bool gotLock = false;
				_locker.Enter ( ref gotLock );
				int idx = _index++;
				_locker.Exit ();

				if ( idx >= _count )
					return;
				_task ( idx );
			}
		}

		public static void RunInParallel ( Action<int> pt, int elementCount )
		{
			_task = pt;
			_count = elementCount;
			_index = 0;
			for ( int i = 0; i < _tasks.Length; i++ )
				_tasks[ i ] = Task.Factory.StartNew ( threadTask, i );

			Task.WaitAll ( _tasks, -1 );
		}

		public static void SyncDo ( ref SpinLock sl, Action action )
		{
			bool gotLock = false;
			try
			{
				sl.Enter ( ref gotLock );
				action ();
			}
			finally
			{
				if ( gotLock )
					sl.Exit ();
			}
		}

	}
}
