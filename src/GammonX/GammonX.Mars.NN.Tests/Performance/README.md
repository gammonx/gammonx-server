# GammonX Engine and NN Performance Analysis

This folder contains the manual CPU tuning runner and the findings from the
move-sequence evaluation investigation begun on **2026-10-26**. The numbers below are
historical measurements on one workstation, not performance guarantees or CI
thresholds. See the current implementation notes before reproducing an older batch.

## Current batch-only implementation (2026-10-29)

- [INeuralEvalService](../../GammonX.Mars.NN/Services/INeuralEvalService.cs) now owns
  `MaxBatchSize`, `ExtractFeatures` and `PredictBatchAsync`. The optional batch
  interface and the legacy two-ply dispatch path have been removed.
- Two-ply scoring always uses bounded groups of at most
  `min(service.MaxBatchSize, 32)` replies. A blocked opponent is a pass and is
  scored as a **one-row batch**, not an empty batch or infinite score.
- `PredictAsync` remains a supported convenience API for cube/one-ply callers
  and interactive probes. In the hosted service it uses the same batch queue with
  one row and preserves the underlying exception/cancellation, rather than a
  separate single-request implementation.
- [BatchedNeuralEvalService](../../GammonX.Mars.NN/Services/BatchedNeuralEvalService.cs)
  is the hosted FIFO worker implementation. The existing serialized, non-hosted
  [NeuralEvalService](../../GammonX.Mars.NN/Services/NeuralEvalService.cs) also
  implements the full batch contract for its direct callers; it does not restore
  the removed two-ply path.
- The library default **and the current server configuration** are 32 rows.
  The 64-row server limit mentioned in Batch 7 was historical. Worker limits above
  32 are still supported; the per-caller reply cap remains 32.
- `UseExplicitBatching` and `GAMMONX_PERF_EXPLICIT_BATCH` are removed. Setting the
  old environment variable cannot select a legacy path. The obsolete
  `compare-inference.py` A/B runner was removed: comparing two labels on the same
  batch-only code would be misleading.
- [BatchEvaluationTests](../Services/BatchEvaluationTests.cs) replaces the old
  dual-path tests. Real-model comparisons use a one-row reference scorer versus
  grouped scoring; other tests retain history, terminal/pass, lifetime, cap and
  restoration coverage. This is a correctness reference, not a restored
  production legacy mode.

The commands below run the current batch-only implementation. Historical
before/after timings and test counts are retained as experiment records, not
claims about the refactored code. Reproducing the old A/B experiment requires the
matching earlier source revision and model.

Refactoring validation on 2026-10-29 passed **367 NN feature/service tests**,
**21 training/search tests** and **14 performance smoke cases**; the Mars server
Release build also passed. Long-running self-play tests and the complete CPU
parameter sweep were not rerun. These checks validate contracts and benchmark
execution, not a new performance improvement claim.

## Running the CPU experiment manually

### Prerequisites

- Python **3.10+**. The runner uses only the standard library; no pip install is
  needed.
- The **.NET 10 SDK** on `PATH`, with the test project's NuGet packages restored.
- The Backgammon model and its metadata available in the checkout:
  [NeuralNets/Backgammon](../../GammonX.Mars.Server/NeuralNets/Backgammon).
  The test project copies these into its build output.
- A quiet machine. Run only one experiment at a time, without a debugger,
  coverage collection, other test runs, or edits/builds in the same checkout.
  Allow several minutes and substantial CPU/GC activity.

From the repository root in PowerShell:

```powershell
dotnet restore .\src\GammonX\GammonX.Mars.NN.Tests\GammonX.Mars.NN.Tests.csproj

python .\src\GammonX\GammonX.Mars.NN.Tests\Performance\run-cpu-tuning.py `
    --baseline-threads 16 `
    --output-dir "$env:TEMP\gammonx-cpu-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
```

Use the thread count of the deployment/reference configuration for
`--baseline-threads`; **16 is the value used in the recorded experiment**, not
a universal recommendation.

The repository is discovered relative to the script, so it is also possible to
run from this folder or invoke the script by its absolute path:

```powershell
python .\run-cpu-tuning.py --help
python .\run-cpu-tuning.py --baseline-threads 16
```

| Option | Meaning |
| --- | --- |
| `--baseline-threads N` | Required positive reference thread count; reference maximum batch size is 32. |
| `--repo PATH` | Optional repository root override. By default, discover the checkout above the script, not from the shell's working directory. |
| `--output-dir PATH` | Optional new or empty output directory. Default: a timestamped folder under `TestResults` beside the script, already covered by the repository's ignore rules. |

Paths containing spaces must be quoted. Relative option paths are resolved from
the shell's working directory. Existing non-empty result directories are rejected;
use a new directory for every experiment. The runner does not install dependencies,
restore packages, reuse cached TRX files, or change the parent shell's environment.
Importing the Python module does not run the experiment.

### What the runner does

1. Builds the test project in **Release**, with `--no-restore`, then fingerprints
   the application assemblies, dependency manifest, model and available metadata.
2. Screens Board 1 at batch limit 32: the reference thread count, followed by
   1, 8, 2 and 4 threads, omitting a duplicate reference value.
3. Uses the fastest observed thread count to screen batch limits **8, 32, 16, 64**.
4. Confirms the chosen pair against the reference on **all three boards**,
   alternating which configuration runs first.
5. Repeats Board 2 after the other boards, reversing its initial tuned-first order.
   The Board 2 comparison pools **six raw timed samples per configuration**, not
   the median of two run medians. Boards 1 and 3 have three samples per configuration.
6. Checks that the fingerprint has not changed and writes the final comparison
   and context reports.

