# Game-server T0 baseline findings

The runs below used the Release benchmark on 2026-11-19. Reproduction commands and metric definitions are in the [performance README](../README.md). The raw data includes the [listener-off run](t0-off.json), [listener-on run](t0-on.json), and [comparison](t0-on.json.comparison.json).

Both recorded runs used .NET 10.0.13, Windows 10.0.26300 X64, 16 runtime-visible logical processors, workstation GC, disabled tiered compilation, 5 warmups and 30 measured iterations per stage. No container CPU/memory limit was imposed. Runs were sequential, with no test/build process intentionally running alongside them. This was not an isolated performance lab, and these timings must not be extrapolated to a 0.25-vCPU task.

There are 12 fixtures and 52 stage/fixture results per run: opening 1-2/double-one positions for all five variants, the Backgammon spread fixture, and a Backgammon fixture with 1,000 pre-existing history events. Four Backgammon fixtures also exercise the complete repository-level roll/move/end-turn path, including per-command snapshots/commits and serialization for both players. Hosting, transport, timers, authentication, AWS and Mars are excluded from that component benchmark. No real load-test game records were available for additional fixtures.

Listener-off results (rounded; MB/KB are decimal):

| Backgammon fixture | Legal sequences | Prepared active-player SignalR message | Repository-turn allocation | Repository-turn mean |
|---|---:|---:|---:|---:|
| Opening, 1-2 | 30 | 2,159 B | 281 KB | 1.706 ms |
| Opening, double ones | 245 | 22,591 B | 1.014 MB | 3.774 ms |
| Spread, double ones | 35,370 | 3,115,575 B | 86.426 MB | 386.345 ms |
| Opening, 1-2, 1,000 prior history events | 30 | 2,159 B | 511 KB | 1.555 ms |

The message size above describes the prepared rolled state, not all messages in the turn. Compared with the original analysis probe, it now includes the event contract, SignalR envelope and separator using the configured JSON protocol. The standalone serialization stage includes fresh output-buffer growth; its allocation is not directly comparable to the earlier `SerializeToUtf8Bytes` probe.

The spread fixture breaks down as follows:

| Stage | Off: mean / allocation | On: mean / allocation | Allocation change |
|---|---:|---:|---:|
| Generate sequences | 123.982 ms / 22.601 MB | 99.882 ms / 22.549 MB | -0.232% |
| Clone rolled game | 72.337 ms / 16.414 MB | 60.405 ms / 16.414 MB | approximately 0% |
| Build active payload | 54.434 ms / 16.129 MB | 59.162 ms / 16.129 MB | approximately 0% |
| Serialize message | 41.564 ms / 8,910,032 B | 40.985 ms / 8,910,064 B | +32 B (+0.000359%) |
| Repository turn | 386.345 ms / 86.426 MB | 330.851 ms / 86.416 MB | -0.0116% |

Interpretation:

- The original branching/allocation finding is reproduced. Generation, one clone and one active-player payload together allocate approximately 55 MB in the stress fixture before serialization.
- Enabling message-size observation added exactly **32 B per standalone serialization in all 12 fixtures**, including the 3.12 MB message. This demonstrates constant-size counting-writer overhead without a second payload serialization. Tests also verify a payload getter is read once and the protocol bytes are unchanged.
- The attached listener counted 4,516 measurements. It only counts samples in memory; these figures do not include a production exporter or full hub-host overhead.
- The off/on comparison is an instrumentation experiment, not a performance optimization. Timing swings in both directions, and small allocation differences in other stages, are not evidence that subscribing makes gameplay faster. Use repeated same-mode runs on controlled hardware to assess future optimizations.
- Runtime totals include preparation, warmup and harness work. The off run took 25.19 seconds, consumed 24.61 CPU-seconds, allocated 5.448 GB and recorded 10.31 seconds of GC pauses (40.9%). The on run took 21.60 seconds, consumed 21.50 CPU-seconds, allocated 5.447 GB and recorded 9.00 seconds of GC pauses (41.7%). Both recorded zero first-chance exceptions and a sampled peak thread-pool queue length of zero. The stress-heavy synchronous benchmark is not a production request distribution.
