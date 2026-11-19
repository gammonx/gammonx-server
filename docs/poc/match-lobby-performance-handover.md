# MatchLobbyHub performance and lifecycle implementation handover

- Date: 2026-11-17
- Reviewed commit: `90fde9cedf528a00424682741715f0529f33d18b`
- Status: T0 implemented and locally validated on 2026-11-19; T1-T7 pending.

## Objective and scope

Improve the game server's CPU efficiency and behavior under overload. The reported AWS load test ran smoothly at approximately 100 concurrent users before CPU reached 100%; RAM did not reach its limit. Reported container resources were 0.25 vCPU and approximately 500 MB RAM.

Scope includes `MatchLobbyHub`, its session repository, game engine calls, payload construction, bot orchestration on the game server, timers, lobby cleanup, and result publication. Mars inference, neural-net tuning, and Mars resource sizing are excluded.

The strongest measured candidate is the complete legal-move pipeline: enumeration, repeated snapshots, and payload serialization. Source inspection also identified unbounded bot-loop behavior, stale timeout callbacks, and failure paths that can retain started matches. These findings have not been correlated with a production CPU trace or the deployed commit. Do not describe them as a proven explanation of the 100-user threshold.

This file records the analysis and implementation handover. No application code was changed during the original analysis. T0 has since been implemented; its completion record below identifies the code, measurements, validation, and remaining production verification. Existing user work was preserved.

## Source map

Paths below are relative to `src/GammonX/`. Search by method name; line numbers will move during implementation.

| Area | Source and methods |
|---|---|
| Hub orchestration | `GammonX.Server/MatchLobbyHub.cs`: `StartMatchAsync`, `EndTurnAsync`, `PerformBotTurnAsync`, `PerformMoveAsync`, `PerformRollAsync`, `SendGameState`, `SendMatchState` |
| Match state ownership | `GammonX.Server/Services/MatchSessionRepository.cs`: `AcquireStateAsync`, `Get`, `MatchSessionLease.Commit`, `DisposeAsync` |
| Active game | `GammonX.Server/Models/gameSession/GameSessionImpl.cs`: `CalculateLegalMoveSequences`, `MoveCheckers`, `UndoLastMove`, `Clone` |
| Variant cloning | `GammonX.Server/Models/gameSession/BackgammonGameSession.cs`, `PlakotoGameSession.cs` |
| Completed rounds | `GammonX.Server/Models/gameSession/GameSessionSnapshot.cs` |
| Move enumeration | `GammonX.Engine/Services/boards/BoardBaseServiceImpl.cs`: `GetLegalMoveSequences`, `ExploreSequencesRecursively`, `FilterSequencesByDiceRules` |
| Move data and validation | `GammonX.Engine/Models/MoveSequenceModel.cs`; `GammonX.Server/Models/gameSession/MoveSequences.cs` |
| Client payloads | `GammonX.Server/Contracts/payloads/EventGameStatePayload.cs`: `Create` |
| History snapshots | `GammonX.Engine/History/impls/BoardHistoryImpl.cs`: `Clone` |
| Deadline registry | `GammonX.Server/Services/ICancellationTokenService.cs`, `CancellationTokenServiceImpl.cs` |
| Timer callbacks | `MatchLobbyHub.cs`: `StartTurnTimer`, `HandleTurnTimeoutAsync`, `StartDisconnectGraceTimer`, `ResolveDisconnectExpiryAsync` |
| Lobby lifecycle | `GammonX.Server/Services/matchmaking/MatchLobbyCleanupService.cs`, `MatchmakingWorker.cs` |
| Publications | `GammonX.Server/Extensions/MatchSessionExtensions.cs`; `GammonX.Server/Queue/WorkQueueService.cs`, `SqsWorkQueue.cs` |

The mutation path is:

```text
Hub command
  -> acquire per-match lease
  -> clone working match
  -> change game state / enumerate legal continuations
  -> clone committed match
  -> construct recipient payloads
  -> serialize and send, currently still holding the lease
```

State-only queries already use `ReadGameState` / `ReadMatchState` without cloning a working match. Completed rounds already use copy-on-write wrappers. Built-in immutable history events are shared, although their stack storage is copied. Waiting players already receive no legal-move sequences, and bot recipients are skipped. Preserve these optimizations.

