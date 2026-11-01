"""Compare candidate windows in fresh .NET test processes on a quiet machine.

Manual execution from this directory (or pass the script's path from elsewhere):
    python run-candidate-windows.py --threads 16
    python run-candidate-windows.py --threads 16 --output-dir C:/Temp/window-results
    python run-candidate-windows.py --help

Requires Python 3.10+, the .NET 10 SDK on PATH, restored NuGet packages and the
Backgammon model. Uses only the Python standard library. Builds Release without
restore, then runs 45 sequential test processes; allow several minutes. Reports
go to a new timestamped TestResults/windows-* directory beside this script by
default. Importing this module does not build or execute any benchmarks.
"""

import argparse
import hashlib
import json
import math
import os
import shutil
import statistics
import subprocess
import sys
import xml.etree.ElementTree as ET
from collections.abc import Sequence
from datetime import datetime
from pathlib import Path


PROJECT_PATH = Path(
    "src", "GammonX", "GammonX.Mars.NN.Tests", "GammonX.Mars.NN.Tests.csproj"
)
TEST_FILTER = "FullyQualifiedName~PerformanceTests.MeasureCandidateWindows"
# Fix acceptance gates before the first comparison; never fit them to results.
THRESHOLDS = {
    "MinimumGain": 0.05,
    "MinimumWinningRounds": 2,
    "MaximumAllocationRatio": 1.10,
    "MaximumPrivateRatio": 1.20,
    "PrivateAllowanceBytes": 32 * 1024 * 1024,
    "MaximumP95Ratio": 1.10,
    "P95AllowanceMs": 0.10,
    "MaximumWorstRatio": 1.25,
    "WorstAllowanceMs": 0.50,
    "MinimumMixedThroughputRatio": 0.97,
    "Rounds": 3,
    "SamplesPerProcess": 3,
}
FINGERPRINT_PATHS = (
    "GammonX.Engine.dll", "GammonX.Models.dll", "GammonX.Mars.NN.dll",
    "GammonX.Mars.NN.Tests.dll", "GammonX.Mars.NN.Tests.deps.json",
    "Data/NeuralNets/Backgammon/training_net.dat",
    "Data/NeuralNets/Backgammon/training_net.dat.meta.json",
)


class BenchmarkError(RuntimeError):
    """Stop the experiment when a process fails or evidence is inconsistent."""


def positive_int(text: str) -> int:
    value = int(text)
    if value <= 0:
        raise argparse.ArgumentTypeError("must be a positive integer")
    return value


def parse_arguments(argv: Sequence[str] | None = None) -> argparse.Namespace:
    """Discover the checkout from the script, independent of the working directory."""
    script = Path(__file__).resolve()
    default_repo = next(
        (parent for parent in script.parents if (parent / PROJECT_PATH).is_file()),
        None,
    )
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--repo", type=Path, default=default_repo,
        help="Repository root (default: discover above this script).",
    )
    parser.add_argument(
        "--threads", type=positive_int, default=16,
        help="Native/Torch thread count for every process (default: 16).",
    )
    parser.add_argument(
        "--output-dir", type=Path,
        default=script.parent / "TestResults" / datetime.now().strftime("windows-%Y%m%d-%H%M%S-%f"),
        help="New or empty results directory (default: TestResults/windows-<timestamp>).",
    )
    args = parser.parse_args(argv)
    if args.repo is None:
        parser.error("Cannot locate the repository; specify --repo.")
    args.repo = args.repo.expanduser().resolve()
    args.output_dir = args.output_dir.expanduser().resolve()
    if not (args.repo / PROJECT_PATH).is_file():
        parser.error(f"Test project not found: {args.repo / PROJECT_PATH}")
    if args.output_dir.exists() and (
        not args.output_dir.is_dir() or any(args.output_dir.iterdir())
    ):
        parser.error("--output-dir must be new or empty; previous results are not overwritten.")
    if shutil.which("dotnet") is None:
        parser.error("dotnet was not found on PATH; install the .NET 10 SDK.")
    return args


def save_json(path: Path, value: object) -> None:
    path.write_text(json.dumps(value, indent=2, allow_nan=False) + "\n", encoding="utf-8")


