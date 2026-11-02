"""Compare roll batching or reply-board reuse in 30 fresh Release test processes.

Run from any directory: python run-roll-batching.py --threads 16
Requires restored packages, the .NET 10 SDK and the checked-in Backgammon model.
Uses the standard library and shared helpers from run-candidate-windows.py.
"""

import argparse
import importlib
import json
import os
from pathlib import Path
import statistics
import sys
from datetime import datetime

windows = importlib.import_module("scripts.run-candidate-windows")

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
}


def compare(runs, field="CrossRollBatching"):
    reasons, boards = [], []
    for board in (1, 2, 3):
        timing = [run for run in runs if run["Mode"] == "Timing" and run["Board"] == board]
        baseline = [run for run in timing if not run[field]]
        candidate = [run for run in timing if run[field]]
        before = statistics.median(windows.samples(baseline))
        after = statistics.median(windows.samples(candidate))
        wins = sum(
            statistics.median(windows.samples([run for run in candidate if run["Round"] == round_number]))
            <= statistics.median(windows.samples([run for run in baseline if run["Round"] == round_number]))
            * (1 - THRESHOLDS["MinimumGain"])
            for round_number in range(1, THRESHOLDS["Rounds"] + 1)
        )
        allocation = statistics.median(windows.samples(candidate, "AllocatedBytes")) / statistics.median(windows.samples(baseline, "AllocatedBytes"))
        if after > before * (1 - THRESHOLDS["MinimumGain"]) or wins < THRESHOLDS["MinimumWinningRounds"]:
            reasons.append(f"Board {board} lacks a repeatable 5% improvement.")
        if allocation > THRESHOLDS["MaximumAllocationRatio"]:
            reasons.append(f"Board {board} allocation exceeds gate.")
        if statistics.median(run["PrivateBytes"] for run in candidate) > (
            statistics.median(run["PrivateBytes"] for run in baseline) * THRESHOLDS["MaximumPrivateRatio"]
            + THRESHOLDS["PrivateAllowanceBytes"]
        ):
            reasons.append(f"Board {board} private memory exceeds gate.")
        boards.append({"Board": board, "BeforeMs": before, "AfterMs": after,
                       "Gain": 1 - after / before, "WinningRounds": wins, "AllocationRatio": allocation})

    baseline = [run for run in runs if run["Mode"] == "Mixed" and not run[field]]
    candidate = [run for run in runs if run["Mode"] == "Mixed" and run[field]]
    before_probes = [probe for run in baseline for probe in run["Probes"]]
    after_probes = [probe for run in candidate for probe in run["Probes"]]
    before_p95, after_p95 = windows.percentile(before_probes, 0.95), windows.percentile(after_probes, 0.95)
    before_worst, after_worst = max(before_probes), max(after_probes)
    throughput = statistics.median(windows.samples(baseline)) / statistics.median(windows.samples(candidate))
    if after_p95 > before_p95 + max(THRESHOLDS["P95AllowanceMs"], before_p95 * (THRESHOLDS["MaximumP95Ratio"] - 1)):
        reasons.append("Interactive p95 exceeds fairness gate.")
    if after_worst > before_worst + max(THRESHOLDS["WorstAllowanceMs"], before_worst * (THRESHOLDS["MaximumWorstRatio"] - 1)):
        reasons.append("Worst interactive probe exceeds fairness gate.")
    if throughput < THRESHOLDS["MinimumMixedThroughputRatio"]:
        reasons.append("Mixed throughput exceeds regression gate.")
    return {"Accepted": not reasons, "Reasons": reasons, "Boards": boards,
            "BeforeProbeP95Ms": before_p95, "AfterProbeP95Ms": after_p95,
            "BeforeWorstProbeMs": before_worst, "AfterWorstProbeMs": after_worst,
            "MixedThroughputRatio": throughput}


