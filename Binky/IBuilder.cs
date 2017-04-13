using System;
using System.Threading.Tasks;

namespace Binky
{

	public interface IBuilder<TKey, TValue>
	{
		IBuilder<TKey, TValue> RefreshEvery(TimeSpan every);
		IBuilder<TKey, TValue> BeginAfter(TimeSpan every);
		IBuilder<TKey, TValue> WithRampUpDuration(TimeSpan timeSpan);
		IBuilder<TKey, TValue> Preload(params TKey[] values);
		IBuilder<TKey, TValue> EvictUnused();
		Cache<TKey, TValue> Build();
	}

}
