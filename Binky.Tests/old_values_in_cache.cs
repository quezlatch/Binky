using System;
using System.Threading;
using Xunit;

namespace Binky.Tests
{
	public class old_values_in_cache : IDisposable
	{
        const int EnoughTimeToPreload = 100;
        const int TimeForAFewRefreshes = 1000;
        const int TwoPreloadsAndOneRefresh = 3;
        readonly Cache<string, DateTime> _cache;
		int _count;

		public old_values_in_cache()
		{
			_cache = CacheBuilder
				.With<string, DateTime>(key =>
				{
					_count++;
					return DateTime.Now;
				})
				.RefreshEvery(TimeSpan.FromMilliseconds(200))
				.EvictUnused()
				.Preload("a", "b")
				.Build();
		}

		[Fact]
		public void can_be_evicted_if_they_have_not_been_retrieved_since_the_last_refresh()
		{
            Thread.Sleep(EnoughTimeToPreload);
            _cache.Get("a");
			Thread.Sleep(TimeForAFewRefreshes);
			Assert.Equal(TwoPreloadsAndOneRefresh, _count);
		}

		public void Dispose()
		{
			_cache.Dispose();
		}
	}
}
