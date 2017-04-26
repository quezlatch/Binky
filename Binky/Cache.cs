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

        public void Load(params TKey[] keys)
        {
            Load(keys, false);
        }

        public void Load(IEnumerable<TKey> keys, bool markAsUsed = false)
        {
            var updaters = keys
                .Select(key => new { key, item = Item.New() })
                .Where(t => _dictionary.TryAdd(t.key, t.item))
                .Select((t, i) => new Updater(t.key, t.item, new TimeSpan(i * _rampUpTicks)));
            foreach (var updater in updaters)
                updater.UpdateValueInBackground(markAsUsed, _getUpdateValue);
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
            if (_evictUnused)
                EvictUnusedItems();
            RefreshItems();
        }

        private void RefreshItems()
        {
            var updaters = _dictionary
                .Select((kvp, i) => new Updater(kvp.Key, kvp.Value, new TimeSpan(i * _rampUpTicks)));
            foreach (var updater in updaters)
                updater.UpdateValueInBackground(false, _getUpdateValue);
        }

        private void EvictUnusedItems()
        {
            Item item;
            foreach (var key in from kvp in _dictionary where !kvp.Value.Used select kvp.Key)
                _dictionary.TryRemove(key, out item);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        public delegate Task<TValue> UpdateValueDelegate(TKey key, CancellationToken cancellationToken);

        class Updater
        {
            private TKey _key;
            private TimeSpan _rampUp;
            private Item _item;

            public Updater(TKey key, Item item, TimeSpan rampUp)
            {
                _key = key;
                _item = item;
                _rampUp = rampUp;
            }

            internal void UpdateValueInBackground(bool used, UpdateValueDelegate getUpdateValue)
            {
                _item.Used = used;
                _item.UpdateValueInBackground(_rampUp, _key, getUpdateValue);
            }
        }

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
                            await UpdateValue(key, getUpdateValue, cancellationToken);
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

            private async Task UpdateValue(TKey key, UpdateValueDelegate getUpdateValue, CancellationToken cancellationToken)
            {
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