With a reference of 16 threads, this is **17 fresh test processes and 51 timed
evaluations**, plus startup, first inference and full-evaluation warm-ups.
This is a **sequential screening experiment**, not a full factorial search or proof
that the selected pair is optimal.
It now compares thread/worker-limit configurations of the batch-only
implementation; increasing a worker limit above 32 does not increase the two-ply
per-caller group cap.

Each process runs
[MeasureCpuThreadAndBatchTuning](./PerformanceTests.CPU.cs).
The runner sets `OMP_NUM_THREADS` and `MKL_NUM_THREADS` before Torch loads, together
with the test's `GAMMONX_PERF_*` configuration variables. The C# test verifies the
requested thread count on **every inference-worker batch during warm-up**.
Inter-op threads are not tuned. There is **no metrics listener in the three timed
evaluations**. Model loading, first inference, warm-up and cleanup are outside
their timed regions.

Cross-process checks require unchanged selected moves, candidate move sets,
root-candidate and prediction counts, model, runtime and inter-op thread settings.
Every candidate's warm-up equity is compared with an absolute tolerance of
**1e-5**. Timed evaluations also assert that the selected move matches warm-up.

### Reports and failures

All outputs are placed below the selected output directory:

| Output | Contents |
| --- | --- |
| `fresh-process/<run>/result.trx` | Original test output, including timings, scores and worker-setting verification. |
| `tuning-summary.csv` | Per-run min/median/max wall time, median process CPU time, managed allocations, GC counts and search counts. |
| `tuning-samples.csv` | Individual measured iterations. |
| `candidate-scores.csv` | All candidate move keys and warm-up scores for each configuration. |
| `confirmation-comparison.csv` | Per-board reference/candidate comparison; `Speedup = default median / tuned median`. Values above 1 favor the candidate. |
| `tuning-context.json` | Successful-run context: selected pair, fingerprints, environment, tolerance, run counts and worker evidence. |

The three raw CSV reports are saved after each successfully validated run.
A failing build/test, invalid TRX output, changed result or changed runtime
fingerprint stops the experiment with a nonzero exit code. Tool failures include
the command's output. Cancellation returns exit code 130. Partial reports are
retained, but **do not treat a partial directory as a completed comparison**.
Rerun into a new directory after resolving the failure.

The Python runner orchestrates the CPU experiment only. To collect the separate
baseline, stage profiles or mixed-load output from the repository root:

```powershell
# Baseline: engine-only, one-ply and two-ply, without stage collection.
dotnet test .\src\GammonX\GammonX.Mars.NN.Tests\GammonX.Mars.NN.Tests.csproj `
    -c Release --no-restore `
    --filter "FullyQualifiedName~PerformanceTests.MeasureEvalPerformance|FullyQualifiedName~PerformanceTests.MeasureEnginePerformance" `
    --logger "trx;LogFileName=baseline.trx" `
    --results-directory .\TestResults\performance-baseline

# Stage profiles: opt-in collection during measured two-ply evaluations.
dotnet test .\src\GammonX\GammonX.Mars.NN.Tests\GammonX.Mars.NN.Tests.csproj `
    -c Release --no-restore `
    --filter "FullyQualifiedName~PerformanceTests.ProfileTwoPlyPerformance" `
    --logger "trx;LogFileName=stages.trx" `
    --results-directory .\TestResults\performance-stages

# Mixed load: two evaluations sharing the worker with single-row probes.
dotnet test .\src\GammonX\GammonX.Mars.NN.Tests\GammonX.Mars.NN.Tests.csproj `
    -c Release --no-restore `
    --filter "FullyQualifiedName~PerformanceTests.MeasureSharedWorkerInference" `
    --logger "trx;LogFileName=mixed.trx" `
    --results-directory .\TestResults\performance-mixed
```

`MIXED` now reports
`evaluations,single_predictions,wall_ms,cpu_ms,single_median_ms,single_p95_ms,single_max_ms`;
`MIXED_SINGLE` reports one latency value. Neither contains the removed
`explicit_batching` flag. `MixedMaxBatchSize` records the worker limit, selected
through `GAMMONX_PERF_BATCH_SIZE` (default 32).

## Workload and measurement rules

The fixtures are defined in
[MockBoardContracts.cs](../Data/MockBoardContracts.cs):

| Fixture | Player | Dice | Unique root candidates |
| --- | --- | --- | ---: |
| Board 1 | Black | Double 2: `[2,2,2,2]` | 131 |
| Board 2 | Black | `[3,2]` | 53 |
| Board 3 | Black | Double 2: `[2,2,2,2]` | 96 |

`Hard` uses one-ply neural evaluation; `Expert` uses two-ply.
[TwoPlySearchEvaluator](../../GammonX.Mars.NN/Services/TwoPlySearchEvaluator.cs)
evaluates every root candidate against **21 distinct opponent dice outcomes**,
weighted to represent all 36 ordered rolls. It scores all unique replies and
selects the opponent's best score for each outcome. There is no candidate limit
in these experiments.

Consequently, work scales approximately as:

```text
root candidates x 21 dice outcomes x average opponent replies
```

An engine-only test measures initial root generation, **not** all opponent
generation inside two-ply. Fast root generation does not establish that move
generation is cheap across the entire search.

[PerformanceTests.cs](./PerformanceTests.cs) fixes the device to CPU,
uses one full warm-up and three timed iterations, and isolates its collection
from parallel test collections. Inference workers are stopped and disposed.
There is no forced GC.

Interpret the measurements carefully:

- Wall time is elapsed request latency. Process CPU time sums work across threads
  and can exceed wall time; very short operations can read as zero CPU time.
- Baseline allocation and GC counters are **process-wide**, including worker
  allocations across `await` continuations. They assume no unrelated test work.
- Allocations are **cumulative managed bytes**, not peak/resident memory, and
  exclude native Torch allocations.
- Stage scopes use synchronous **current-thread allocation deltas** and must
  not span an `await`.
