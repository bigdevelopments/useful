namespace Useful;

/// <summary>
/// Batches items which can not be serviced immediately because the servicer is still processing previous items
/// </summary>
public class Batcher<T> : IDisposable
{
	// synchronous service delegate
	private readonly Action<List<T>> _batchedHandlerSync;

	// asynchronous service delegate
	private readonly Func<List<T>, Task> _batchedHandlerAsync;

	// use a system of double buffering
	private readonly List<T> _bufferA;
	private readonly List<T> _bufferB;

	// references to the buffers
	private List<T> _inputBuffer;
	private List<T> _workBuffer;

	// input locking
	private readonly object _inputLock;

	// work signalling
	private readonly AutoResetEvent _signal;

	// disposal
	private volatile bool _disposed;

	private Batcher()
	{
		_bufferA = new List<T>();
		_bufferB = new List<T>();
		_inputBuffer = _bufferA;
		_workBuffer = _bufferB;
		_inputLock = new object();
		_signal = new AutoResetEvent(false);
	}

	public Batcher(Action<List<T>> handler) : this()
	{
		_batchedHandlerSync = handler;

		// for synchronous use, use a long running task
		Task.Factory.StartNew(DispatcherSync, TaskCreationOptions.LongRunning);
	}

	public Batcher(Func<List<T>, Task> handler) : this()
	{
		_batchedHandlerAsync = handler;

		// for asynchronous use, just use a threadpool thread
		Task.Run(DispatcherSync);
	}

	public void Dispose()
	{
		_disposed = true;

		// signal to 'unblock' the dispatcher
		_signal.Set();
	}

	public void Add(T item)
	{
		// no insertions after disposal
		if (_disposed) throw new ObjectDisposedException(nameof(Batcher<T>));

		// stick the item in the input buffer
		lock (_inputLock) _inputBuffer.Add(item);

		// signal the data is present
		_signal.Set();
	}

	private async Task DispatcherSync()
	{
		// run until disposed
		while (!_disposed)
		{
			// wait for a signal that more data is ready or disposal
			if (_batchedHandlerSync != null) _signal.WaitOne();
			else await WaitOneAsync(_signal);

			// swap buffers
			lock (_inputLock)
			{
				if (_inputBuffer == _bufferA)
				{
					_inputBuffer = _bufferB;
					_workBuffer = _bufferA;
				}
				else
				{
					_inputBuffer = _bufferA;
					_workBuffer = _bufferB;
				}
			}

			// work buffer may not have anything in it during disposal
			if (_workBuffer.Count > 0)
			{
				try
				{
					// call the service method
					if (_batchedHandlerSync != null) _batchedHandlerSync(_workBuffer);
					else await _batchedHandlerAsync(_workBuffer);
				}
				catch
				{
					// we have no control over the behaviour of the servicer,
					// but it is running on our thread (or context), so we must
					// catch here to be able to carry on
				}

				// and clear buffer nor next switch
				_workBuffer.Clear();
			}
		}
	}

	private static Task WaitOneAsync(WaitHandle waitHandle)
	{
		var tcs = new TaskCompletionSource<bool>();
		var handle = ThreadPool.RegisterWaitForSingleObject(waitHandle, (_, _) => tcs.TrySetResult(true), null, 0, true);
		var task = tcs.Task.ContinueWith(_ => handle.Unregister(null));
		return task;
	}
}