def run_experiment(args):
    repo = next(parent for parent in Path(__file__).resolve().parents if (parent / windows.PROJECT_PATH).is_file())
    output = args.output_dir.resolve()
    if output.exists() and (not output.is_dir() or any(output.iterdir())):
        raise windows.BenchmarkError("Output directory must be new or empty.")
    output.mkdir(parents=True, exist_ok=True)
    windows.save_json(output / "thresholds.json", THRESHOLDS)
    project = str(repo / windows.PROJECT_PATH)
    windows.execute(["dotnet", "build", project, "-c", "Release", "--no-restore"], repo, log=output / "build.log")
    target = Path(windows.execute(["dotnet", "msbuild", project, "-p:Configuration=Release", "-getProperty:TargetPath"], repo))
    if not target.is_absolute():
        target = repo / target
    fingerprints = windows.fingerprints(target.parent)
    commit = windows.execute(["git", "rev-parse", "HEAD"], repo)
    runs = []
    field = {"reply-board": "ReuseReplyBoard", "feature-buffers": "ReuseFeatureBuffers",
             "roll-batching": "CrossRollBatching"}[args.optimization]
    rounds = 1 if args.quick else THRESHOLDS["Rounds"]

    def run(round_number, board, cross_roll, mode):
        name = f"{mode}-r{round_number}-b{board}-{field}{int(cross_roll)}"
        print(f"Running {name}", flush=True)
        env = os.environ.copy()
        env.update(OMP_NUM_THREADS=str(args.threads), MKL_NUM_THREADS=str(args.threads),
                   GAMMONX_PERF_THREADS=str(args.threads), GAMMONX_PERF_WINDOW="1",
                   GAMMONX_PERF_BOARD=str(board), GAMMONX_PERF_WINDOW_MODE=mode,
                   GAMMONX_PERF_CROSS_ROLL_BATCHING=str(int(cross_roll)) if field == "CrossRollBatching" else "1",
                   GAMMONX_PERF_REUSE_REPLY_BOARD="1" if field == "ReuseFeatureBuffers" else str(int(cross_roll)) if field == "ReuseReplyBoard" else "0",
                   GAMMONX_PERF_REUSE_FEATURE_BUFFERS=str(int(cross_roll)) if field == "ReuseFeatureBuffers" else "0")
        if windows.fingerprints(target.parent) != fingerprints:
            raise windows.BenchmarkError("Artifacts changed before " + name)
        windows.execute(["dotnet", "test", project, "-c", "Release", "--no-build", "--no-restore",
                         "--filter", windows.TEST_FILTER, "--logger", f"trx;LogFileName={name}.trx",
                         "--results-directory", str(output)], repo, log=output / f"{name}.log", env=env)
        result = windows.read_result(output / f"{name}.trx", args.threads, board, 1, mode)
        expected = {"CrossRollBatching": env["GAMMONX_PERF_CROSS_ROLL_BATCHING"] == "1",
                    "ReuseReplyBoard": env["GAMMONX_PERF_REUSE_REPLY_BOARD"] == "1",
                    "ReuseFeatureBuffers": env["GAMMONX_PERF_REUSE_FEATURE_BUFFERS"] == "1"}
        if any(result[key] != value for key, value in expected.items()):
            raise windows.BenchmarkError("Wrong batching configuration: " + name)
        if mode == "Mixed" and len(result["Probes"]) != 100:
            raise windows.BenchmarkError("Expected 100 overlapping probes: " + name)
        if runs and result["InteropThreads"] != runs[0]["InteropThreads"]:
            raise windows.BenchmarkError("Inter-op threads changed: " + name)
        if windows.fingerprints(target.parent) != fingerprints:
            raise windows.BenchmarkError("Artifacts changed during " + name)
        result.update(Round=round_number, ProcessName=name)
        runs.append(result)
        windows.save_json(output / "runs.json", runs)

    for round_number in range(1, rounds + 1):
        for board in (1, 2, 3):
            for cross_roll in ((False, True) if round_number % 2 else (True, False)):
                run(round_number, board, cross_roll, "Timing")
    for board in ((1,) if args.quick else (1, 2, 3)):
        for cross_roll in (False, True):
            run(1, board, cross_roll, "Diagnostic")
    for round_number in range(1, rounds + 1):
        for cross_roll in ((False, True) if round_number % 2 else (True, False)):
            run(round_number, 1, cross_roll, "Mixed")

    score_checks = windows.check_scores(runs)
    comparison = {"Accepted": False, "Reasons": ["Quick comparison cannot establish repeatability; full acceptance not evaluated."]} if args.quick else compare(runs, field)
    windows.save_json(output / "results.json", {
        "StartedFromCommit": commit, "Optimization": args.optimization, "Quick": args.quick, "RecordedAt": datetime.now().astimezone().isoformat(),
        "Fingerprints": fingerprints, "Thresholds": THRESHOLDS, "Comparison": comparison,
        "ScoreChecks": score_checks, "Runs": runs,
    })
    print(json.dumps(comparison, indent=2), flush=True)
    print(f"Raw evidence: {output}", flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--threads", type=windows.positive_int, default=16)
    parser.add_argument("--optimization", choices=("roll-batching", "reply-board", "feature-buffers"), default="roll-batching")
    parser.add_argument("--quick", action="store_true", help="10-process screening run; never establishes acceptance.")
    parser.add_argument("--output-dir", type=Path, default=Path(__file__).resolve().parent / "TestResults"
                        / datetime.now().strftime("roll-batching-%Y%m%d-%H%M%S-%f"))
    args = parser.parse_args()
    try:
        run_experiment(args)
        return 0
    except KeyboardInterrupt:
        print("Cancelled; partial evidence is not a completed comparison.", file=sys.stderr)
        return 130
    except (windows.BenchmarkError, OSError, ValueError, KeyError, TypeError, windows.ET.ParseError) as error:
        print(f"Comparison failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