- Stage durations are cumulative elapsed work, not a CPU profile or an exclusive
  partition of request wall time. Worker and producer work can overlap.
- `GenerationClone` is nested inside `MoveGeneration`; do not add them together.
  Queue waits overlap between requests and include channel backpressure.
- `Forward` includes the network forward pass, output transfer to CPU and
  materialization of the output array. It is not pure neural-kernel time.
- Instrumentation can change scheduling and batch occupancy. Keep instrumented
  results separate from uninstrumented baselines and compare within a run.

## Recorded environment

All recorded batches used this workstation and model:

- Intel Core Ultra 7 265H, 16 logical processors.
- Windows 10.0.26200, x64; .NET SDK 10.0.403, runtime .NET 10.0.14.
- TorchSharp CPU package 0.106.0; CPU inference.
- Initial Torch intra-op / inter-op counts: 16 / 16. Inter-op stayed at 16
  throughout the CPU tuning experiment.
- Backgammon model SHA-256:
  `F65C0695C4074E5ED02193E8C4AC2AEFDA15D52FFF9871DED64BD8B24636DF8E`.

The following tables capture different batches, not a single simultaneous run.
Differences between tables are not evidence of an optimization.
Batch 7 records the original comparison with single-position delivery. That
comparison switch is no longer available in the batch-only implementation.

## Batch 1: uninstrumented baseline

Medians of three timed iterations after warm-up, using 16 threads / batch limit 32:

| Board | Engine | One-ply | Two-ply | Two-ply NN requests | Two-ply managed allocations |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 0.743 ms | 36.809 ms | 4,660.399 ms | 104,291 | 2.458 GB |
| 2 | 0.067 ms | 11.115 ms | 994.445 ms | 28,008 | 0.591 GB |
| 3 | 0.733 ms | 17.801 ms | 2,164.606 ms | 61,717 | 1.344 GB |

GB here means decimal gigabytes. Two-ply generated roughly **529-796 times**
as many neural requests as one-ply. Board 1's process CPU / wall-time ratio was
about 10.3, illustrating substantial multithreaded work.

Cold startup was recorded separately: the first service load/start took about
**1.33 s**, and its first inference about **27.8 ms**. Later service startups
in the same process took roughly 3-4 ms. Do not mix those cold/warm startup
observations into steady-state evaluation latency.

The baseline batch passed 24 focused tests.

## Batch 2: stage attribution and actual batching

Medians from three **instrumented** evaluations:

| Board | Move generation | Nested terminal clones | Reply clones | Features + vector | Batch preparation | Forward/output |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 1,128 ms | 661 ms | 58 ms | 201 ms | 89 ms | 2,837 ms |
| 2 | 255 ms | 113 ms | 15 ms | 57 ms | 28 ms | 1,133 ms |
| 3 | 563 ms | 292 ms | 36 ms | 132 ms | 72 ms | 2,656 ms |

Forward/output was the largest measured duration component. Feature calculation
was comparatively small. Move generation accounted for **65-68% of managed
allocations** in the instrumented evaluations.

| Board | Generation calls | Raw terminal snapshots | Retained sequences | Raw / retained | Average batch | Full batches |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 2,752 | 674,385 | 104,422 | 6.46 | 15.21 | 21.1% |
| 2 | 1,114 | 174,635 | 28,061 | 6.22 | 11.78 | 9.2% |
| 3 | 2,017 | 353,862 | 61,813 | 5.72 | 13.47 | 13.1% |

Search counts include root and opponent generation **per evaluation**; retained
sequences are summed over generation calls, not globally unique across the whole
search. Batch averages and percentages pool the three measured iterations, with
maximum batch size 32.

[BoardBaseServiceImpl](../../GammonX.Engine/Services/boards/BoardBaseServiceImpl.cs)
originally created full terminal-board snapshots **before deduplication**.
Backgammon cloning also copies history. On Board 1, terminal snapshot cloning alone allocated
approximately **1.17 GB**, within about **1.67 GB** attributed to move generation.
The evaluator additionally clones each retained opponent reply.

The worker drains currently queued predictions immediately; it does not wait
for a full batch. Underfilled batches are therefore real, but some arise at
roll boundaries. Adding a queue delay or parallelizing more work is not
automatically an improvement.

Validation passed 31 evaluation/profile/metrics tests and 565 engine regression
tests. Instrumented selected moves matched uninstrumented warm-up; stage counts,
prediction counts and batch membership reconciled.

### Does metric capture affect production?

[EvaluationMetrics](../../GammonX.Engine/Diagnostics/EvaluationMetrics.cs) is
**runtime opt-in, not compiled out of production**:

- Without a metrics subscriber, scopes do not read timestamps or allocation
  counters, histogram calls have no consumer, and queue timing returns zero.
- The disabled path still has helper calls, enabled checks and no-op disposal,
  one-time meter initialization, and an extra timestamp field per inference
  request. This overhead has **not** been quantified as zero.
- Creating the test-only
  [EvaluationMetricsCollector](./EvaluationMetricsCollector.cs)
  enables synchronous callbacks, aggregation and locking. Another `MeterListener`
  or configured metrics integration could enable these instruments in production
  too.
- Collection is process-wide. Disposing the collector disables capture only if
  no other subscriber remains.

## Batch 3: CPU thread and batch-limit tuning

### Rejected attempt and corrected method

An initial in-process sweep changed Torch's thread setting at runtime. The
inference-worker check caught **requested 8, observed 4**. Those measurements were
rejected; checking the caller's thread setting alone was insufficient.

The accepted experiment used the fresh-process method described above. Native
thread settings were applied before Torch loaded and verified throughout warm-up.
The timed evaluations had no metrics collector.

### Board 1 screening results

Thread sweep at batch limit 32:

