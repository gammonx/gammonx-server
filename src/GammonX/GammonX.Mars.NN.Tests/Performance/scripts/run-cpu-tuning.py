"""Screen CPU inference settings using one fresh .NET test process per run.

Uses only the Python standard library. Run with --help for prerequisites and CLI
options; importing this module does not build or execute any benchmarks.
"""

import argparse
import csv
import hashlib
import json
import math
import os
import re
import shutil
import statistics
import subprocess
import sys
import xml.etree.ElementTree as ET
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path


PROJECT_PATH = Path(
    "src", "GammonX", "GammonX.Mars.NN.Tests", "GammonX.Mars.NN.Tests.csproj"
)
ASSEMBLY_PATH = Path(
    "src", "GammonX", "artifacts", "bin", "GammonX.Mars.NN.Tests",
    "release", "GammonX.Mars.NN.Tests.dll",
)
TEST_FILTER = (
    "FullyQualifiedName="
    "GammonX.Mars.NN.Tests.Performance.PerformanceTests.MeasureCpuThreadAndBatchTuning"
)
TRX_NAMESPACE = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
SCORE_TOLERANCE = 1e-5
MEASUREMENT_ITERATIONS = 3
SAMPLE_FIELDS = (
    "Phase", "Board", "Workload", "Threads", "MaxBatchSize", "Iteration",
    "RootCandidates", "WallMs", "CpuMs", "AllocatedBytes", "Gen0", "Gen1",
    "Gen2", "Predictions",
)
SUMMARY_FIELDS = (
    "Phase", "Board", "Workload", "Threads", "MaxBatchSize", "RootCandidates",
    "MinMs", "MedianMs", "MaxMs", "MedianCpuMs", "MedianAllocatedBytes",
    "Gen0Total", "Gen1Total", "Gen2Total", "PredictionsPerIteration",
)
CANDIDATE_FIELDS = (
    "Phase", "Board", "Threads", "MaxBatchSize", "MoveKey", "Score",
)
SELECTION_FIELDS = ("Phase", "Board", "Threads", "MaxBatchSize", "MoveKey")


class BenchmarkError(RuntimeError):
    """A failed benchmark or invalid result that must stop the experiment."""


@dataclass
class BenchmarkResult:
    """Validated test output; raw CSV values remain strings for lossless export."""

    run: str
    summary: dict[str, str]
    samples: list[dict[str, str]]
    candidates: list[dict[str, str]]
    scores: dict[str, float]
    selection: str
    context: dict[str, str]
    worker: str
    score_delta: float = 0.0

    @property
    def median_ms(self) -> float:
        return float(self.summary["MedianMs"])


def positive_int(text: str) -> int:
    value = int(text)
    if value <= 0:
        raise argparse.ArgumentTypeError("must be a positive integer")
    return value


def parse_arguments(argv: Sequence[str] | None = None) -> argparse.Namespace:
    """Resolve the checkout from the script, not the caller's working directory."""
    script = Path(__file__).resolve()
    default_repo = next(
        (parent for parent in script.parents if (parent / PROJECT_PATH).is_file()),
        None,
    )
    parser = argparse.ArgumentParser(
        description=__doc__,
        epilog=(
            "Requires Python 3.10+, the .NET 10 SDK, restored NuGet packages and "
            "the Backgammon model. Builds Release with --no-restore. Runs "
            "sequentially; allow several minutes. See README.md for methodology."
        ),
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--repo", type=Path, default=default_repo,
        help="Repository root (default: discover above this script).",
    )
    parser.add_argument(
        "--baseline-threads", type=positive_int, required=True,
        help="Thread count of the deployment/reference configuration, e.g. 16.",
    )
    parser.add_argument(
        "--output-dir", type=Path,
        default=script.parent / "TestResults" / datetime.now().strftime("%Y%m%d-%H%M%S-%f"),
        help="New or empty results directory (default: TestResults/<timestamp> beside script).",
    )
    args = parser.parse_args(argv)
    if args.repo is None:
        parser.error("Cannot locate the repository; specify --repo.")
    args.repo = args.repo.expanduser().resolve()
    args.output_dir = args.output_dir.expanduser().resolve()
    if not (args.repo / PROJECT_PATH).is_file():
        parser.error(f"Test project not found below --repo: {args.repo / PROJECT_PATH}")
    if args.output_dir.exists() and (
        not args.output_dir.is_dir() or any(args.output_dir.iterdir())
    ):
        parser.error("--output-dir must be new or empty; previous results are not overwritten.")
    if shutil.which("dotnet") is None:
        parser.error("dotnet was not found on PATH; install the .NET 10 SDK.")
    return args


