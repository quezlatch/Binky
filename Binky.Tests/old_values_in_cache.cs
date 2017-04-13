using System;
using System.Threading;
using Xunit;

namespace Binky.Tests
{
	public class old_values_in_cache : IDisposable
	{
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
			var _ = _cache.Get("a");
			Thread.Sleep(1000);
			Assert.Equal(1, _count);
		}

		public void Dispose()
		{
			_cache.Dispose();
		}
	}
}