| Threads | Median wall time |
| ---: | ---: |
| 16 (reference) | 3,258.598 ms |
| 1 | 7,954.512 ms |
| 8 | **2,881.269 ms** |
| 2 | 3,654.447 ms |
| 4 | 3,118.577 ms |

Batch-limit sweep at 8 threads:

| Maximum batch size | Median wall time |
| ---: | ---: |
| 8 | 5,349.751 ms |
| 32 | **3,016.590 ms** |
| 16 | 3,502.623 ms |
| 64 | 3,212.439 ms |

The final screening selected **8 threads / batch limit 32**. One thread was about
2.4 times slower than the 16-thread reference; smaller batch limits hurt latency.
An earlier sweep briefly favored batch limit 64, but this was not consistent
across reruns.

### Confirmation against 16 threads / batch limit 32

| Board | Reference median | Candidate median (8 / 32) | Speedup | Wall-time reduction |
| --- | ---: | ---: | ---: | ---: |
| 1 | 3,310.992 ms | 3,238.096 ms | 1.023x | 2.2% |
| 2 | 975.938 ms | 900.327 ms | 1.084x | 7.7% |
| 3 | 1,973.393 ms | 1,868.292 ms | 1.056x | 5.3% |

Board 2 pools six samples per configuration. Its reversed-order repeat measured
**1,006.986 ms reference vs 1,022.038 ms candidate**, so the candidate was slightly
slower in that repeat. Small gains should not be treated as robust, particularly
on a shared workstation.

The experiment validated:

- 17 passing isolated runs and 51 timed samples.
- 1,845 exported candidate scores; maximum absolute score difference approximately
  **8.4e-8**, below the 1e-5 tolerance.
- Unchanged selected moves and prediction counts.
- A separate 32-test performance/diagnostics/two-ply regression run passed.

**Decision: retain production defaults.** The measured gains were modest and
inconsistent. These are CPU, single-request latency experiments, not GPU results,
concurrent-request throughput tests, or evidence for another deployment's optimum.

## Batch 4: deduplicating terminal positions before cloning

On **2026-10-27**, the unique-generation path was changed to check terminal
positions against retained snapshots **before allocating a move sequence or deep
board/history clone**. The first occurrence is still cloned and retained; duplicate
occurrences are not.

The implementation reuses `HashSet<IBoardModel>` and the existing board equality,
including home bars, bear-off counts, pins and cube state. It does not introduce
a hash-only shortcut or a second definition of position identity. The mutable
working board is used for lookup only; stored snapshots are never subsequently
modified. The raw traversal and its order remain unchanged, and the same dice
filter is still applied after deduplication. Non-unique move APIs keep their
original behavior.

### Allocation result

Fresh before/after runs used Release, CPU, 16 intra-op/native threads, 16 inter-op
threads, maximum batch size 32, one warm-up and three timed iterations. Each
board also ran in a separate test process to export every root candidate score.
These settings did not enable the marginal CPU tuning candidate from Batch 3.

The separate per-board process measurements produced:

| Board | Two-ply allocations before | After | Reduction | Terminal clones before | After |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 2.458 GB | 1.329 GB | 45.9% | 674,385 | 104,422 |
| 2 | 0.591 GB | 0.341 GB | 42.3% | 174,635 | 28,061 |
| 3 | 1.344 GB | 0.768 GB | 42.8% | 353,862 | 61,813 |

Allocations are cumulative managed bytes per evaluation, not resident memory.
Clone counts come from the stage profile and include root and opponent generation.
They fell by **82.5-84.5%**, while raw terminal-path and prediction counts stayed
unchanged. For Board 1 this avoids **569,963** full board/history snapshots per
two-ply evaluation.

The fresh engine-only baseline also showed managed allocation reductions of
**67.1% / 38.1% / 66.3%** for Boards 1 / 2 / 3.

### Timing caveat

The per-board process measurements were:

| Board | Two-ply median before | After |
| --- | ---: | ---: |
| 1 | 5,272 ms | 5,307 ms |
| 2 | 1,615 ms | 1,407 ms |
| 3 | 3,631 ms | 3,260 ms |

These are sequential before/after measurements on a shared workstation, not
randomized repetitions. A separate combined run had mixed timing changes,
including slower results on Boards 1 and 2. **The verified improvement is the
allocation reduction; a consistent end-to-end latency speedup has not been
established.** Root generation got faster in the combined run, but inference,
batch scheduling and machine load still influence total evaluation latency.

### Correctness and metric semantics

- An independent copy-per-branch reference traversal retains every terminal
  position and then performs the old post-traversal deduplication and dice filter.
  [UniqueMoveSequenceTests](../../GammonX.Engine.Tests/UniqueMoveSequenceTests.cs)
  compares exact first representatives, order, dice used, unique end positions
  and restored board/history state for all five variants, both perspectives,
  all 21 dice outcomes on starting and reached positions, plus targeted edge cases.
- All **582 engine regression tests** passed, including 17 new equivalence cases.
  The existing 32 performance/diagnostic/two-ply cases also passed.
- All **280 root candidate scores** matched the pre-change results within a maximum
  absolute difference of **7.5e-8**, below the existing 1e-5 tolerance. Selected
  moves, root counts and prediction counts were unchanged.
- A focused metric regression also covers a board with 100 prior roll events:
  duplicate terminals must not trigger deep clones, and input history must remain
  unchanged.
- `PROFILE_SEARCH.RawSequences` still counts **all terminal paths visited**,
  not snapshots allocated. `GenerationClone.Calls` now counts **only first-seen
  terminal snapshots before dice filtering**. For the three performance fixtures,
  all retained sequences use the full roll, so clone counts equal their reported
  retained-sequence counts.

### Remaining work

