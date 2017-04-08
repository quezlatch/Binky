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
		readonly TKey[] _keys;

		public Cache(Func<TKey, TValue> getUpdateValue, TimeSpan every, TimeSpan begin, TKey[] keys)
		{
			var kvp = from key in keys select new KeyValuePair<TKey, Item>(key, Item.New());
			_dictionary = new ConcurrentDictionary<TKey, Item>(kvp);
			_getUpdateValue = getUpdateValue;
			_keys = keys;
			_timer = new Timer(Tick, null, begin, every);
		}

		public TValue Get(TKey key)
		{
			return _dictionary[key].Completion.Task.Result;
		}

		void Tick(object state)
		{
			//TODO: need an interlock here to ditch Tick if it's already going
			foreach (var key in _keys)
			{
				Item item;
				if (_dictionary.TryGetValue(key, out item))
				{
					var k = key;
					Task.Run(() => _getUpdateValue(k)).ContinueWith(task =>
					{
						//TODO: probably some complicate concurrency stuff here...
						if (item.Completion.Task.Status == TaskStatus.RanToCompletion)
						{
							item.Completion = new TaskCompletionSource<TValue>();
						}
						item.Completion.SetResult(task.Result);
					});
				}
				else
				{
					//TODO: should probably do something here...
				}
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
