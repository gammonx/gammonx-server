# Game-server performance baseline (T0)

Opt-in .NET 10 console benchmarks for the game server and Mars neural evaluation. The game-server benchmarks run when called with the options below. Use the `nn` subcommand for neural benchmarks and tuning studies. [NN benchmarks and historical findings](NN/README.md)

The T0 baseline recorded 35,370 legal sequences, a 3,115,575-byte SignalR message and 86.4 MB allocated for a repository-level turn on the Backgammon spread fixture. An attached metrics listener added 32 bytes per measured serialization across all 12 fixtures. [Baseline findings and raw reports](Baselines/README.md)

## Run from the repository root

```powershell
dotnet build src/GammonX/GammonX.Server.Performance/GammonX.Server.Performance.csproj -c Release
dotnet run --project src/GammonX/GammonX.Server.Performance/GammonX.Server.Performance.csproj -c Release --no-build -- --iterations 30 --warmup 5 --telemetry off --output out/performance/baseline.json --cpu-limit unconstrained-local --memory-limit-mb unconstrained-local
dotnet run --project src/GammonX/GammonX.Server.Performance/GammonX.Server.Performance.csproj -c Release --no-build -- --iterations 30 --warmup 5 --telemetry on --compare out/performance/baseline.json --output out/performance/observed.json --cpu-limit unconstrained-local --memory-limit-mb unconstrained-local
```

Run sequentially, with tests/builds and other CPU-intensive work stopped. For optimization comparisons, use the same `--telemetry` value, environment, fixture version, warmup, and repetitions on both revisions. The off/on comparison above specifically estimates the cost of an attached lightweight metric listener; it is not an optimization result or an exporter overhead measurement.

`--cpu-limit` and `--memory-limit-mb` record descriptive resource limits; they do not enforce them. Supply the actual limits when running inside a constrained container. `LogicalProcessors` records what the runtime sees, which is not necessarily the CPU quota. The project disables tiered compilation for stable small samples, and records the runtime, OS, architecture, GC mode, build configuration, assembly revision and module IDs. Uncommitted source changes are reflected by module IDs; assembly revision alone does not identify the working tree.

The executable refuses Debug builds. Default warmup is 5 and measured repetitions are 30. Output is a machine-readable JSON file plus a console table. `--compare` also writes `<output>.comparison.json` with absolute measurements and percentage changes; positive changes mean increases. It checks fixture identities, dice, history, sequence counts, warmup and iterations. It permits payload sizes to change, so future wire-format optimizations can be compared. Compare the recorded environment metadata before interpreting timings.

## Fixtures and stages

- Opening positions with 1-2 and double ones for Backgammon, Portes, Plakoto, Fevga and Tavla.
- Constructed Backgammon spread position: white has one checker on each field 2 through 16, black has 15 on field 23, and double ones are rolled. This is a branching stress fixture, not a measured production distribution.
- Backgammon opening position with 1,000 synthetic roll-history events followed by a 1-2 roll. The rolled-state metadata therefore records 1,001 history events.
- Fixed IDs and dice make sizes and move counts reproducible. Real load-test positions were not available; add versioned, anonymized fixtures when available.

Stages:

| Stage | Included work |
|---|---|
| `generate` | Engine legal-sequence generation on the unrolled board; traversal restores the board/history |
| `clone` | Deep clone of the prepared, rolled game |
| `payload` | Detached active-white-player game payload |
| `serialize` | Production JSON protocol/options, event contract and SignalR envelope; fresh output buffer per operation |
| `repository-turn` | Backgammon only: roll, individual legal moves, end turn, per-command leases and commits, payloads and SignalR serialization for both players, then repository removal |

The turn stage exercises the repository/model/serialization path without a SignalR host, transport, timers, authentication, bot inference or SQS. It includes small harness helper allocations and final removal; it is not a latency benchmark for a deployed hub. Each iteration prepares a fresh match outside the measured section. Other stages reuse an unchanged prepared fixture. Buffer growth is included in serialization allocation; output capacity is not retained between standalone serialization samples.

`AllocatedBytes` uses current-thread allocation around synchronous operations only. The turn stage checks that uncontended lease acquisition completes synchronously. Do not extend these measurements across arbitrary asynchronous I/O without changing the allocation method. Mean/p95 are descriptive measurements, never unit-test timing thresholds.

`WireBytes` is the prepared rolled active-player **SignalR message** size, including its envelope/separator, repeated as fixture metadata on each stage. It is not the cumulative traffic of a full turn. It differs from the earlier handover probe, which serialized a bare payload using default JSON options.

Runtime totals cover the entire run, including warmup, fixture preparation, sampling and console output. They include CPU seconds/core equivalents, total allocation/rate, GC pauses/collections, first-chance exception count, and thread-pool queue length sampled every 100 ms. Short queue spikes can be missed. These totals must not be presented as the cost of measured stage operations alone. The normal production app does not install the benchmark's first-chance exception handler or sampling timer.

