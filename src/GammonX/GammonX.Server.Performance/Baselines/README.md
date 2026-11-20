# Game-server baseline findings

## T5 snapshot allocation

The listener-off Release run used five warmups and 30 measured iterations on the T5 working tree. All 84 fixture/stage pairs matched the preceding move-sequence baseline in sequence count and wire size. Cloning the Backgammon spread position fell from 1,832 to 1,304 bytes per operation (28.8%). The full human repository turn allocated 33.18 MB and averaged 127.62 ms, compared with 33.21 MB and 125.14 ms previously. This single local run establishes an allocation baseline for future changes. [T5 capture](t5-off.json) and [stage-by-stage comparison](t5-off.json.comparison.json)

## Move-sequence refactor

The Release captures use the T0 fixture IDs, dice, five warmups and 30 iterations. All 52 shared standard stages retained their sequence counts and wire sizes. The spread fixture still produced 35,370 sequences and a 3,115,575-byte active message. [Standard capture](move-sequences-off.json), [T0 comparison](move-sequences-off.json.comparison.json)

| Backgammon spread operation | Mean ms | p95 ms | MB per operation | Full generations |
|---|---:|---:|---:|---:|
| Single move command | 0.0101 | 0.0107 | 0.0052 | 0 |
| Clone rolled game | 0.0012 | 0.0018 | 0.0018 | 0 |
| Human repository turn | 125.14 | 143.31 | 33.21 | 4 |
| Repeated active query | 84.93 | 104.52 | 22.23 | 1 |
| Waiting query | 0.0013 | 0.0017 | 0.0014 | 0 |
| Complete bot batch | 1.50 | 1.59 | 0.20 | 0 |
| Local bot fallback | 87.60 | 103.38 | 22.26 | 1 |

Coordinate commands use the engine resolver. All 12 command fixtures recorded zero full generations. The spread command allocated 5,176 bytes. Its complete human turn generated four lists for active-player responses and allocated 33.21 MB, down from 86.43 MB at T0 and 58.77 MB with the full-list validator. Repeated active queries still generate the response while holding the lease. These local measurements exclude deployed traffic and Mars inference. [Bot capture](move-sequences-bot-off.json), [Historical bot comparison](move-sequences-bot-off.json.comparison.json)

## Post-T2 baseline and bot-turn diagnostic

The listener-off Release capture after T1, T2, and the deferred-generation change uses the same 12 fixtures, 5 warmups, and 30 measurements per stage as T0. Both captures report .NET 10.0.13 on Windows X64, 16 runtime-visible logical processors, and no container limits. All standard fixture sequence counts and prepared message sizes match T0. Use `t2-off.json` as the baseline for subsequent component changes. [Post-T2 baseline](t2-off.json) · [Comparison with T0](t2-off.json.comparison.json)

The standard repository-turn stages run the human move path. The spread turn allocated 86.43 MB at T0, 86.42 MB in the same-session pre-change run, and 86.42 MB after the change. Its means were 386.3 ms, 327.4 ms, and 320.0 ms respectively. The same-session run shows why the historical timing delta should not be attributed to bot deferral.

The paired bot diagnostic replays one precomputed sequence per fixture and measures game-server work with and without local sequence generation. Below are first-run means and decimal allocation units. The repeat kept the spread allocation below 0.20 MB on the deferred path, with an eager mean of 162.5 ms and a deferred mean of 1.25 ms. [Paired diagnostic](t2-bot-turns-off.json) · [Repeat](t2-bot-turns-off-repeat.json)

| Backgammon fixture | Eager mean | Deferred mean | Eager allocation | Deferred allocation |
|---|---:|---:|---:|---:|
| Opening, 1-2 | 3.91 ms | 1.96 ms | 225 KB | 184 KB |
| Opening, double ones | 3.22 ms | 1.80 ms | 613 KB | 193 KB |
| Spread, double ones | 168.76 ms | 1.13 ms | 42.82 MB | 0.19 MB |

Allocation fell on all 12 fixtures in both runs. The Backgammon opening with double ones saved about 69% and the spread fixture saved about 99.6%. Timings varied more on the small fixtures. The spread position is a branching stress case. These measurements exclude Mars inference and deployed request latency. [Diagnostic scope and reproduction](../README.md#paired-bot-turn-diagnostic)

## T0 baseline findings

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