At the end of Batch 4, full board/history copies still existed for each first-seen
terminal position, and opponent reply clones were unchanged. Batch 5 below removes
history from the deduplication snapshots only. All legal paths are still traversed.
Further reductions would need careful treatment of lighter state representations
or search-state reuse without duplicating variant equality or changing dice
legality.

## Batch 5: history-free position snapshots

On **2026-10-27**, built-in boards gained an internal equality-only snapshot with
independent position arrays and an empty, independent history. The unique-generation
path now uses this snapshot for first-seen terminal positions.

[BoardBaseImpl](../../GammonX.Engine/Models/impls/BoardBaseImpl.cs) shares the
variant-state copy factory between full clones and position snapshots:

- Ordinary `ICloneable.Clone()` / `DeepClone()` still deep-copy the entire history.
- Equality-only snapshots copy fields, bear-off counts, home bars, pins and cube
  state, but do not clone any history events.
- The snapshot retains the concrete variant type and uses the existing equality
  and hash implementations. It is not a new hash-only key.
- Only built-in **unique-generation deduplication** uses the history-free path.
  Non-unique generation and other board implementations retain their full-clone
  behavior.
- **Opponent reply clones remain full deep clones.** Inference consumes
  history-derived features such as roll/turn count; dropping history there would
  change evaluation inputs. Position snapshots must not be used as evaluation
  boards.

### Allocation result

Fresh before/after runs used the same Release, CPU, 16 intra-op/native threads,
16 inter-op threads and maximum batch size 32. Each per-board process performed
one full warm-up and three timed iterations. The "before" already includes the
Batch 4 early-deduplication optimization.

| Board | Two-ply allocations before | After | Additional reduction |
| --- | ---: | ---: | ---: |
| 1 | 1.330 GB | 1.216 GB | 8.5% |
| 2 | 0.340 GB | 0.319 GB | 6.4% |
| 3 | 0.768 GB | 0.703 GB | 8.6% |

These are cumulative managed allocations per evaluation, not resident memory.
Board 1 saves approximately **113 MB more per two-ply evaluation** in this batch.
The combined engine-only baseline also showed allocation reductions of
**11.3% / 19.4% / 10.7%** for Boards 1 / 2 / 3.

The instrumented post-change snapshot stage measured:

| Board | Position snapshots | Snapshot-stage managed bytes |
| --- | ---: | ---: |
| 1 | 104,422 | 59,311,696 |
| 2 | 28,061 | 15,938,648 |
| 3 | 61,813 | 35,109,784 |

This is **568 bytes per Backgammon snapshot** for these measurements, independent
of copied history because no history events are copied. It still includes a full
board object, copied fields, delegates and empty history objects; this is not a
packed or minimal position key. Snapshot counts, raw path counts and prediction
counts did not change.

### Timing caveat

The separate per-board process measurements were:

| Board | Two-ply median before | After |
| --- | ---: | ---: |
| 1 | 3,786 ms | 4,018 ms |
| 2 | 1,696 ms | 1,474 ms |
| 3 | 2,880 ms | 2,498 ms |

As in Batch 4, before/after measurements were sequential on a shared workstation,
not randomized repetitions. The separate combined run was slower after the change
on all three boards. **This establishes another allocation reduction, not a
consistent end-to-end latency speedup.** No production thread or batch defaults
were changed.

### Correctness

- **597 engine regression tests passed**, including the previous independent
  traversal-equivalence tests and **15 new snapshot tests** across all five variants.
- [BoardPositionSnapshotTests](../../GammonX.Engine.Tests/BoardPositionSnapshotTests.cs)
  verifies symmetric equality and matching hashes, copied variant state,
  independent arrays/history and preserved full-clone behavior. A deliberately
  non-cloneable history event proves unique generation never tries to clone it,
  while ordinary deep cloning still does.
- The snapshot allocation test compares repeated snapshots before and after
  adding 100 roll events; allocated bytes must be identical.
- All **33 performance/diagnostic/two-ply tests passed**.
- All **280 root candidate scores** matched the pre-change values within a
  maximum absolute difference of **5.4e-8**, below the existing 1e-5 tolerance.
  Selected moves, root candidates, raw terminal paths and NN request counts
  remained unchanged.

At the end of Batch 5, remaining costs included visiting every legal path,
constructing full position objects for first-seen terminals, full opponent reply
clones, feature extraction, task/channel scheduling and inference. Batch 6 below
avoids revisiting equivalent doubles subtrees. Any attempt to reuse mutable reply
boards must still respect asynchronous evaluation lifetimes and retain
history-dependent features.

## Batch 6: reusing equivalent intermediate doubles states

On **2026-10-27**, unique-generation traversal gained per-call sets of previously
expanded intermediate positions for built-in boards with **more than two identical
dice** (normally a four-move double).

For identical dice, the remaining dice count identifies the remaining roll
multiset. At a fixed player perspective and die value, an equal position with the
same number of dice left therefore has the same legal continuation subtree.
The first depth-first visit fully expands that subtree; later visits skip it.
This is **exact state deduplication**, not selective search or a candidate limit.

Important boundaries:

- The sets are partitioned by remaining dice count, so states with different dice
  still to play are never merged. They are local to one generation call, not a
  cross-request or cross-roll cache.
- Position identity uses the existing full equality and hash implementations.
  Only independent, history-free position snapshots are stored; the mutable
  working board is used for lookup only.
- Built-in variant legality was checked to depend on position state rather than
  the preceding move history. The first path is retained, preserving exact move
  representatives, order and dice usage.
- Mixed-roll searches, non-unique APIs and other board implementations keep
  their previous traversal. The eligibility check does not allocate a capturing
  delegate; Board 2's non-double root-generation allocations remained exactly
  unchanged in the fresh comparison.
- This optimization is active for eligible production searches without a metrics
  subscriber. Its diagnostic measurements remain opt-in.