def execute(command: list[str], repo: Path, env: dict[str, str] | None = None) -> None:
    """Run without a shell and include complete tool output on failure."""
    completed = subprocess.run(
        command, cwd=repo, env=env, capture_output=True, text=True,
        encoding="utf-8", errors="replace", check=False,
    )
    if completed.returncode:
        raise BenchmarkError(
            f"Command failed ({completed.returncode}): {subprocess.list2cmdline(command)}\n"
            f"{completed.stdout}\n{completed.stderr}"
        )


def runtime_fingerprint(assembly: Path) -> dict[str, str]:
    """Detect changes to application binaries, dependency manifest and model."""
    model = assembly.parent / "Data" / "NeuralNets" / "Backgammon" / "training_net.dat"
    paths = sorted(assembly.parent.glob("GammonX*.dll"))
    paths.extend([assembly.with_suffix(".deps.json"), model])
    metadata = model.with_suffix(".dat.meta.json")
    if metadata.exists():
        paths.append(metadata)
    return {
        str(path.relative_to(assembly.parent)): hashlib.sha256(path.read_bytes()).hexdigest()
        for path in paths
    }


def save_csv(
    path: Path,
    rows: Sequence[Mapping[str, object]],
    fields: Sequence[str] | None = None,
) -> None:
    """Use UTF-8 with BOM so CSV reports also open cleanly in Excel."""
    if not rows:
        raise BenchmarkError(f"No rows to write: {path}")
    with path.open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=fields or list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)


def records(
    lines: list[str], prefix: str, fields: Sequence[str],
) -> list[dict[str, str]]:
    values = [
        next(csv.reader([line], strict=True))[1:]
        for line in lines if line.startswith(prefix + ",")
    ]
    if any(len(value) != len(fields) for value in values):
        raise BenchmarkError(f"Invalid columns for {prefix}")
    return [dict(zip(fields, value)) for value in values]


def single_line(lines: list[str], prefix: str) -> str:
    matches = [line for line in lines if line.startswith(prefix)]
    if len(matches) != 1:
        raise BenchmarkError(f"Expected exactly one {prefix!r} line; found {len(matches)}.")
    return matches[0][len(prefix):]


