#!/usr/bin/env python3
"""Fail the build when a tracked eval metric drops below its committed floor.

This is the enforcement half of the P-19 CI gate. It reads the harness's JSON report
(``Eval/results/report-*.json``, produced by ``dotnet run --project Eval`` — see
``Eval/EvalReport.cs``) and the committed floor in ``Eval/baseline.json``. For every metric named
in ``baseline["metrics"]`` it compares the report's ``summary.<metric>`` against ``floor -
tolerance`` and exits non-zero if any metric falls short, so a regression in citation-hit rate,
refusal accuracy, or groundedness turns the workflow red.

Usage:
    python3 check-baseline.py --report <report.json | results-dir> --baseline <baseline.json>

``--report`` accepts either a report file or a directory; given a directory it picks the newest
``report-*.json`` in it (the harness stamps each report with the run time).
"""

import argparse
import glob
import json
import os
import sys


def resolve_report(path):
    """Return the report file to read, picking the newest report-*.json when given a directory."""
    if os.path.isdir(path):
        candidates = sorted(glob.glob(os.path.join(path, "report-*.json")))
        if not candidates:
            sys.exit(f"No report-*.json found in '{path}' — did the eval harness run?")
        return candidates[-1]
    if not os.path.isfile(path):
        sys.exit(f"Report '{path}' not found — did the eval harness run?")
    return path


def load_json(path):
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--report", required=True, help="Report JSON file, or a directory of them.")
    parser.add_argument("--baseline", required=True, help="Committed baseline JSON (floors + tolerance).")
    args = parser.parse_args()

    report_path = resolve_report(args.report)
    report = load_json(report_path)
    baseline = load_json(args.baseline)

    summary = report.get("summary")
    if not isinstance(summary, dict):
        sys.exit(f"Report '{report_path}' has no 'summary' object — unexpected schema.")

    metrics = baseline.get("metrics")
    if not isinstance(metrics, dict) or not metrics:
        sys.exit(f"Baseline '{args.baseline}' has no 'metrics' to enforce.")
    tolerance = float(baseline.get("tolerance", 0.0))

    print(f"Comparing {report_path}")
    print(f"  against  {args.baseline} (tolerance {tolerance})\n")

    header = f"{'metric':<18}{'observed':>12}{'floor':>10}{'min':>10}   verdict"
    print(header)
    print("-" * len(header))

    failures = []
    for name, floor in metrics.items():
        if name not in summary:
            sys.exit(f"Metric '{name}' is not in the report summary. Available keys: {sorted(summary)}")
        observed = float(summary[name])
        floor = float(floor)
        minimum = floor - tolerance
        ok = observed >= minimum
        print(f"{name:<18}{observed:>12.4f}{floor:>10.4f}{minimum:>10.4f}   {'PASS' if ok else 'FAIL'}")
        if not ok:
            failures.append((name, observed, minimum))

    print()
    if failures:
        for name, observed, minimum in failures:
            # ::error:: annotations surface on the GitHub Actions summary and PR checks.
            print(f"::error::{name} regressed: {observed:.4f} is below the floor {minimum:.4f}")
        print(f"\n{len(failures)} metric(s) below baseline — failing the eval gate.")
        return 1

    print("All tracked metrics are at or above their committed floor.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
