using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Binky
{
	public class CacheBuilder
	{
		//public static void WithFactory<TKey, TValue>(IUpdateValueFactory<TKey, TValue> factory)
		//{ }
		//public static void WithFactory<TKey, TValue, TState>(IUpdateValueFactory<TKey, TValue, TState> factory)
		//{ }
		public static IBuilder<TKey, TValue> With<TKey, TValue>(Func<TKey, TValue> getUpdateValue)
		{
			return new Builder<TKey, TValue>(getUpdateValue);
		}
		//public static void With<TKey, TValue, TState>(Func<TKey, TState, TValue> getUpdateValue)
		//{ }

		private class Builder<TKey, TValue> : IBuilder<TKey, TValue>
		{
			Func<TKey, TValue> _getUpdateValue;

			TKey[] _values;

			TimeSpan _every;

			TimeSpan _begin;


			public Builder(Func<TKey, TValue> getUpdateValue)
			{
				_getUpdateValue = getUpdateValue;
			}

			public Cache<TKey, TValue> Build()
			{
				return new Cache<TKey, TValue>(_getUpdateValue, _every, _begin, _values);
			}

			public IBuilder<TKey, TValue> Preload(params TKey[] values)
			{
				_values = values;
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
		}
	}

	public interface IBuilder<TKey, TValue>
	{
		IBuilder<TKey, TValue> RefreshEvery(TimeSpan every);
		IBuilder<TKey, TValue> BeginAfter(TimeSpan every);
		IBuilder<TKey, TValue> Preload(params TKey[] values);
		Cache<TKey, TValue> Build();
	}

	public interface IUpdateValueFactory<TKey, TValue>
	{
		IUpdateValue<TKey, TValue> Get();
	}

	public interface IUpdateValueFactory<TKey, TValue, TState>
	{
		IUpdateValue<TKey, TValue> Get(TState state);
	}

	public interface IUpdateValue<TKey, TValue>
	{
		TValue UpdateValue(TKey key);
	}
	public interface IUpdateValue<TKey, TValue, TState>
	{
		TValue UpdateValue(TKey key, TState state);
	}
}
