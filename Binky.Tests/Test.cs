using System;
using System.Threading;
using Xunit;
namespace Binky.Tests
{
	public class Test
	{
		readonly Cache<string, string> _cache;

		public Test()
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
		public void value_is_preloaded_by_key(string key)
		{
			Assert.StartsWith($"{key}: timestamp is ", _cache.Get(key));
		}

		[Fact]
		public void value_is_not_refreshed_before_interval()
		{
			var initialValue = _cache.Get("a");
			var refreshedValue = _cache.Get("a");
			Assert.Same(initialValue,refreshedValue);
		}

		[Fact]
		public void value_is_refreshed_after_interval()
		{
			var initialValue = _cache.Get("a");
			System.Threading.Thread.Sleep(200);
			var refreshedValue = _cache.Get("a");
			Assert.NotSame(initialValue,refreshedValue);
		}

		public string UpdateWithKeyAndTime(string key) => $"{key}: timestamp is {DateTime.Now}";
	}
}
