using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
	public class AsyncQueue<T>
	{
		private readonly Queue<TaskCompletionSource<T>> _awaiters;
		private readonly Queue<T> _queue;

		public AsyncQueue()
		{
			_queue = new Queue<T>();
			_awaiters = new Queue<TaskCompletionSource<T>>();
		}

		public Task EnqueueAsync(T item)
		{
			Enqueue(item);
			return Task.FromResult(0);
		}

		public void Enqueue(T item)
		{
			lock (_awaiters)
			{
				if (_awaiters.Count > 0)
				{
					_awaiters.Dequeue().SetResult(item);
				}
				else
				{
					_queue.Enqueue(item);
				}
			}
		}

		public T Dequeue()
		{
			Task<T> promise = DequeueAsync();
			promise.Wait();
			return promise.Result;
		}

		public Task<T> DequeueAsync()
		{
			lock (_awaiters)
			{
				if (_queue.Count > 0) return Task.FromResult(_queue.Dequeue());
				TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
				_awaiters.Enqueue(tcs);
				return tcs.Task;
			}
		}
	}
}