def read_result(
    path: Path, phase: str, board: int, threads: int, batch_size: int,
) -> BenchmarkResult:
    """Validate the C# test's CSV contract and worker evidence before comparison."""
    tests = ET.parse(path).getroot().findall("t:Results/t:UnitTestResult", TRX_NAMESPACE)
    if len(tests) != 1 or tests[0].attrib.get("outcome") != "Passed":
        raise BenchmarkError(f"Expected one passing test: {path}")
    stdout = tests[0].findtext("t:Output/t:StdOut", default="", namespaces=TRX_NAMESPACE)
    lines = [line.strip() for line in stdout.splitlines()]
    summaries = records(lines, "TUNING_SUMMARY", SUMMARY_FIELDS)
    samples = records(lines, "TUNING_SAMPLE", SAMPLE_FIELDS)
    candidates = records(lines, "TUNING_CANDIDATE", CANDIDATE_FIELDS)
    selections = records(lines, "TUNING_SELECTION", SELECTION_FIELDS)
    if (
        len(summaries) != 1
        or len(samples) != MEASUREMENT_ITERATIONS
        or len(selections) != 1
        or {row["Iteration"] for row in samples} != {"1", "2", "3"}
    ):
        raise BenchmarkError(f"Unexpected report shape: {path}")

    summary = summaries[0]
    expected_axes = {
        "Phase": phase, "Board": f"Board{board}",
        "Threads": str(threads), "MaxBatchSize": str(batch_size),
    }
    for row in summaries + samples + candidates + selections:
        if any(row[key] != value for key, value in expected_axes.items()):
            raise BenchmarkError(f"Configuration mismatch: {path}")
    scores = {row["MoveKey"]: float(row["Score"]) for row in candidates}
    if (
        not scores
        or len(scores) != len(candidates)
        or len(scores) != int(summary["RootCandidates"])
        or not all(math.isfinite(score) for score in scores.values())
        or selections[0]["MoveKey"] not in scores
    ):
        raise BenchmarkError(f"Missing, duplicate or invalid candidate scores: {path}")
    for sample in samples:
        if (
            sample["Predictions"] != summary["PredictionsPerIteration"]
            or sample["RootCandidates"] != summary["RootCandidates"]
            or sample["Workload"] != "TwoPly"
        ):
            raise BenchmarkError(f"Search counts or workload are inconsistent: {path}")
    wall_times = [float(sample["WallMs"]) for sample in samples]
    if not all(math.isfinite(value) and value > 0 for value in wall_times):
        raise BenchmarkError(f"Invalid wall-clock timings: {path}")
    for field, expected in (
        ("MinMs", min(wall_times)),
        ("MedianMs", statistics.median(wall_times)),
        ("MaxMs", max(wall_times)),
    ):
        if not math.isclose(float(summary[field]), expected, rel_tol=0, abs_tol=0.001):
            raise BenchmarkError(f"{field} does not match the raw samples: {path}")

    worker = single_line(lines, "TuningPhase=")
    verified = re.fullmatch(
        r"(\w+); WorkerTorchThreads=(\d+); WorkerBatchesVerified=(\d+); MaxBatchSize=(\d+)",
        worker,
    )
    if (
        not verified or verified[1] != phase or int(verified[2]) != threads
        or int(verified[3]) <= 0 or int(verified[4]) != batch_size
    ):
        raise BenchmarkError(f"Worker settings not verified: {worker}")
    context = {
        key: single_line(lines, key + "=")
        for key in ("ModelSHA256", "Runtime", "TorchThreads")
    }
    thread_settings = re.fullmatch(r"(\d+); TorchInteropThreads=(\d+)", context["TorchThreads"])
    if not thread_settings or int(thread_settings[1]) != threads:
        raise BenchmarkError(f"Unexpected Torch settings: {context['TorchThreads']}")
    context["TorchInteropThreads"] = thread_settings[2]
    return BenchmarkResult(
        path.parent.name, summary, samples, candidates, scores,
        selections[0]["MoveKey"], context, "TuningPhase=" + worker,
    )


def compare(reference: BenchmarkResult, candidate: BenchmarkResult) -> float:
    """Reject changed search work, moves or scores rather than timing a different task."""
    if reference.selection != candidate.selection:
        raise BenchmarkError("Selected move changed.")
    if reference.scores.keys() != candidate.scores.keys():
        raise BenchmarkError("Candidate move set changed.")
    delta = max(abs(reference.scores[key] - candidate.scores[key]) for key in reference.scores)
    if not math.isfinite(delta) or delta > SCORE_TOLERANCE:
        raise BenchmarkError(f"Candidate score delta {delta} exceeds {SCORE_TOLERANCE}.")
    for field in ("RootCandidates", "PredictionsPerIteration"):
        if reference.summary[field] != candidate.summary[field]:
            raise BenchmarkError(f"{field} changed.")
    for field in ("ModelSHA256", "Runtime", "TorchInteropThreads"):
        if reference.context[field] != candidate.context[field]:
            raise BenchmarkError(f"{field} changed.")
    return delta


