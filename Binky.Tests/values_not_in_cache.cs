using System;
using System.Threading;
using Xunit;

namespace Binky.Tests
{
	public class values_not_in_cache : IDisposable
	{
		readonly Cache<string, string> _cache;

		public values_not_in_cache()
		{
			_cache = CacheBuilder
				.With<string, string>(UpdateWithKeyAndTime)
				.RefreshEvery(TimeSpan.FromMilliseconds(100))
				.Preload("a", "b", "c")
				.Build();
		}

		[Fact]
		public void can_be_retrieved_adhoc()
		{
			Assert.StartsWith("d: timestamp is ", _cache.Get("d"));
		}

		[Fact]
		public void will_be_refreshed_after_interval_after_they_have_first_been_retrieved()
		{
			var initialValue = _cache.Get("d");
			Thread.Sleep(200);
			var refreshedValue = _cache.Get("d");
			Assert.NotSame(initialValue, refreshedValue);
		}

		public string UpdateWithKeyAndTime(string key) => $"{key}: timestamp is {DateTime.Now}";

		public void Dispose()
		{
			_cache.Dispose();
		}
	}
}
