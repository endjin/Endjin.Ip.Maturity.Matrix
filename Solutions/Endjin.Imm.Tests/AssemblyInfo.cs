// Every test builds its own rule set, IMM, HTTP stub and memory cache, and none of them depends on shared
// mutable state, so they can run in parallel.
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]
