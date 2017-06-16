# Binky

![Build Status](https://ci.appveyor.com/api/projects/status/github/quezlatch/binky)

[![](https://codescene.io/projects/1143/status.svg) Get more details at **codescene.io**.](https://codescene.io/projects/1143/jobs/latest-successful/results)

*Binky* is a very simple pre-emptive cache for .net that will refresh in the background, and thus provide you with zero latency reads.

The cache is created with a builder that basically looks like:

``` csharp
  _cache = CacheBuilder
    .WithFactory(Ioc.GetInstance<SomeRepo>)
    .RefreshEvery(TimeSpan.FromSeconds(1))
    .Preload("a", "b", "c")
    .Build();
```

This will build a cache initially populate inself with the values for keys *a*, *b*, and *c*, and then refresh every second.
Note, the factory is referenced as a method group.

You can also do things such as:
* Add a ramp up period for each value update on a refresh
* Add an duration before begining the preload
* Evict items that have not been used between refreshes


[![Join the chat at https://gitter.im/quezlatch/Binky](https://badges.gitter.im/quezlatch/Binky.svg)](https://gitter.im/quezlatch/Binky?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
preemptive caching for .net

# Example

If you have a repository as the below

```

public class MyRepository 
{
  public Thing GetThings(string key)
  {
    # NB has to take a cancellation token
    return _someExpensiveMethodCall(key, default(CancellationToken));
  }
}

```

then amend this to

```

public class MyRepository
{
  public static Cache<string, Thing> _cache = CacheBuilder
                .WithAsync<string, Thing>(_someExpensiveMethodCall)
                .RefreshEvery(TimeSpan.FromMinutes(1))
                .Build();
                
   public Thing GetThings(string key)
  {
    return _cache.Get(key);
  }
}

```
