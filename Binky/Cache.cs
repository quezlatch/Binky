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

		public Task<TValue> GetAsync(TKey key) => _dictionary.GetOrAdd(key, AddNewValue).Completion.Task;
		public TValue Get(TKey key) => GetAsync(key).Result;

		Item AddNewValue(TKey key)
		{
			var item = Item.New();
			UpdateValueInBackground(new TimeSpan(), key, item);
			return item;
		}

		void Tick(object state)
		{
			var i = 0;
			foreach (var kvp in _dictionary)
			{
				var rampUp = new TimeSpan(i * _rampUpTicks);
				var key = kvp.Key;
				var item = kvp.Value;
				UpdateValueInBackground(rampUp, key, item);
				i++;
			}
		}

		void UpdateValueInBackground(TimeSpan rampUpDelay, TKey key, Item item)
		{
			Task.Run(async () =>
			{
				if (Interlocked.CompareExchange(ref item.IsProcessingTick, 1, 0) == 0)
					try
					{
						await Task.Delay(rampUpDelay);
						var result = await _getUpdateValue(key);
						item.SetResult(result);
					}
					finally
					{
						Interlocked.Exchange(ref item.IsProcessingTick, 0);
					}
			});
		}

		public void Dispose()
		{
			_timer.Dispose();
		}

		public delegate Task<TValue> UpdateValueDelegate(TKey key);

		// is class not struct so we can mutate Completion
		public class Item
		{
			public int IsProcessingTick;
			public TaskCompletionSource<TValue> Completion;
			public static Item New() => new Item
			{
				Completion = new TaskCompletionSource<TValue>()
			};
			internal void SetResult(TValue result)
			{
				EnsureCompletionIsUpdatable();
				Completion.SetResult(result);
			}

			void EnsureCompletionIsUpdatable()
			{
				if (Completion.Task.Status == TaskStatus.RanToCompletion)
				{
					Completion = new TaskCompletionSource<TValue>();
				}
			}
	}
	}
}