- Neural evaluation and opponent reply histories are unchanged. Board 2 still
  benefits during Expert evaluation because opponent dice outcomes include doubles,
  even though its root roll is not a double.

### Traversal work

The fresh instrumented before/after comparison measured:

| Board | Terminal visits before | After | Reduction | Repeated intermediate states skipped |
| --- | ---: | ---: | ---: | ---: |
| 1 | 674,385 | 247,078 | 63.4% | 40,657 |
| 2 | 174,635 | 67,030 | 61.6% | 12,462 |
| 3 | 353,862 | 141,543 | 60.0% | 23,584 |

These are totals across root and opponent generation for one full two-ply
evaluation. A skipped intermediate state can avoid many terminal visits, so the
last column is not the number of avoided terminals. The retained unique sequences
were unchanged at **104,422 / 28,061 / 61,813**, including root sequences.

Cache bookkeeping has a real cost. It created **43,715 / 13,121 / 26,879**
intermediate snapshots, with approximately **27.5 / 8.2 / 16.9 MB** attributed
to the snapshot-and-cache-insertion stage, including hash-set growth. The net
generation allocation totals, already including this cost, fell from:

- Board 1: **429.3 MB to 268.7 MB**.
- Board 2: **115.7 MB to 75.5 MB**.
- Board 3: **238.1 MB to 159.6 MB**.

Instrumented cumulative move-generation time decreased from **254 to 191 ms**,
**85 to 70 ms**, and **150 to 111 ms**, respectively. These stage measurements
are not exclusive request-latency partitions.

### Total allocations and latency

Fresh per-board processes used Release, CPU, 16 intra-op/native threads, 16 inter-op
threads and maximum batch size 32, with one full warm-up and three timed iterations.
The "before" already includes all previous batches.

| Board | Two-ply allocations before | After | Additional reduction |
| --- | ---: | ---: | ---: |
| 1 | 1.216 GB | 1.056 GB | 13.2% |
| 2 | 0.318 GB | 0.278 GB | 12.6% |
| 3 | 0.703 GB | 0.624 GB | 11.2% |

Allocations are cumulative managed bytes, not resident memory. Board 1 saves
approximately **160 MB more per two-ply evaluation** in this batch.

The combined root-generation baseline showed approximately **45% fewer allocated
bytes** for both doubles fixtures, with medians of **0.383 to 0.219 ms** on Board 1
and **0.290 to 0.173 ms** on Board 3. Board 2's non-double root allocation stayed
at **98,544 bytes**.

Per-board process two-ply medians were:

| Board | Before | After |
| --- | ---: | ---: |
| 1 | 4,097 ms | 3,728 ms |
| 2 | 1,384 ms | 1,062 ms |
| 3 | 2,503 ms | 2,336 ms |

Those per-board runs improved, but the separate combined run was slightly slower
on Boards 1 and 3. The before/after runs were sequential on a shared workstation,
not randomized repetitions. **Reduced traversal work and allocations are verified;
a consistent end-to-end latency speedup is still not established.** Production
thread and batch-size defaults remain unchanged.

### Correctness and metric changes

- **597 engine regression tests passed**, including the independent full-traversal
  reference across all five variants, both perspectives and all 21 dice outcomes
  on starting and reached positions, plus dice-rule and restoration edge cases.
- All **34 performance/diagnostic/two-ply tests passed**. A new focused test checks
  that the state reuse occurs for unique doubles generation, but not mixed rolls
  or non-unique generation.
- All **280 root candidate scores** matched before/after within a maximum absolute
  difference of **6.8e-8**, below the existing 1e-5 tolerance. Selected moves,
  representatives and NN request counts stayed unchanged.
- `GenerationClone` still measures first-seen terminal position snapshots.
  The new `TraversalSnapshot` stage measures intermediate snapshots and their
  insertion into the local state sets. **Both are nested inside `MoveGeneration`**;
  do not sum them with their parent.
- The new counter `gammonx.evaluation.traversal.reused` counts skipped repeated
  intermediate states. `PROFILE_SEARCH` now appends a `reused_states` column:
  `board,workload,iteration,generation_calls,raw_sequences,unique_sequences,reused_states`.
- `raw_sequences` now counts terminal paths actually visited after subtree reuse,
  not the terminal count that a full enumeration would have produced. A decrease
  is expected and must not be interpreted as missing unique candidates.

The largest remaining inference workload is unchanged: all unique reply positions
are still scored. Further work should distinguish inference/task scheduling costs
from generation costs rather than infer end-to-end speed from allocation reductions.

## Batch 7: explicit bounded neural inference groups

On **2026-10-28**, the two-ply evaluator gained explicit prediction groups through
an optional internal batch capability. At that time, other implementations kept
their single-position path; the current contract unifies these operations in
[INeuralEvalService](../../GammonX.Mars.NN/Services/INeuralEvalService.cs).
The following tables describe that historical A/B experiment, not a currently
selectable legacy mode. Its library worker default was 32, while the server then
overrode the worker limit to 64; the current server setting is 32.

### Execution and lifetime rules

For each opponent dice outcome, replies are handled in original order in chunks
of at most **32**, or the service's configured batch limit if smaller:

1. Make full reply-board clones, apply the moves and retain the complete histories.
2. Score terminal positions directly; calculate the normal features for the
   remaining positions and extract independent feature arrays.
3. Submit the feature arrays as one bounded FIFO queue item with one completion
   source for the group, rather than one per position.
4. The **same single inference worker** combines whole queue items that fit within
   the worker's row limit. It does not split a group or exceed that limit. Result rows are
   mapped back to their original inputs; scalar outputs retain the existing
   five-element normalization.
5. After scoring finishes, restore the reply boards in `finally`. The evaluator
   awaits one chunk before submitting the next, retaining at most one chunk of
   reply clones at a time and allowing other queued callers between chunks.

