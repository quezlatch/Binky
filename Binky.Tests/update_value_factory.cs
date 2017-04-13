using System;
using System.Threading.Tasks;
using Xunit;

namespace Binky.Tests
{
	public class update_value_factory
	{
		readonly Cache<string, string> _cache;

		public update_value_factory()
		{
			_cache = CacheBuilder
				.WithFactory(() => new FakeValueRepo())
				.RefreshEvery(TimeSpan.FromSeconds(1))
				.Build();
		}

		[Fact]
		public void provides_cache_value_repository() => Assert.Equal("a and value", _cache.Get("a"));

		class FakeValueRepo : IUpdateValue<string, string>
		{
			public Task<string> Get(string key) => Task.FromResult(key + " and value");
		}
	}
}
