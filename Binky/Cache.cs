using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Binky
{
	//TODO: don't forget ASP Hosting Queue

	public sealed class Cache<TKey, TValue> : IDisposable
	{
		int _isProcessingTick;
		readonly ConcurrentDictionary<TKey, Item> _dictionary;
		readonly Timer _timer;

		readonly UpdateValueDelegate _getUpdateValue;

		readonly long _rampUpTicks;

		public Cache(UpdateValueDelegate getUpdateValue, TimeSpan every, TimeSpan begin, TKey[] keys, TimeSpan rampUp)
		{
			var kvp = from key in keys select new KeyValuePair<TKey, Item>(key, Item.New());
			_dictionary = new ConcurrentDictionary<TKey, Item>(kvp);
			_getUpdateValue = getUpdateValue;
			_timer = new Timer(Tick, null, begin, every);
			_rampUpTicks = rampUp.Ticks;
		}

		public TValue Get(TKey key) => _dictionary.GetOrAdd(key, _ => Item.New()).Completion.Task.Result;

		void Tick(object state)
		{
			if (Interlocked.CompareExchange(ref _isProcessingTick, 1, 0) == 0)
				try
				{
					ThreadSafeTick();
				}
				finally
				{
					Interlocked.Exchange(ref _isProcessingTick, 0);
				}
		}

		void ThreadSafeTick()
		{
			var i = 0;
			foreach (var kvp in _dictionary)
			{
				var key = kvp.Key;
				var item = kvp.Value;
				var rampUp = new TimeSpan(i * _rampUpTicks);
				Task.Run(async () =>
				{
					await Task.Delay(rampUp);
					var result = await _getUpdateValue(key);
					if (item.Completion.Task.Status == TaskStatus.RanToCompletion)
					{
						item.Completion = new TaskCompletionSource<TValue>();
					}
					item.Completion.SetResult(result);
				});
				i++;
			}
		}

		public void Dispose()
		{
			_timer.Dispose();
		}

		public delegate Task<TValue> UpdateValueDelegate(TKey key);

		// is class not struct so we can mutate Completion
		public class Item
		{
			public TaskCompletionSource<TValue> Completion;
			public static Item New() => new Item
			{
				Completion = new TaskCompletionSource<TValue>()
			};
		}
	}

}