## Evidence and reproducible baseline

A temporary console probe referenced the Release assemblies for Server, Engine, and Models, used .NET 10, and executed without hosting the application or calling Mars. Allocations were measured with `GC.GetAllocatedBytesForCurrentThread()` around synchronous operations after five warmup iterations. Generation used 100 measured iterations for opening positions and 15 for the spread position; clone, payload, and serialization used 100 each. Values are rounded decimal KB/MB.

| Position / dice | Legal sequences | Generate sequences | Clone active game once | Build active-player payload | JSON payload bytes |
|---|---:|---:|---:|---:|---:|
| Opening / 1,2 | 30 | 22 KB | 14 KB | 12 KB | 2 KB |
| Opening / 1,1,1,1 | 245 | 175 KB | 116 KB | 112 KB | 23 KB |
| Spread / 1,1,1,1 | 35,370 | 22.7 MB | 16.4 MB | 16.1 MB | 3.1 MB |

The spread fixture was constructed as follows:

```csharp
var game = new GameSessionFactory(new DiceServiceFactory())
    .Create(Guid.NewGuid(), GameModus.Backgammon);
var playerId = Guid.NewGuid();
game.StartGame(playerId, Guid.NewGuid());
Array.Clear(game.BoardModel.Fields);
for (var i = 2; i <= 16; i++)
{
    game.BoardModel.Fields[i] = -1;
}
game.BoardModel.Fields[23] = 15;
```

Measure each operation independently:

1. `BoardServiceFactory.Create(GameModus.Backgammon).GetLegalMoveSequences(game.BoardModel, true, [1, 1, 1, 1])`.
2. Prepare the rolled state with `game.SetDiceRolls([1, 1], true)`, then measure `((ICloneable)game).Clone()`.
3. Measure `game.ToPayload(playerId, [], false)` using the prepared state.
4. Prepare that payload once, then measure `JsonSerializer.SerializeToUtf8Bytes(payload)`.

The JSON measurement excludes the SignalR envelope and uses the default System.Text.Json options. This is a constructed high-branching position, not a production-frequency estimate or an AWS capacity benchmark. The approximately 55 MB combined generation/clone/payload allocation excludes serialization and transport. Do not sum the allocation values and present the result as retained memory.

History-only clone allocations, with no legal moves, were approximately 1.8 KB at zero events, 4.8 KB at 100 events, and 26.4 KB at 1,000 events. History remains a scaling cost, but the large move lists dominated this probe.

Completed verification during analysis:

- Server Release build passed with one existing CS8602 warning in `MatchHistoryImpl.cs`.
- 33 tests passed across `MatchSessionRepositoryTests` and `MatchLobbyCleanupTests`.
- The bot-loop and stale-turn-timer findings were established by source inspection; deterministic regression tests still need to be added.
- No production load test, CPU profile, or heap-retention proof was collected.

## Delivery order

Deliver small PRs with their own validation. The order below separates correctness changes from the protocol and ownership changes with greater compatibility risk.

| Order | Work item | Priority | Dependency |
|---|---|---|---|
| 1 | T0: reproducible measurements and bounded telemetry **(complete locally)** | High | None |
| 2 | T1: turn-deadline generation checks | High correctness | None |
| 3 | T2: bounded bot progress and recovery | High correctness | Reuse T1 deadline identity for recovery |
| 4 | T3: remove started lobbies reliably | High lifecycle | None |
| 5 | T4: move timer ownership out of hubs | Medium | Preserve T1 and T2 behavior |
| 6 | T5: reduce active-game snapshot allocation | High CPU | T0 baseline |
| 7 | T6: compact legal continuations and client payloads | High CPU | T0; coordinate with T5 and client implementation |
| 8 | T7: shorten match leases around external I/O | High resilience | T1, T2; explicit operation/version ownership |

### T0: establish a repeatable performance baseline

Implementation:

