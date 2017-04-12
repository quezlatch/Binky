using System;
using System.Threading;
using Xunit;

namespace Binky.Tests
{
	public class values_preloaded_in_cache : IDisposable
	{
		readonly Cache<string, string> _cache;

		public values_preloaded_in_cache()
		{
			_cache = CacheBuilder
				.With<string, string>(UpdateWithKeyAndTime)
				.RefreshEvery(TimeSpan.FromMilliseconds(100))
				.Preload("a", "b", "c")
				.Build();
		}

		[Theory]
		[InlineData("a")]
		[InlineData("b")]
		[InlineData("c")]
		public void are_preloaded_by_key(string key)
		{
			Assert.StartsWith($"{key}: timestamp is ", _cache.Get(key));
		}

		[Fact]
		public void are_not_refreshed_before_interval()
		{
			var initialValue = _cache.Get("a");
			var refreshedValue = _cache.Get("a");
			Assert.Same(initialValue, refreshedValue);
		}

		[Fact]
		public void are_refreshed_after_interval()
		{
			var initialValue = _cache.Get("a");
			Thread.Sleep(200);
			var refreshedValue = _cache.Get("a");
			Assert.NotSame(initialValue, refreshedValue);
		}

		public string UpdateWithKeyAndTime(string key) => $"{key}: timestamp is {DateTime.Now}";

		public void Dispose()
		{
			_cache.Dispose();
		}
	}
}
