# Binky
preemptive caching for .net


## random thoughts/todo
* Add ramp-up duration when updating cache to spread the load on external services
* Add ability to evict values, if they haven't been used since the last refresh
* What to do with exceptions? Maybe propagate to callee through task completion source?