The worker does not make parallel model calls or introduce queue delays.
History-dependent features, dice probabilities, terminal equity, candidate sets
and perspective conversion are unchanged. No mutable board is shared with the
worker; it receives feature arrays that remain unchanged until completion.

Forward errors fault the affected requests rather than returning defaults.
Shutdown and linked worker cancellation finish pending requests with failure or
cancellation, including callers waiting for channel capacity. Work submitted
before startup or after shutdown is rejected explicitly. This does not add
per-prediction cancellation to the existing public API.

The queue remains bounded by **queue items**, not feature rows. Explicit items
can contain up to 32 rows at the default setting, so the worst-case queued feature
storage under many callers is larger than when every item held one row.
The evaluator's one-outstanding-chunk-per-request rule limits prefetching, but
deployment-scale concurrency and native/peak memory still merit separate testing.

### Alternating comparison method

Both paths were built into the same binaries. Three rounds alternated baseline
and candidate order by round and board, using a fresh test process for every
configuration. Each run performed a full warm-up and three timed iterations.
This yielded **18 configuration runs and 54 timed evaluations**: nine timed
samples per path per board.

Settings: Release, CPU, `OMP_NUM_THREADS=MKL_NUM_THREADS=16`, 16 inter-op threads
and maximum batch size 32. Neither timed path had a stage-metrics subscriber.
Every run exported all candidate scores, checked its worker thread settings during
warm-up, and was compared against a reference for that board. Model and application
fingerprints stayed fixed throughout the paired experiment.

### Latency, CPU and allocation results

The medians below pool the nine individual samples for each path, rather than
taking the median of run medians:

| Board | Single-request delivery | Explicit groups | Speedup | Latency reduction |
| --- | ---: | ---: | ---: | ---: |
| 1 | 4,727.750 ms | 2,773.901 ms | 1.704x | 41.3% |
| 2 | 1,410.334 ms | 731.375 ms | 1.928x | 48.1% |
| 3 | 2,654.000 ms | 1,743.542 ms | 1.522x | 34.3% |

The explicit path had a lower median in **every board/round pair**, unlike the
previous smaller latency changes. There was still substantial spread on the
shared workstation; for example, Board 1 ranged from 3,017 to 5,923 ms on the
legacy path and 2,409 to 4,247 ms with explicit groups. These observations support
enablement on this workload, not a universal deployment guarantee.

| Board | Managed allocations, single delivery | Explicit groups | Reduction | Median process CPU reduction |
| --- | ---: | ---: | ---: | ---: |
| 1 | 1,054,155,544 bytes | 947,309,240 bytes | 10.1% | 44.4% |
| 2 | 278,008,160 bytes | 248,568,240 bytes | 10.6% | 51.6% |
| 3 | 623,237,064 bytes | 559,059,840 bytes | 10.3% | 35.2% |

Allocations remain cumulative managed bytes, not peak memory or native Torch
allocation totals. The reduction reflects fewer per-position tasks, completion
sources and queue operations; board generation and the number of neural positions
scored are unchanged.

### Separate instrumented batching evidence

Three stage-profile iterations per board/path were collected separately from
the latency comparison:

| Board | Queue items before -> after | Median forward passes before -> after | Average actual batch before -> after |
| --- | ---: | ---: | ---: |
| 1 | 104,291 -> 4,376 | 6,616 -> 4,376 | 15.71 -> 23.83 |
| 2 | 28,008 -> 1,423 | 2,359 -> 1,423 | 11.74 -> 19.68 |
| 3 | 61,717 -> 2,684 | 4,504 -> 2,684 | 13.88 -> 22.99 |

Queue-item counts are per evaluation. Actual batch averages pool the three
instrumented iterations; full batches increased from **22.9% to 39.2%** on Board 1,
**10.9% to 21.8%** on Board 2 and **14.1% to 24.9%** on Board 3. Partial chunks at
roll boundaries remain, so average batches are not expected to reach 32.

Batch preparation still allocates nearly the same total flat feature storage,
because it processes the same number of feature values. This batch does **not**
establish a benefit from pooling or reusing those buffers.

The new counter `gammonx.evaluation.inference.requests` records successfully
queued items, distinct from predicted positions. `PROFILE_INFERENCE` adds a
separate report line with columns:

```text
board,workload,iteration,queue_requests,predictions,forward_passes,average_batch_size
```

`PROFILE_BATCH` still reports row counts, and queue-wait measurements still count
each predicted row even when several share one queue item. Search-report columns
and traversal semantics are unchanged.

### Shared-worker fairness and mixed-load latency

Three fresh processes per path ran **two simultaneous full Board 1 Expert
evaluations**, plus up to 100 serial single-prediction probes with a 5 ms delay
between probes. Probes started only while evaluation work was active; all three
runs completed 100 probes per path. There were no stage subscribers in these runs.

| Metric | Single-request delivery | Explicit groups |
| --- | ---: | ---: |
| Median time for both evaluations | 7,346.953 ms | 5,409.180 ms |
| Pooled single-probe median | 3.1285 ms | 1.9171 ms |
| Pooled single-probe p95, nearest rank | 37.7175 ms | 13.1412 ms |
| Worst probe | 904.4044 ms | 33.1296 ms |

Pooled probe percentiles use 300 samples per path. One paired round had a worse
single-probe p95 with explicit groups (**19.34 vs 8.52 ms**); the other two favored
groups. Therefore the result is an overall improvement for this tested load,
not proof that no individual request or deployment load can regress.

A deterministic worker test additionally holds one group in flight, enqueues a
single, then releases the group. It verifies that the queued single is served
before the large caller's next chunk, independently of timing measurements.

### Historical 64-row hosted-worker configuration

