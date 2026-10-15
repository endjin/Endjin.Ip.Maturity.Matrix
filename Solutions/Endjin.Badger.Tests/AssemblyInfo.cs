// Badge rendering is a pure function of its arguments, so the tests can run in parallel.
// (The one test that changes the current culture does so only on its own execution flow.)
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]
