using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Binky
{

	public sealed class Cache<TKey, TValue> : IDisposable
	{
		readonly ConcurrentDictionary<TKey, Item> _dictionary;
		readonly Timer _timer;

		readonly Func<TKey, TValue> _getUpdateValue;

		public Cache(Func<TKey, TValue> getUpdateValue, TimeSpan every, TimeSpan begin, TKey[] keys)
		{
			var kvp = from key in keys select new KeyValuePair<TKey, Item>(key, Item.New());
			_dictionary = new ConcurrentDictionary<TKey, Item>(kvp);
			_getUpdateValue = getUpdateValue;
			_timer = new Timer(Tick, null, begin, every);
		}

		public TValue Get(TKey key) => _dictionary.GetOrAdd(key, _ => Item.New()).Completion.Task.Result;

		void Tick(object state)
		{
			//TODO: need an interlock here to ditch Tick if it's already going
			foreach (var kvp in _dictionary)
			{
				var key = kvp.Key;
				var item = kvp.Value;
				Task.Run(() => _getUpdateValue(key)).ContinueWith(task =>
				{
					//TODO: probably some complicate concurrency stuff here...
					if (item.Completion.Task.Status == TaskStatus.RanToCompletion)
					{
						item.Completion = new TaskCompletionSource<TValue>();
					}
					item.Completion.SetResult(task.Result);
				});
			}
		}

		public void Dispose()
		{
			_timer.Dispose();
		}

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