def execute(
    command: list[str], repo: Path, *, log: Path | None = None,
    env: dict[str, str] | None = None,
) -> str:
    """Avoid shell quoting; retain combined process output for manual inspection."""
    completed = subprocess.run(
        command, cwd=repo, env=env, stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT, text=True, encoding="utf-8", errors="replace",
        check=False,
    )
    if log is not None:
        log.write_text(completed.stdout, encoding="utf-8")
    if completed.returncode:
        detail = f"See {log}" if log is not None else completed.stdout
        raise BenchmarkError(
            f"Command failed ({completed.returncode}): "
            f"{subprocess.list2cmdline(command)}\n{detail}"
        )
    return completed.stdout.strip()


def fingerprints(binary: Path) -> dict[str, str]:
    """Hash the same binaries, dependency manifest and model before/after each run."""
    return {
        name: hashlib.sha256((binary / name).read_bytes()).hexdigest().upper()
        for name in FINGERPRINT_PATHS
    }


def read_result(path: Path, threads: int, board: int, window: int, mode: str) -> dict:
    """Extract exactly one passing test's WINDOW_JSON record from its TRX file."""
    tests = ET.parse(path).getroot().findall("./{*}Results/{*}UnitTestResult")
    if len(tests) != 1 or tests[0].get("outcome") != "Passed":
        raise BenchmarkError(f"Expected one passing test: {path}")
    stdout = tests[0].findtext("./{*}Output/{*}StdOut", default="")
    records = [
        line.strip()[len("WINDOW_JSON,"):]
        for line in stdout.splitlines() if line.strip().startswith("WINDOW_JSON,")
    ]
    if len(records) != 1:
        raise BenchmarkError(f"Expected one WINDOW_JSON record: {path}")
    run = json.loads(records[0])
    expected = {"Threads": threads, "Board": board, "Window": window, "Mode": mode}
    if any(run[key] != value for key, value in expected.items()):
        raise BenchmarkError(f"Unexpected configuration: {path}")
    expected_samples = {"Timing": THRESHOLDS["SamplesPerProcess"], "Mixed": 1, "Diagnostic": 0}
    if len(run["Samples"]) != expected_samples[mode]:
        raise BenchmarkError(f"Unexpected sample count: {path}")
    for sample in run["Samples"]:
        if not math.isfinite(sample["WallMs"]) or sample["WallMs"] <= 0:
            raise BenchmarkError(f"Invalid wall-clock timing: {path}")
    if mode == "Mixed" and (
        not run["Probes"]
        or any(not math.isfinite(value) or value < 0 for value in run["Probes"])
    ):
        raise BenchmarkError(f"Missing or invalid interactive probes: {path}")
    return run


def percentile(values: list[float], quantile: float) -> float:
    """Use the original runner's nearest-rank percentile, without interpolation."""
    return sorted(values)[math.ceil(len(values) * quantile) - 1]


def samples(runs: list[dict], field: str = "WallMs") -> list[float]:
    return [sample[field] for run in runs for sample in run["Samples"]]


def check_scores(runs: list[dict]) -> list[dict]:
    """Reject changes in coverage, selected move or scores across fresh processes."""
    checks = []
    for board in (1, 2, 3):
        board_runs = [run for run in runs if run["Board"] == board]
        reference = board_runs[0]
        expected = {row["Key"]: row["Score"] for row in reference["Scores"]}
        for run in board_runs:
            actual = {row["Key"]: row["Score"] for row in run["Scores"]}
            if (
                any(run[key] != reference[key] for key in ("Selected", "Rows", "Candidates"))
                or not actual or actual.keys() != expected.keys()
                or len(actual) != len(run["Scores"])
                or len(actual) != run["Candidates"]
            ):
                raise BenchmarkError(f"Coverage or selected move changed in {run['ProcessName']}.")
            differences = [abs(expected[key] - actual[key]) for key in expected]
            if any(not math.isfinite(value) or value > 1e-5 for value in differences):
                raise BenchmarkError(f"Score tolerance exceeded in {run['ProcessName']}.")
            checks.append({"Process": run["ProcessName"], "MaximumScoreDifference": max(differences)})
    return checks


