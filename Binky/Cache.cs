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

		readonly UpdateValueDelegate _getUpdateValue;

		readonly long _rampUpTicks;

		readonly bool _evictUnused;


		public Cache(UpdateValueDelegate getUpdateValue, TimeSpan every, TimeSpan begin, TKey[] keys, TimeSpan rampUp, bool evictUnused)
		{
			var kvp = from key in keys select new KeyValuePair<TKey, Item>(key, Item.New());
			_dictionary = new ConcurrentDictionary<TKey, Item>(kvp);
			_getUpdateValue = getUpdateValue;
			_timer = new Timer(Tick, null, begin, every);
			_rampUpTicks = rampUp.Ticks;
			_evictUnused = evictUnused;
		}

		public Task<TValue> GetAsync(TKey key)
		{
			var item = _dictionary.GetOrAdd(key, AddNewValue);
			item.Used = true;
			return item.Completion.Task;
		}
		public TValue Get(TKey key)
		{
			try
			{
				return GetAsync(key).Result;
			}
			catch (AggregateException ex)
			{
				throw ex.InnerException;
			}
		}

		Item AddNewValue(TKey key)
		{
			var item = Item.New();
			item.UpdateValueInBackground(new TimeSpan(), key, _getUpdateValue);
			return item;
		}

		void Tick(object state)
		{
			var i = 0;
			foreach (var kvp in _dictionary)
			{
				var key = kvp.Key;
				var item = kvp.Value;
				if (_evictUnused && !item.Used)
					_dictionary.TryRemove(key, out item);
				else
				{
					item.Used = false;
					var rampUp = new TimeSpan(i * _rampUpTicks);
					item.UpdateValueInBackground(rampUp, key, _getUpdateValue);
					i++;
				}
			}
		}

		public void Dispose()
		{
			_timer.Dispose();
		}

		public delegate Task<TValue> UpdateValueDelegate(TKey key, CancellationToken cancellationToken);

		// is class not struct so we can mutate Completion
		class Item
		{
			int _isProcessingTick;
			public bool Used;
			public TaskCompletionSource<TValue> Completion;
			public static Item New() => new Item
			{
				Completion = new TaskCompletionSource<TValue>(),
                Used = true
			};

			internal void UpdateValueInBackground(TimeSpan rampUpDelay, TKey key, UpdateValueDelegate getUpdateValue)
			{
				Runner.Enqueue(async cancellationToken =>
				{
					if (Interlocked.CompareExchange(ref _isProcessingTick, 1, 0) == 0)
						try
						{
							await Task.Delay(rampUpDelay, cancellationToken);
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                var result = await getUpdateValue(key, cancellationToken);
                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    SetResult(result);
                                }
                            }
                            if (cancellationToken.IsCancellationRequested)
                                SetCanceled();
						}
						catch (AggregateException ex)
						{
							SetException(ex.InnerException);
						}
						catch (Exception ex)
						{
							SetException(ex);
						}
						finally
						{
							Interlocked.Exchange(ref _isProcessingTick, 0);
						}
				});
			}

			void SetResult(TValue result)
			{
				EnsureCompletionIsUpdatable();
				Completion.SetResult(result);
			}

			void SetException(Exception ex)
			{
				EnsureCompletionIsUpdatable();
				Completion.SetException(ex);
			}

            void SetCanceled()
            {
                EnsureCompletionIsUpdatable();
                Completion.SetCanceled();
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