- Add an opt-in benchmark harness for generation, clone, payload construction, serialization, and a complete roll/move/end-turn flow. Recreate the fixtures above and add representative positions from actual load-test games when available.
- Keep throughput benchmarks out of ordinary timing-sensitive unit-test assertions. Record runtime version, build configuration, machine/container CPU allocation, variant, dice, history length, sequence count, and repetitions.
- Add histograms for command duration, move-generation duration, sequence count, outgoing payload bytes, lease wait/hold duration, and external-call duration.
- Add aggregate counts for active matches, pending lobbies, registered deadlines, running callbacks, bot fallbacks, and recovery attempts. Do not use player IDs or match IDs as metric labels.
- Measure wire size at serialization/transport where possible; do not serialize each payload a second time solely to measure it.
- Capture runtime allocation rate, GC pause/time, CPU, exceptions, and thread-pool queue length. Log individual failures with correlation IDs without logging credentials.

Acceptance:

- Another developer can recreate the sequence counts and allocation measurements without AWS or Mars.
- Normal instrumentation does not add allocations proportional to the number of legal moves.
- Baseline and changed runs use identical fixtures and report both absolute values and percentage changes.

#### T0 completion record — 2026-11-19

Implemented against base commit `64e2bb3730d6390167264d30ba5b4251864acb1d`, with the T0 working-tree changes. Raw reports also record assembly module IDs to distinguish uncommitted source builds.

Deliverables:

- [GammonX.Server.Performance](../../src/GammonX/GammonX.Server.Performance/README.md): opt-in Release console benchmark and collection instructions. Added to the solution without changing its existing CPU/platform configurations.
- [Server diagnostics](../../src/GammonX/GammonX.Server/Diagnostics/ServerMetrics.cs): `GammonX.Server` meter for hub command durations, game-server move generation, sequence counts, lease waits/holds, external calls, actual JSON protocol message sizes, and bot fallback/reconnect recovery counts.
- Host-owned observable gauges for current matches, unique lobbies, registered deadlines, outstanding timer tasks, and active expiry callbacks. Late subscribers see current state; collection does not clone matches or traverse legal moves/history.
- [MeasuredJsonHubProtocol](../../src/GammonX/GammonX.Server/Diagnostics/MeasuredJsonHubProtocol.cs): counts bytes during the existing serialization pass and preserves configured JSON options, parsing, and message bytes. Metrics contain no player/match/connection IDs or command arguments. Relevant bot error logs now include match IDs for correlation.
- [ServerMetricsTests](../../src/GammonX/GammonX.Server.Tests/ServerMetricsTests.cs): protocol compatibility, single serialization, duration/error paths, lease-disposal accounting, move counts, and current-state gauges.
- Checked-in [listener-off baseline](performance-baselines/t0-off.json), [listener-on baseline](performance-baselines/t0-on.json), and [absolute/percentage comparison](performance-baselines/t0-on.json.comparison.json).

Reproduce from the repository root:

```powershell
dotnet build src/GammonX/GammonX.Server.Performance/GammonX.Server.Performance.csproj -c Release
dotnet run --project src/GammonX/GammonX.Server.Performance/GammonX.Server.Performance.csproj -c Release --no-build -- --iterations 30 --warmup 5 --telemetry off --output out/performance/baseline.json --cpu-limit unconstrained-local --memory-limit-mb unconstrained-local
dotnet run --project src/GammonX/GammonX.Server.Performance/GammonX.Server.Performance.csproj -c Release --no-build -- --iterations 30 --warmup 5 --telemetry on --compare out/performance/baseline.json --output out/performance/observed.json --cpu-limit unconstrained-local --memory-limit-mb unconstrained-local
```

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

Validation:

- Release build of the performance project and its server dependencies passed. An existing CS8602 warning remains in `MatchHistoryImpl.cs`; the final incremental build had no warnings/errors.
- **84 focused tests passed** across `ServerMetricsTests`, `MatchSessionRepositoryTests`, `CancellationTokenServiceTests`, `MatchLobbyCleanupTests`, `SqsWorkQueueTests`, and `MarsClientTests`.
- **20 additional tests passed** covering fake-bot fallback, reconnect recovery, old-socket disconnect, expiry waiting for a lease, move-before-notification persistence, completion cleanup failures, and `MarsBotServiceTests`.
- An earlier broad `MatchHubTests` run was stopped because it included live-bot-dependent tests. It is not counted as a passed run; the selected self-contained cases above were used instead.
- Tracked and newly added text files passed whitespace/conflict-marker checks.

