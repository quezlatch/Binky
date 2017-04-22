using System;
using System.Threading.Tasks;

namespace Binky
{
	public static class CacheBuilder
	{
		public static IBuilder<TKey, TValue> WithFactory<TKey, TValue>(Func<IUpdateValue<TKey, TValue>> getValueUpdater)
			=> new Builder<TKey, TValue>(getValueUpdater().Get);
		public static IBuilder<TKey, TValue> With<TKey, TValue>(Func<TKey, TValue> getUpdateValue)
			=> new Builder<TKey, TValue>((key,_) => Task.FromResult(getUpdateValue(key)));
		public static IBuilder<TKey, TValue> WithAsync<TKey, TValue>(Cache<TKey, TValue>.UpdateValueDelegate getUpdateValue)
			=> new Builder<TKey, TValue>(getUpdateValue);

		class Builder<TKey, TValue> : IBuilder<TKey, TValue>
		{
			Cache<TKey, TValue>.UpdateValueDelegate _getUpdateValue;

			TKey[] _keys;

			TimeSpan _every;

			TimeSpan _begin;

			TimeSpan _rampUp;

			bool _evictUnused;



			public Builder(Cache<TKey, TValue>.UpdateValueDelegate getUpdateValue)
			{
				_getUpdateValue = getUpdateValue;
			}

			public Cache<TKey, TValue> Build()
			{
				var cache = new Cache<TKey, TValue>(_getUpdateValue, _every, _begin, _rampUp, _evictUnused);
                if (_keys != null)
                    cache.Load(_keys);
                return cache;
			}

			public IBuilder<TKey, TValue> Preload(params TKey[] values)
			{
				_keys = values;
				return this;
			}

			public IBuilder<TKey, TValue> RefreshEvery(TimeSpan every)
			{
				_every = every;
				return this;
			}

			public IBuilder<TKey, TValue> BeginAfter(TimeSpan begin)
			{
				_begin = begin;
				return this;
			}

			public IBuilder<TKey, TValue> WithRampUpDuration(TimeSpan rampUp)
			{
				_rampUp = rampUp;
				return this;
			}

			public IBuilder<TKey, TValue> EvictUnused()
			{
				_evictUnused = true;
				return this;
			}
		}
	}
}