## Production telemetry

The `GammonX.Server` meter is available without an exporter package. Attach a .NET metrics listener, `dotnet-counters`, or an existing exporter configured to subscribe to that meter. No metric includes player ID, match ID, connection ID, URI, or command arguments as labels. Matching IDs are used only in correlated error logs.

| Instrument | Type / units | Meaning |
|---|---|---|
| `gammonx.server.command.duration` | Histogram, ms | Hub invocation/connect/disconnect elapsed time, `operation` = resolved method name or lifecycle name |
| `gammonx.server.moves.duration` | Histogram, ms | GameSession legal-move calculation, including replacing the move list |
| `gammonx.server.moves.sequences` | Histogram, sequences | Returned legal-sequence count |
| `gammonx.server.lease.wait` | Histogram, ms | Lease acquisition including registry access and semaphore wait |
| `gammonx.server.lease.hold` | Histogram, ms | Lease lifetime, recorded once on disposal |
| `gammonx.server.external.duration` | Histogram, ms | `operation` = `signalr.send`, `mars.move`, `mars.cube`, `mars.board`, `sqs.send`, `sqs.batch`, `sqs.fifo_batch`; includes retries, serialization/deserialization within each measured call |
| `gammonx.server.message.serialized_size` | Histogram, bytes | Actual JSON protocol bytes serialized; no second serialization and no content labels |
| `gammonx.server.matches.active` | Observable gauge | Stored match count, including pre-start sessions |
| `gammonx.server.lobbies.pending` | Observable gauge | Unique stored lobbies across normal/ranked/bot modes; aliases are not counted separately |
| `gammonx.server.deadlines.registered` | Observable gauge | Current turn/disconnect registrations, including deliberately retained disconnected markers |
| `gammonx.server.timer.tasks` | Observable gauge | Started timer tasks that have not finished; includes asynchronous delays and callback waits |
| `gammonx.server.timer.callbacks.active` | Observable gauge | Expiry handlers in progress, including waits for match leases and I/O; excludes sleeping timer tasks |
| `gammonx.server.bot.fallbacks` | Counter | Fallback decisions, `operation` = `move`, `cube.offer`, `cube.take`, `move.expert_to_hard` |
| `gammonx.server.bot.recoveries` | Counter | Existing reconnect-driven bot-turn resume attempts; automatic recovery is still T2 work |

Durations are inclusive elapsed time, not CPU time. Nested durations overlap. Hub methods catch many errors internally, so command completion does not imply game-command success. HTTP/SQS duration counts failed calls too. Health probes are outside these external-call measurements.

Message size is measured where SignalR actually serializes. Cached serialization may serve multiple recipients; the histogram is neither recipient count nor network egress. WebSocket framing, TLS, compression and retries are not counted. With no listener, `WriteMessage` directly forwards to the original JSON protocol. With a listener, its counting writer has constant-size overhead independent of the number of moves and does not copy the payload.

Gauges read current counts rather than replaying process-start deltas, so a late-attaching collector sees current state. Counts do not clone games or enumerate their histories/move lists. The host owns and disposes the state-observation meter. Histogram recording starts when subscribed; durations already in progress at subscription may not be captured.

## Capture during a load test

Use an installed compatible `dotnet-counters` tool. .NET 10 also supports `dnx dotnet-counters ...` if downloading the tool is appropriate for the environment. Run inside the container or another context with access to the target runtime's diagnostic socket; network reachability alone does not provide diagnostics access.

```powershell
dotnet-counters ps
# Set this to the game-server PID, not the Mars PID.
$serverProcessId = 1234
dotnet-counters collect --process-id $serverProcessId --counters System.Runtime,GammonX.Server --refresh-interval 1 --format json --output out/performance/load-counters.json
```

Stop collection after the workload and cooldown. Capture `System.Runtime` allocation rate, GC pauses/collections, CPU, exceptions and thread-pool queue length alongside the custom meter. Record deployed commit, CPU/memory quota, client command rate, bot/human mix and active variant distribution. For stack attribution, collect a CPU trace separately; this implementation does not establish the production saturation cause.

References: [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters), [runtime metrics](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics-runtime).

## Focused correctness checks

```powershell
dotnet test src/GammonX/GammonX.Server.Tests/GammonX.Server.Tests.csproj --filter "FullyQualifiedName~ServerMetricsTests|FullyQualifiedName~MatchSessionRepositoryTests|FullyQualifiedName~CancellationTokenServiceTests|FullyQualifiedName~MatchLobbyCleanupTests|FullyQualifiedName~SqsWorkQueueTests|FullyQualifiedName~MarsClientTests"
```

The tests verify unchanged protocol bytes and parsing, one serialization per message, duration recording on failure, single lease-disposal measurement, returned sequence counts, and late-observer/current-state behavior. Keep benchmarks separate from these tests.