def compare_windows(runs: list[dict]) -> list[dict]:
    """Pool raw timing samples and apply all repeatability, memory and fairness gates."""
    comparisons = []
    for window in (2, 4):
        reasons = []
        boards = []
        for board in (1, 2, 3):
            timing = [run for run in runs if run["Mode"] == "Timing" and run["Board"] == board]
            baseline = [run for run in timing if run["Window"] == 1]
            candidate = [run for run in timing if run["Window"] == window]
            base_wall = statistics.median(samples(baseline))
            wall = statistics.median(samples(candidate))
            winning_rounds = sum(
                statistics.median(samples([run for run in candidate if run["Round"] == round_number]))
                <= statistics.median(samples([run for run in baseline if run["Round"] == round_number]))
                * (1 - THRESHOLDS["MinimumGain"])
                for round_number in range(1, THRESHOLDS["Rounds"] + 1)
            )
            allocation_ratio = (
                statistics.median(samples(candidate, "AllocatedBytes"))
                / statistics.median(samples(baseline, "AllocatedBytes"))
            )
            if (
                wall > base_wall * (1 - THRESHOLDS["MinimumGain"])
                or winning_rounds < THRESHOLDS["MinimumWinningRounds"]
            ):
                reasons.append(f"Board {board} lacks a repeatable 5% gain.")
            if allocation_ratio > THRESHOLDS["MaximumAllocationRatio"]:
                reasons.append(f"Board {board} allocation exceeds gate.")
            if statistics.median(run["PrivateBytes"] for run in candidate) > (
                statistics.median(run["PrivateBytes"] for run in baseline)
                * THRESHOLDS["MaximumPrivateRatio"] + THRESHOLDS["PrivateAllowanceBytes"]
            ):
                reasons.append(f"Board {board} private memory exceeds gate.")
            boards.append({
                "Board": board, "BaselineMedianMs": base_wall, "MedianMs": wall,
                "Gain": 1 - wall / base_wall, "WinningRounds": winning_rounds,
                "BaselineP95Ms": percentile(samples(baseline), 0.95),
                "P95Ms": percentile(samples(candidate), 0.95),
                "AllocationRatio": allocation_ratio,
            })
        baseline = [run for run in runs if run["Mode"] == "Mixed" and run["Window"] == 1]
        candidate = [run for run in runs if run["Mode"] == "Mixed" and run["Window"] == window]
        base_probes = [probe for run in baseline for probe in run["Probes"]]
        probes = [probe for run in candidate for probe in run["Probes"]]
        base_p95, p95 = percentile(base_probes, 0.95), percentile(probes, 0.95)
        base_worst, worst = max(base_probes), max(probes)
        throughput = statistics.median(samples(baseline)) / statistics.median(samples(candidate))
        if p95 > base_p95 + max(
            THRESHOLDS["P95AllowanceMs"], base_p95 * (THRESHOLDS["MaximumP95Ratio"] - 1)
        ):
            reasons.append("Interactive p95 exceeds fairness gate.")
        if worst > base_worst + max(
            THRESHOLDS["WorstAllowanceMs"], base_worst * (THRESHOLDS["MaximumWorstRatio"] - 1)
        ):
            reasons.append("Worst interactive probe exceeds fairness gate.")
        if throughput < THRESHOLDS["MinimumMixedThroughputRatio"]:
            reasons.append("Mixed throughput exceeds regression gate.")
        comparisons.append({
            "Window": window, "Accepted": not reasons, "Reasons": reasons,
            "Boards": boards, "MixedThroughputRatio": throughput,
            "BaselineProbeP95Ms": base_p95, "ProbeP95Ms": p95,
            "BaselineWorstProbeMs": base_worst, "WorstProbeMs": worst,
        })
    return comparisons


