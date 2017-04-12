using System;
using Xunit;

namespace Binky.Tests
{
	public class values_updated_in_cache : IDisposable
	{
		readonly Cache<string, DateTime> _cache;

		public values_updated_in_cache()
		{
			_cache = CacheBuilder
				.With<string, DateTime>(key => DateTime.Now)
				.RefreshEvery(TimeSpan.FromMilliseconds(10000))
				.WithRampUpDuration(TimeSpan.FromMilliseconds(100))
				.Preload("a", "b")
				.Build();
		}

		[Fact]
		public void have_a_ramp_up_time_to_stagger_the_retrieval_timings()
		{
			var aDate = _cache.Get("a");
			var bDate = _cache.Get("b");
			Assert.NotInRange((bDate - aDate).TotalMilliseconds, -100.0, 100.0);
		}

		public void Dispose()
		{
			_cache.Dispose();
		}
	}
}
