// The tests are pure functions of in-memory YAML and a fixed reference date, so they can run in parallel.
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]