def run_experiment(args: argparse.Namespace) -> None:
    """Build once, execute isolated comparisons, and retain incremental evidence."""
    repo, output = args.repo, args.output_dir
    project = str(repo / PROJECT_PATH)
    output.mkdir(parents=True, exist_ok=True)
    print(f"Results directory: {output}", flush=True)
    save_json(output / "thresholds.json", THRESHOLDS)
    print("Building Release (--no-restore); restore NuGet packages first.", flush=True)
    execute(
        ["dotnet", "build", project, "-c", "Release", "--no-restore"],
        repo, log=output / "build.log",
    )
    target = execute(
        ["dotnet", "msbuild", project, "-p:Configuration=Release", "-getProperty:TargetPath"],
        repo,
    )
    assembly = Path(target)
    if not assembly.is_absolute():
        assembly = repo / assembly
    if not assembly.is_file():
        raise BenchmarkError(f"Cannot resolve Release output: {target}")
    initial_fingerprints = fingerprints(assembly.parent)
    runs = []

    def run(round_number: int, board: int, window: int, mode: str) -> None:
        name = f"{mode}-r{round_number}-b{board}-w{window}"
        print(f"Running {name}", flush=True)
        # Pass settings only to the child; the caller's environment stays intact.
        env = os.environ.copy()
        env.update(
            OMP_NUM_THREADS=str(args.threads), MKL_NUM_THREADS=str(args.threads),
            GAMMONX_PERF_THREADS=str(args.threads), GAMMONX_PERF_WINDOW=str(window),
            GAMMONX_PERF_BOARD=str(board), GAMMONX_PERF_WINDOW_MODE=mode,
        )
        if fingerprints(assembly.parent) != initial_fingerprints:
            raise BenchmarkError(f"Inputs changed before {name}.")
        execute(
            [
                "dotnet", "test", project, "-c", "Release", "--no-build", "--no-restore",
                "--filter", TEST_FILTER, "--logger", f"trx;LogFileName={name}.trx",
                "--results-directory", str(output),
            ],
            repo, log=output / f"{name}.log", env=env,
        )
        result = read_result(output / f"{name}.trx", args.threads, board, window, mode)
        result.update(Round=round_number, ProcessName=name)
        if fingerprints(assembly.parent) != initial_fingerprints:
            raise BenchmarkError(f"Inputs changed during {name}.")
        runs.append(result)
        save_json(output / "runs.json", runs)

    # Reverse relative pair ordering in alternate rounds to limit ordering bias.
    for round_number in range(1, THRESHOLDS["Rounds"] + 1):
        for board in (1, 2, 3):
            for window in ((1, 2, 4) if round_number % 2 else (4, 2, 1)):
                run(round_number, board, window, "Timing")
    # Metrics listeners run separately, outside timing and mixed-load measurements.
    for board in (1, 2, 3):
        for window in (1, 2, 4):
            run(1, board, window, "Diagnostic")
    for round_number in range(1, THRESHOLDS["Rounds"] + 1):
        for window in ((1, 2, 4) if round_number % 2 else (4, 2, 1)):
            run(round_number, 1, window, "Mixed")

    score_checks = check_scores(runs)
    comparisons = compare_windows(runs)
    chosen = min((row["Window"] for row in comparisons if row["Accepted"]), default=1)
    save_json(output / "results.json", {
        "StartedFromCommit": execute(["git", "rev-parse", "HEAD"], repo),
        "RecordedAt": datetime.now().astimezone().isoformat(),
        "Thresholds": THRESHOLDS, "Fingerprints": initial_fingerprints,
        "ChosenWindow": chosen, "Comparisons": comparisons,
        "ScoreChecks": score_checks, "Runs": runs,
    })
    print(json.dumps(comparisons, indent=2), flush=True)
    print(f"Chosen window: {chosen}; raw results: {output}", flush=True)


def main(argv: Sequence[str] | None = None) -> int:
    args = parse_arguments(argv)
    try:
        run_experiment(args)
    except KeyboardInterrupt:
        print(f"\nCancelled. Partial evidence remains in {args.output_dir}", file=sys.stderr)
        return 130
    except (BenchmarkError, OSError, ValueError, KeyError, TypeError, ZeroDivisionError, ET.ParseError) as error:
        print(
            f"Candidate-window comparison failed: {error}\n"
            f"Partial evidence, if any: {args.output_dir}", file=sys.stderr,
        )
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