Operational follow-up (not performed locally): deploy the instrumented build, subscribe to `GammonX.Server` and `System.Runtime`, and repeat the controlled AWS workload. The [benchmark README](../../src/GammonX/GammonX.Server.Performance/README.md#capture-during-a-load-test) provides the collection command and metric semantics. Confirm CPU quota, traffic mix and cooldown counts, and capture CPU stacks for attribution. T0 makes that measurement possible; it does not resolve the T1-T7 defects or establish a new supported user capacity.

### T1: prevent stale turn timers from changing current state

Current defect: `StartTurnTimer` cancels/removes its registration before awaiting `HandleTurnTimeoutAsync`. The handler waits for the match lease but receives no timer identity. A command can advance the game before the waiting callback acquires the lease; the callback can then resign a player using newer state.

Implementation:

1. Introduce a deadline registration containing a unique generation, player/match/category, absolute deadline, and the expected obligation: join, initial start, next-game acceptance, turn, or cube response.
2. Include the relevant game/turn identity for turn deadlines. Do not invalidate a valid turn deadline merely because a roll or move changed the phase or state revision within that same turn.
3. Pass the registration identity into the expiry handler. Keep the registration current while waiting for the lease.
4. Under the match lease, verify registration identity, deadline, participant, expected obligation, and connection/disconnect policy. Then conditionally consume that exact registration and apply the transition once.
5. Replacement/cancellation must make an already-queued callback inert. Add a compare-and-remove operation to the registry instead of a key-only removal for expiry.
6. Reuse the identity checks already implemented in `ResolveDisconnectExpiryAsync`; preserve its special handling when both humans disconnect.
7. Use an injected `TimeProvider` for controllable deadlines. The warning event should report the original absolute expiration, rather than extending it by computing another half-duration after a delayed send.

Regression tests:

- Hold a match lease, let an old expiry queue, advance the turn/install a replacement deadline, release the lease: old callback makes no state change and cannot cancel the replacement.
- Repeat for reconnect, next-game acceptance, cube response, and match removal.
- Rolling/moving within the same turn does not invalidate its legitimate deadline.
- Duplicate callbacks produce one transition/publication only.
- A delayed warning/send does not extend the advertised action deadline.

Extend `CancellationTokenServiceTests` and `Integration/MatchHubTests.cs`, particularly the existing disconnect-expiry tests. Use barriers and controlled time, not waits of 60-300 seconds. The existing real-time `TurnTimerIntegrationTests` test is skipped because it is slow; do not depend on it as the regression gate.

### T2: bound bot progress and recover failed turns

Current defects:

- `PerformBotTurnAsync` updates `canMove` only inside `foreach (nextMoves.Moves)`. An empty result while `canMove` is true repeatedly requests moves without changing the game.
- An exception after the human deadline was canceled can leave a started match with no successor deadline. With the human still connected, disconnect cleanup does not resolve that state.

Implementation:

1. Before every move request and application, check operation cancellation and the current game/turn identity.
2. Treat null, empty, or illegal bot output as an unusable response when legal moves exist. Use a deterministic legal fallback from the server's current move data, following the existing service-failure fallback policy.
3. Measure progress through unused dice and committed game transitions. Every accepted move batch must consume dice or conclude the game. Bound move-evaluation iterations by the dice remaining at loop entry; each successful iteration must consume at least one die. Cube/start transitions need separate explicit handling.
4. Stop applying the returned batch immediately when the game or match concludes. Validate against the latest committed state after any partial success.
5. If no legal moves exist, finish the turn through the normal rule path. Do not use an empty fallback to keep the loop alive.
6. Handle requested cancellation separately from service failure. It must not trigger additional evaluation requests or continue normal fallback play. Preserve reconnect's ability to resume from committed state.
7. If a non-cancellation failure prevents completion, register a bounded recovery operation with its own identity and deadline. Allow one automatic recovery attempt using server-legal fallback moves. On repeated failure, close/clean up the match as a server failure without awarding a competitive loss to the human, emit an operational error, and prevent duplicate result publications. Keep this recovery path independent of client requests.
8. For connection cancellation, hand responsibility to disconnect/reconnect processing. For host shutdown, cancel and drain owned work rather than scheduling recovery.

Regression tests:

- Fake bot repeatedly returns an empty sequence: bounded request count and legal fallback progress.
- Illegal response, exception after a committed submove, and game-winning move partway through a response: no replay of committed moves and no actions after completion.
- Canceled connection: no subsequent bot requests; reconnect resumes the committed turn.
- Forced recovery failure: no permanently stored match without a live owner/deadline, no human forfeit for an internal server error, and no duplicate result events.

Extend `MatchHubTests`, including `MatchHubBotFallbackCompletesTurnAfterServiceFailure` and `RankedBotReconnectResumesInterruptedTurn`. These tests should use fake bot services, not real Mars inference.

### T3: remove the lobby at the committed start transition

Current defect: `StartMatchAsync` commits the started match, sends notifications, and may perform a bot turn before removing the matchmaking lobby. A failure in that interval retains the lobby. After five minutes, the cleanup worker repeatedly loads/clones the started match and declines to remove it.

Implementation:

- Remove the lobby and its queue aliases immediately after a successful first-game commit while the lease is held, before client sends or bot calls.
- Keep removal idempotent. Do not remove the lobby on a rejected command, an uncommitted start, or an opening-roll tie.
- When the expiry worker encounters a stale lobby whose match already started, remove only the obsolete lobby registration/aliases. Preserve the started match, connections, groups, and deadlines.
- Add a cheap lease read for start status if needed; checking whether a match started should not require cloning its complete active game.

Acceptance tests:

- Throw from the first start notification and from the first bot turn: started match remains valid and its lobby aliases are absent.
- Opening ties retain the lobby; rejected starts preserve existing deadlines.
- Cleanup of a previously stale started lobby performs no forfeiture, connection removal, or result publication.

Extend `MatchLobbyCleanupTests` and the start/failure cases in `MatchHubTests`. T2 is still necessary to recover a match whose bot turn failed after lobby removal.

### T4: give timers service-owned lifetimes

Current behavior: timer tasks capture the transient hub, retaining its context and service-provider references. Turn deadlines can last five minutes. Background sends first access disposed hub properties, catch `ObjectDisposedException`, and then use `IHubContext`. Cancellation sources have no explicit disposal owner. This is avoidable retention and exception work; it is not by itself proof of an unbounded leak.

Implementation:

- Extract deadline scheduling and timeout/disconnect orchestration into a DI-managed service. Timer callbacks capture registration IDs/data and service dependencies, never a hub, `HubCallerContext`, or a connection-scoped service provider.
- Send background notifications directly through `IHubContext<MatchLobbyHub>`.
- Keep scheduled operations observable and drain/cancel them on host shutdown. Asynchronous delays are sufficient initially; a centralized priority queue is optional only if measurement justifies it.
- Give each registration one owner responsible for its cancellation registration and CTS disposal after the callback has finished. Do not dispose a CTS while another operation can still access its `Token` property.
- Avoid invoking arbitrary cancellation callbacks while holding the global registry lock. Remove/replace the expected entry under the lock, then cancel outside it, preserving replacement identity safety.
- Preserve the deliberate disconnected marker used while waiting for the other disconnected human's deadline. Do not remove it automatically just because its delay task completed.

Acceptance:

- Callback completion, replacement, cancellation, and shutdown leave no abandoned tasks or registrations.
- Disposed-hub exceptions disappear from ordinary timer notifications.
- Repeated connect/play/disconnect cycles return repository/registry counts to baseline, allowing intentionally shared bot profiles and active participants.
- Tests cover cancellation/replacement races and both-human-disconnect behavior. A targeted retention diagnostic can use weak references after timer completion; avoid flaky heap-size assertions in ordinary tests.

### T5: reduce active-game snapshot allocation without weakening isolation

Current behavior: a mutation normally clones on working-state load and again on commit. The active game copies its full move list, dice, undo state, board, and history stack. Payload construction copies the move list again. A roll, two individual moves, and end-turn normally produce eight match snapshots.

Implementation:

1. Introduce an internal immutable representation for generated sequences and moves, separate from mutable public/client contracts. A read-only interface over mutable lists is insufficient; callers must not be able to mutate backing storage.
2. Share immutable legal-move data between working and committed snapshots. A command that changes legal continuations replaces the reference. Convert to detached mutable DTOs only at compatibility boundaries.
3. Preserve custom history-event defensive cloning. Built-in event values are already shared; consider persistent/copy-on-write history storage only if the post-move-list profile still justifies it.
4. Use explicit copy constructors/factories for game and match snapshots to avoid allocating default boards, empty lists, or generated IDs that are immediately replaced. Apply consistently to variant implementations.
5. Keep completed-round sharing and state-only query optimizations. Do not replace the repository with mutation of shared live objects simply because a lease exists: callers can retain snapshots and payloads after release.

Acceptance:

- Existing repository isolation tests pass, including uncommitted mutations, snapshot capture at commit, retained earlier snapshots, completed-round detachment, and detached responses.
- Add tests showing mutations to exposed contracts cannot modify shared move data.
- Clone allocation no longer grows linearly with the number of immutable legal sequences. Report measured before/after values for all baseline fixtures.
- All variants retain their specific state, including cube and pinned-checker state, undo order, dice, and history.

### T6: reduce move enumeration and wire payload size

Current behavior: gameplay retains every legal ordering and recomputes remaining sequences after individual moves/undo. The active recipient receives the full ordering list. Equal-dice duplicate subtrees are already skipped; avoid presenting that existing optimization as new work.

Implementation in two independently reviewable stages:

**Stage A: preserve the existing client contract.**

- Profile enumeration, filtering/deduplication, per-edge history-event creation, and allocation separately.
- Evaluate a shared continuation graph keyed by position, side, and remaining dice, preserving incoming paths/orderings. Include all variant-relevant board state in identity. Do not merge positions based only on the fields array.
- Keep the current implementation as a test oracle while validating replacement behavior. Add cancellation/budget checks at safe traversal points and restore board/history in `finally` paths. Exceeding a budget must fail explicitly, never silently return an incomplete legal list.
- Reuse continuations after moves only after proving equivalence to current behavior. `TryUseMove` currently permits an exact single move anywhere in a sequence and combined contiguous prefixes; simply trimming the first element of a sequence is not equivalent.

**Stage B: introduce a compact client contract.**

- Design a versioned/capability-negotiated payload of legal next actions or a compact continuation graph, together with the client code that consumes it. The client repository/consumer was not reviewed in this analysis; inspect it before implementing Stage B.
- Keep current clients on the existing DTO until they opt into the new representation. Continue server-side validation of every submitted move.
- Preserve the ability to express combined moves, undo, maximum dice usage, higher-die constraints, bearing off, bar entry, hits, and variant-specific rules.
- Do not directly substitute `GetUniqueLegalMoveSequences`: equal final boards can have different intermediate moves that gameplay needs.

Acceptance:

- Differential tests compare legal actions/continuations, not only final boards, across all game variants, both players, distinct dice, and doubles.
- Existing `LegalMovesTests`, `MoveSequencesTests`, `UniqueMoveSequenceTests`, board/history tests, and consuming server tests remain valid.
- The new payload avoids enumerating all orderings on the wire. Capture size and end-to-end allocation improvements for the 35,370-sequence fixture and representative ordinary games.
- Old/new clients can coexist during rollout. Protocol rollback remains possible until migration completes.

### T7: shorten leases around external work

Current behavior: client sends, entire bot turns, and sequential SQS publication happen while holding the per-match lease. `AcquireStateAsync` has no cancellation argument. These locks do not serialize unrelated matches, but slow dependencies retain state and delay reconnect, timeout, and state-read operations for the affected match.

Implementation in separate steps:

1. Add cancellation-aware lease acquisition. If a waiter cancels, decrement its reference count under the existing lock-identity gate; dispose/remove the semaphore only when no owner or waiter remains. Never release a semaphore that the canceled waiter did not acquire.
2. For client sends, create detached payloads and record their ordered outbound sequence under the lease, release it, then send through a bounded delivery mechanism. Preserve event order per match/recipient. Do not blindly release the lock and allow concurrent sends to reorder updates.
3. Coalesce replaceable game-state snapshots under backpressure where protocol semantics permit. Never drop required lifecycle/result events. Capture recipient identity under the lease and account for reconnect replacing the destination.
4. For bot I/O, capture an immutable evaluation request plus operation ownership and state revision, release the lease, and evaluate. Reacquire and apply only if the operation, game, player, and revision still match. Discard late/stale responses without duplicating moves or results.
5. Prevent two commands/reconnects from starting competing bot operations. Cancel operations on match removal and preserve recovery ownership from T2.
6. Move result publication outside the critical section only with explicit ownership of the detached final records, completion status, retry policy, and shutdown behavior. Do not introduce untracked `Task.Run` publication.

Publication durability is a separate deliverable if durable result guarantees are required: persist stable event IDs and detached payloads in an outbox before forgetting a completed result; dispatch with retries and idempotent consumers. An in-memory channel does not provide crash durability. Storage, atomic handoff from the in-memory match, infrastructure, and consumer deduplication must be designed together. Do not claim a durable outbox was implemented by only releasing the match lock.

Acceptance:

- A blocked notification or bot HTTP response does not block state reads/disconnect handling for the entire network wait.
- A late bot response cannot mutate a reconnected/replaced/completed match or duplicate a transition.
- Canceled lease waiters do not leak lock entries, split lock identity, or permit concurrent owners.
- Slow-client tests prove bounded buffering and preserved lifecycle ordering.
- Publication failures remain observable; cleanup survives failures; all required queue publications are attempted according to policy. Preserve the existing result-reuse and failure-cleanup tests.

## Secondary work, only after measurement

- `CancelForMatch` / `CancelForPlayer` scan the whole token registry under a global lock. Add indexes or per-match deadline ownership if this appears in contention profiles.
- Match records contain full histories and are serialized for multiple destinations. Stats/rating messages may need only compact summaries, but changing them requires coordinated Lambda/contract changes and consumer tests.
- Error storms can add exception serialization and synchronous console logging costs. Rate-limit repeated identical operational errors if measured; preserve enough detail to diagnose the initial fault.
- Started matches need explicit action/recovery ownership, not a blanket arbitrary TTL that might delete a legitimate long game.

## Validation and rollout

Use AAA tests, braces, existing formatting, and documentation for changed public APIs. Extend existing suites first. Run tests relevant to each PR, with consuming-project tests when shared contracts or engine behavior change.

Baseline and lifecycle commands:

```powershell
dotnet build src/GammonX/GammonX.Server/GammonX.Server.csproj -c Release
dotnet test src/GammonX/GammonX.Server.Tests/GammonX.Server.Tests.csproj --filter "FullyQualifiedName~MatchSessionRepositoryTests|FullyQualifiedName~MatchLobbyCleanupTests|FullyQualifiedName~CancellationTokenServiceTests|FullyQualifiedName~MatchHubTests"
git diff --check
```

For engine changes, run the relevant Engine classes and consuming Server classes. If a shared unique-board API changes, also run relevant Mars consumer tests; this validates compatibility without expanding scope into inference tuning. Do not run the full solution test suite by default.

Repeat the controlled AWS load test with the same image configuration, CPU/memory limits, match variants, bot/human mix, command rate, and game-duration distribution. Record the deployed commit. Test stepped concurrency through and beyond the previous failure threshold, and include late-game/doubles positions and disconnect/reconnect bursts. Distinguish container/process CPU if the two services share a resource limit.

Success criteria:

- Correctness regressions above pass without wall-clock sleeps.
- At the original approximately 100-user workload, CPU has measurable headroom and latency/queue depth remain stable over a sustained run. Report exact workload and measurements; do not promise a new capacity number from local microbenchmarks.
- Allocation per command and payload sizes improve on identical fixtures; no representative variant regresses without an explained tradeoff.
- After clients leave and grace/recovery windows elapse, human connection, match, lobby, deadline, and task counts return to baseline. Shared bot entries are accounted for separately.
- Injected slow/failed client sends, bot responses, and queue publications terminate or recover predictably, without stale timeouts, unbounded loops, duplicate results, or orphaned state.

Keep protocol changes and internal snapshot changes separately revertible. Preserve old-client support through a measured rollout. Capture CPU/GC traces before and after; RAM below its limit does not exclude allocation-driven CPU cost.

## Reference material

- [Microsoft: diagnose high CPU in .NET](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/debug-highcpu)
- [Microsoft: SignalR background services and IHubContext](https://learn.microsoft.com/en-us/aspnet/core/signalr/background-services?view=aspnetcore-10.0)
- [ASP.NET Core 10 Hub source: disposed property access](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/SignalR/server/Core/src/Hub.cs)