class CpuTuningRunner:
    """Keep one experiment's paths and incrementally saved evidence together."""

    def __init__(self, repo: Path, output_dir: Path) -> None:
        self.repo = repo
        self.output_dir = output_dir
        self.project = repo / PROJECT_PATH
        self.assembly = repo / ASSEMBLY_PATH
        self.results: list[BenchmarkResult] = []

    def run(
        self, phase: str, board: int, threads: int, batch_size: int,
        reference: BenchmarkResult | None = None, *, repeat: bool = False,
    ) -> BenchmarkResult:
        """Launch fresh; never reuse cached TRX output or mutate parent settings."""
        case_name = f"{phase}-Board{board}-T{threads}-B{batch_size}"
        if repeat:
            case_name += "-Repeat"
        case = self.output_dir / "fresh-process" / case_name
        env = os.environ.copy()
        env.update(
            OMP_NUM_THREADS=str(threads), MKL_NUM_THREADS=str(threads),
            GAMMONX_PERF_THREADS=str(threads), GAMMONX_PERF_BATCH_SIZE=str(batch_size),
            GAMMONX_PERF_BOARD=str(board), GAMMONX_PERF_PHASE=phase,
        )
        print(f"Running {case_name} ...", flush=True)
        execute(
            [
                "dotnet", "test", str(self.project), "-c", "Release",
                "--no-build", "--no-restore", "--filter", TEST_FILTER,
                "--logger", "console;verbosity=minimal",
                "--logger", "trx;LogFileName=result.trx",
                "--results-directory", str(case),
            ],
            self.repo, env,
        )
        result = read_result(case / "result.trx", phase, board, threads, batch_size)
        if reference is not None:
            result.score_delta = compare(reference, result)
        self.results.append(result)
        self.save_progress()
        print(
            f"{case_name}: median={result.median_ms:.3f} ms, "
            f"CPU={result.summary['MedianCpuMs']} ms, "
            f"max_score_delta={result.score_delta:.3g}",
            flush=True,
        )
        return result

    def save_progress(self) -> None:
        """Keep raw reports usable if a later configuration fails or is cancelled."""
        save_csv(
            self.output_dir / "tuning-summary.csv",
            [dict(Run=result.run, **result.summary) for result in self.results],
            ("Run", *SUMMARY_FIELDS),
        )
        save_csv(
            self.output_dir / "tuning-samples.csv",
            [
                dict(Run=result.run, **row)
                for result in self.results for row in result.samples
            ],
            ("Run", *SAMPLE_FIELDS),
        )
        save_csv(
            self.output_dir / "candidate-scores.csv",
            [
                dict(Run=result.run, **row)
                for result in self.results for row in result.candidates
            ],
            ("Run", *CANDIDATE_FIELDS),
        )


def confirmation_summary(
    defaults: list[BenchmarkResult], tuned: list[BenchmarkResult],
) -> dict[str, object]:
    """Pool raw samples, not per-run medians, for repeated confirmation pairs."""
    reference = defaults[0]
    candidate = tuned[0]
    delta = max(compare(reference, result) for result in defaults + tuned)
    default_samples = [sample for result in defaults for sample in result.samples]
    tuned_samples = [sample for result in tuned for sample in result.samples]
    default_ms = statistics.median(float(row["WallMs"]) for row in default_samples)
    tuned_ms = statistics.median(float(row["WallMs"]) for row in tuned_samples)
    return {
        "Board": reference.summary["Board"],
        "DefaultThreads": int(reference.summary["Threads"]),
        "DefaultBatchSize": int(reference.summary["MaxBatchSize"]),
        "TunedThreads": int(candidate.summary["Threads"]),
        "TunedBatchSize": int(candidate.summary["MaxBatchSize"]),
        "DefaultMedianMs": round(default_ms, 3),
        "TunedMedianMs": round(tuned_ms, 3),
        "Speedup": round(default_ms / tuned_ms, 3),
        "DefaultMedianCpuMs": (
            reference.summary["MedianCpuMs"] if len(defaults) == 1
            else statistics.median(float(row["CpuMs"]) for row in default_samples)
        ),
        "TunedMedianCpuMs": (
            candidate.summary["MedianCpuMs"] if len(tuned) == 1
            else statistics.median(float(row["CpuMs"]) for row in tuned_samples)
        ),
        "MaxCandidateScoreDelta": delta,
        "SelectedMoveUnchanged": True,
        "ConfirmationRunsPerConfiguration": len(defaults),
    }


