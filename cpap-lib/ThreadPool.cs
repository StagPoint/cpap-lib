// Copyright 2017 StagPoint Software
namespace StagPoint.Threading
{
	using System;
	using System.Threading;
	using System.Collections.Generic;

	/// <summary>
	/// Simplistic implementation of a thread pool that does not allocate memory internally for each task (unlike ThreadPool.QueueUserWorkItem)
	/// </summary>
	public class CustomThreadPool
	{
		#region Public properties 

		public Exception LastException { get; private set; }

		#endregion

		#region Private fields

		private int _numberOfThreads = 0;
		private BackgroundThread[] _threads = null;
		private int _nextThread = 0;

		#endregion

		#region Constructor

		public CustomThreadPool( int numberOfThreads )
		{
			_numberOfThreads = numberOfThreads;
			_threads = new BackgroundThread[ numberOfThreads ];

			for( int i = 0; i < numberOfThreads; i++ )
			{
				_threads[ i ] = new BackgroundThread();
			}
		}

		#endregion

		#region Public functions 

		public void ClearException()
		{
			LastException = null;
		}

		public void ExecutePendingTasks()
		{
			for( int i = 0; i < _numberOfThreads; i++ )
			{
				_threads[ i ].BeginExecuting();
			}

			for( int i = 0; i < _numberOfThreads; i++ )
			{
				_threads[ i ].WaitForCompletion();

				if( _threads[ i ].LastException != null )
				{
					LastException = _threads[ i ].LastException;
				}
			}

			if( LastException != null )
			{
				throw LastException;
			}
		}

		public void ExecutePendingTasks( int timeout )
		{
			for( int i = 0; i < _numberOfThreads; i++ )
			{
				_threads[ i ].BeginExecuting();
			}

			for( int i = 0; i < _numberOfThreads; i++ )
			{
				_threads[ i ].WaitForCompletion( timeout );

				if( _threads[ i ].LastException != null )
				{
					LastException = _threads[ i ].LastException;
				}
			}

			if( LastException != null )
			{
				throw LastException;
			}
		}

		public void AddTask( Action task )
		{
			if( _numberOfThreads == 0 )
			{
				throw new InvalidOperationException( "No background threads available" );
			}

			_threads[ _nextThread ].AddTask( task );

			_nextThread = ( _nextThread + 1 ) % _numberOfThreads;
		}

		public void Shutdown()
		{
			for( int i = 0; i < _numberOfThreads; i++ )
			{
				_threads[ i ].Shutdown();
				_threads[ i ] = null;
			}

			_threads = null;
			_numberOfThreads = 0;
		}

		#endregion
	}

	public class BackgroundThread
	{
		#region Public properties

		public bool IsRunning
		{
			get { return _isRunning; }
		}

		public Exception LastException { get; private set; }

		#endregion

		#region Private fields

		private Thread _thread;
		private ManualResetEvent _workDone;
		private ManualResetEvent _workAvailable;
		private ManualResetEvent _shutdownHandle;
		private Queue<Action> _tasks = null;
		private bool _isRunning = false;
		private bool _isProcessing = false;
		private object _lockObject = new object();

		#endregion

		#region Constructor

		public BackgroundThread()
		{
			_tasks = new Queue<Action>( 64 );

			_thread = new Thread( new ThreadStart( this.processJobs ) )
			{
				IsBackground = true,
				Priority = ThreadPriority.Normal,
				Name = "CustomThreadPool Thread"
			};

			_workAvailable = new ManualResetEvent( false );
			_workDone = new ManualResetEvent( false );
			_shutdownHandle = new ManualResetEvent( false );
			_thread.Start();
			_isRunning = true;
		}

		#endregion

		#region Public functions

		public void AddTask( Action task )
		{
			if( task == null )
			{
				throw new ArgumentNullException( "Cannot add a NULL task" );
			}

			lock( _lockObject )
			{
				_tasks.Enqueue( task );
			}
		}

		public void BeginExecuting()
		{
			lock( _lockObject )
			{
				_workAvailable.Set();
				_workDone.Reset();
				_isProcessing = true;
			}
		}

		public void WaitForCompletion()
		{
			_workDone.WaitOne();
		}

		public void WaitForCompletion( int timeout )
		{
			_workDone.WaitOne( timeout );
		}

		public void ClearException()
		{
			lock( _lockObject )
			{
				this.LastException = null;
			}
		}

		public void Shutdown()
		{
			lock( _lockObject )
			{
				if( !_isRunning )
					return;

				_shutdownHandle.Reset();

				_isRunning = false;
				_tasks.Clear();
				_workAvailable.Set();

				_isProcessing = false;
			}

			_shutdownHandle.WaitOne( 1000 );
			_shutdownHandle = null;

			_thread = null;
			_workAvailable = null;
		}

		#endregion

		#region Private functions

		private void processJobs()
		{
			try
			{
				while( true )
				{
					_workAvailable.WaitOne( 100 );

					lock( _lockObject )
					{
						if( !_isRunning )
							break;

						if( !_isProcessing )
						{
							continue;
						}
					}

					try
					{
						while( _isRunning )
						{
							Action currentTask = null;

							lock( _lockObject )
							{
								if( _tasks.Count == 0 )
									break;

								currentTask = _tasks.Dequeue();
							}

							currentTask();
						}
					}
					catch( Exception err )
					{
						this.LastException = err;
						_tasks.Clear();
					}
					finally
					{
						lock( _lockObject )
						{
							_workDone.Set();
							_workAvailable.Reset();
							_isProcessing = false;
						}
					}
				}
			}
			catch( ThreadAbortException )
			{
			}
			finally
			{
				_shutdownHandle.Set();
			}
		}

		#endregion
	}
}
