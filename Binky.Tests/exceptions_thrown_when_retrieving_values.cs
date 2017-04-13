using System;
using Xunit;

namespace Binky.Tests
{
	public class exceptions_thrown_when_retrieving_values : IDisposable
	{
		Cache<string, DateTime> _cache;

		public exceptions_thrown_when_retrieving_values()
		{
			_cache = CacheBuilder
				.With<string, DateTime>(key => { throw new Exception("bad"); })
				.RefreshEvery(TimeSpan.FromMilliseconds(100))
				.Preload("a", "b")
				.Build();
		}

		[Fact]
		public void are_propagated_to_callee()
			=> Assert.Equal("bad", Assert.Throws<Exception>(() => _cache.Get("a")).Message);

		public void Dispose()
		{
			_cache.Dispose();
		}
	}
}