def run_experiment(runner: CpuTuningRunner, baseline_threads: int) -> None:
    """Screen on Board 1, confirm on all boards, then reverse Board 2's order."""
    print("Building Release (--no-restore); restore NuGet packages first.", flush=True)
    execute(
        [
            "dotnet", "build", str(runner.project), "-c", "Release",
            "--no-restore", "--verbosity", "quiet",
        ],
        runner.repo,
    )
    initial_fingerprint = runtime_fingerprint(runner.assembly)
    baseline = runner.run("ThreadScreen", 1, baseline_threads, 32)
    thread_results = [baseline]
    for threads in (1, 8, 2, 4):
        if threads != baseline_threads:
            thread_results.append(runner.run("ThreadScreen", 1, threads, 32, baseline))
    best_threads = int(min(thread_results, key=lambda result: result.median_ms).summary["Threads"])
    batch_results = [
        runner.run("BatchScreen", 1, best_threads, batch, baseline)
        for batch in (8, 32, 16, 64)
    ]
    best_batch = int(min(batch_results, key=lambda result: result.median_ms).summary["MaxBatchSize"])
    print(
        f"Screening choice (not a production recommendation): "
        f"{best_threads} threads, maximum batch {best_batch}",
        flush=True,
    )

    confirmations: dict[int, tuple[list[BenchmarkResult], list[BenchmarkResult]]] = {}
    for board in (1, 2, 3):
        # Alternate order to avoid always measuring the reference first.
        if board % 2:
            default = runner.run("ConfirmDefault", board, baseline_threads, 32)
            tuned = runner.run("ConfirmTuned", board, best_threads, best_batch, default)
        else:
            tuned = runner.run("ConfirmTuned", board, best_threads, best_batch)
            default = runner.run("ConfirmDefault", board, baseline_threads, 32, tuned)
        confirmations[board] = ([default], [tuned])

    # Repeat Board 2 in reverse order regardless of whether the first pair improved.
    board2_defaults, board2_tuned = confirmations[2]
    board2_defaults.append(
        runner.run("ConfirmDefault", 2, baseline_threads, 32, board2_defaults[0], repeat=True)
    )
    board2_tuned.append(
        runner.run("ConfirmTuned", 2, best_threads, best_batch, board2_defaults[0], repeat=True)
    )
    comparisons = [
        confirmation_summary(defaults, tuned) for defaults, tuned in confirmations.values()
    ]
    if runtime_fingerprint(runner.assembly) != initial_fingerprint:
        raise BenchmarkError("Application binaries, dependency manifest or model changed during the experiment.")

    save_csv(runner.output_dir / "confirmation-comparison.csv", comparisons)
    choice = {"Threads": best_threads, "MaxBatchSize": best_batch}
    context = {
        "Strategy": (
            "Sequential thread screening at batch 32, then batch screening at the fastest "
            "observed thread count, then confirmation on all boards; not a full factorial search."
        ),
        "ProcessIsolation": (
            "One fresh dotnet test process per run; OMP_NUM_THREADS and MKL_NUM_THREADS "
            "set before Torch loads. No runtime setters or cached test results."
        ),
        "Repeat": (
            "Board2 repeated in reversed order; its comparison pools six timed samples per "
            "configuration. Boards1/3 have three samples per configuration."
        ),
        "TimedInstrumentation": (
            "No metrics listener during timed evaluations. "
            "Worker thread settings verified on every warm-up batch."
        ),
        "InteropThreads": "Unchanged; intra-op/native environment settings only.",
        "AssemblySHA256": initial_fingerprint[runner.assembly.name],
        "RuntimeFingerprint": initial_fingerprint,
        "Choice": choice,
        "Environment": baseline.context,
        "ScoreTolerance": SCORE_TOLERANCE,
        "PassingConfigurations": len(runner.results),
        "Samples": sum(len(result.samples) for result in runner.results),
        "MaxCandidateScoreDelta": max(result.score_delta for result in runner.results),
        "WorkerVerification": [result.worker for result in runner.results],
    }
    (runner.output_dir / "tuning-context.json").write_text(
        json.dumps(context, indent=2) + "\n", encoding="utf-8"
    )
    print(json.dumps({"Comparisons": comparisons, "Choice": choice}, indent=2), flush=True)
    print(f"Completed. Reports: {runner.output_dir}", flush=True)


def main(argv: Sequence[str] | None = None) -> int:
    args = parse_arguments(argv)
    try:
        args.output_dir.mkdir(parents=True, exist_ok=True)
        print(f"Results directory: {args.output_dir}", flush=True)
        run_experiment(CpuTuningRunner(args.repo, args.output_dir), args.baseline_threads)
    except KeyboardInterrupt:
        print(f"\nCancelled. Partial evidence remains in {args.output_dir}", file=sys.stderr)
        return 130
    except (BenchmarkError, OSError, ValueError, csv.Error, ET.ParseError) as error:
        print(
            f"CPU tuning failed: {error}\nPartial evidence, if any: {args.output_dir}",
            file=sys.stderr,
        )
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
