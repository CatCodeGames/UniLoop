# Changelog

## [2.0.0] - 2026-10-26

### Added
- Counted loop: `Loop.Schedule(action, count)` and `Loop.ScheduleOnce(action)` run an action N times or once, then call `OnCompleted`.
- Runner Monitor window (`Window/UniLoop/Runner Monitor`): all tasks in one table by runner type and PlayerLoop point, with peak values.
- `UniLoop.GetMetrics(...)`: task counts without allocations.

### Changed
- Stopping tasks is about 5 times cheaper.

### Fixed
- Player builds failed to compile.
- An exception in one task stopped the other tasks.
- Several bugs in `DeferredDenseArray` and in handling stale handles.
- Runners could be inserted into the PlayerLoop twice with Domain Reload disabled.