The server then configured a 64-row worker limit, rather than relying on the
library's 32-row default. An initial prototype also allowed **64 rows per explicit
caller group**. Although it reduced evaluation latency, its pooled mixed-load
single-probe p95 regressed from **5.7440 to 9.0089 ms**. That version was rejected
for default enablement.

The accepted implementation caps **each caller group at 32 rows**, independently
of the worker limit. The worker can still combine whole items into up to 64 rows,
but a caller cannot prefill the queue with a whole roll or monopolize it with
64-row reply groups.

Another three alternating rounds tested the final cap with the existing 64-row
worker limit: **18 fresh configuration processes, 54 timed evaluations and six
mixed-load processes**. These measurements are separate from the 32-row-worker
table, not a direct comparison between worker limits.

| Board | Legacy median, worker 64 | Explicit median, caller cap 32 | Speedup |
| --- | ---: | ---: | ---: |
| 1 | 3,785.719 ms | 3,401.213 ms | 1.113x |
| 2 | 1,408.111 ms | 818.598 ms | 1.720x |
| 3 | 2,840.946 ms | 1,734.423 ms | 1.638x |

Every board/round pair had a lower explicit-path median. Managed allocations
remained about 10% lower. Pooled mixed-load single-probe median/p95 were
**2.0253 / 5.1417 ms legacy** versus **1.4591 / 3.5612 ms explicit**, with 300
probes per path. Median time for both simultaneous evaluations fell from
**5,713.917 to 4,364.474 ms**.

The maximum probe was slightly worse (**18.7964 vs 14.6464 ms**), and one round's
p95 was slightly worse (**4.2119 vs 3.9298 ms**). The decision is based on repeated
evaluation gains and improved pooled tail latency, not a claim that every request
improves. Larger production concurrency and native/peak memory remain unmeasured.
Correctness comparisons with actual model weights cover all five variants at
both worker limits, and a regression asserts that reply groups remain capped at
32 when the service advertises 64.

### Correctness and enablement decision

- **74 focused evaluation/performance/diagnostics tests passed**, including real
  model comparisons for all five game variants, terminal and pass replies,
  history-feature equivalence, awaited board lifetimes and restoration on errors.
- Single-output `[N]` and `[N,1]` and five-output `[N,5]` mappings are covered,
  including mixed single/group callers and partial groups. Invalid input groups
  and invalid output shapes fail explicitly.
- Forward failures, worker recovery, FIFO chunk fairness, shutdown and cancellation
  of queued/backpressured single and group requests are tested.
- All **597 engine regression tests passed**.
- The alternating runs checked every candidate set and score: 280 candidates
  across the three boards, with maximum absolute cross-run score difference
  **8.0e-8**, below the existing 1e-5 tolerance. Selected moves and the
  **104,291 / 28,008 / 61,717** predictions per evaluation stayed unchanged.

**Decision: enable explicit bounded groups by default for batch-capable services.**
The repeated latency/CPU gains, lower allocations and mixed-load evidence justify
this change, while retaining the legacy path for other implementations and paired
tests. The 32-row caller cap is part of this decision. Thread settings, worker
batch-size defaults and the server's existing 64-row worker configuration are
not changed.

### Measuring the current batch-only path

The historical switch and A/B runner are obsolete. To record a current
configuration with candidate scores, from the repository root in PowerShell:

```powershell
dotnet build .\src\GammonX\GammonX.Mars.NN.Tests\GammonX.Mars.NN.Tests.csproj `
    -c Release --no-restore

$env:OMP_NUM_THREADS = '16'
$env:MKL_NUM_THREADS = '16'
$env:GAMMONX_PERF_THREADS = '16'
$env:GAMMONX_PERF_BATCH_SIZE = '32'
$env:GAMMONX_PERF_BOARD = '1'
$env:GAMMONX_PERF_PHASE = 'ConfirmDefault'

dotnet test .\src\GammonX\GammonX.Mars.NN.Tests\GammonX.Mars.NN.Tests.csproj `
    -c Release --no-build --no-restore `
    --filter "FullyQualifiedName=GammonX.Mars.NN.Tests.Performance.PerformanceTests.MeasureCpuThreadAndBatchTuning" `
    --logger "trx;LogFileName=inference.trx" `
    --results-directory ".\TestResults\inference-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
if ($LASTEXITCODE -ne 0) { throw "Inference measurement failed" }
```

Run this in a fresh PowerShell process or restore any pre-existing environment
values afterward. This is a measurement of one configuration, not a legacy/batch
comparison. Use new result directories for repeat runs.
Use `MeasureSharedWorkerInference` as the test-name filter for mixed-load output,
and `ProfileTwoPlyPerformance` for separate instrumented batch statistics.
Set `GAMMONX_PERF_BATCH_SIZE=64` to test an alternative worker limit;
reply groups remain capped at 32. The mixed-load benchmark reads this
setting too, while the general stage-profile cases use their fixed 32-row worker.

## Further optimization constraints

Any further implementation must preserve board restoration, legal dice usage,
all unique end states and evaluation results rather than prune candidates
implicitly.

Other constraints already identified:

- Duplicate adjacent dice values are already skipped in the recursion. Batch 6
  additionally avoids re-expanding equal intermediate states for unique doubles
  searches; mixed-roll traversal keeps its previous behavior.
- `maxCandidates` currently limits the **first generated candidates**, not the
  best one-ply candidates. Lowering it changes search behavior.
- Selective two-ply search already has prior art in
  [SelfPlayRunner](../../GammonX.Mars.Training/Generator/SelfPlayRunner.cs), but
  pruning candidates is a deliberate strength/behavior tradeoff, not a transparent
  performance optimization.
- Baseline and stage collection established likely targets; they do not replace
  native-capable CPU/allocation profiling if a finer breakdown inside LibTorch
  or cloning is needed.