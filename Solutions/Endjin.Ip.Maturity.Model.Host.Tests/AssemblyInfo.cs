// Every test creates its own function instance, request and substitute GitHub sources, so they can run in parallel.
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]
