#!/usr/bin/env python3
"""SolverBatch 가 남긴 json 리포트를 csv 두 장으로 바꾼다.

스테이지 요약과 굴려 본 판 목록은 줄 수가 달라 한 표에 못 담는다.
그래서 <이름>.stages.csv 와 <이름>.attempts.csv 로 나눠 쓴다.
attempts 쪽에 stageId 를 같이 넣어 두어 둘을 그것으로 잇는다.

획(좌표)은 담지 않는다. csv 로 펴면 판마다 줄 수가 달라져 표가 아니게 된다 —
그림이 필요하면 json 의 solution 을 본다.

사용:
  python BuildTools/solver_report_csv.py SolverReports/Stage01.json
  python BuildTools/solver_report_csv.py 리포트.json -o 어디에/쓸지
"""
import argparse
import csv
import json
import sys
from pathlib import Path

# Excel 이 한글을 깨뜨리지 않게 BOM 을 붙인다.
ENCODING = "utf-8-sig"

STAGE_COLUMNS = [
    "stageId", "cleared", "pass", "passName", "tries",
    "bestGoalDist", "seconds", "seed", "deviceCount",
    "attemptCount", "clearCount", "strokeCount", "pivotCount",
]

ATTEMPT_COLUMNS = [
    "stageId", "index", "pass", "passName", "outcome",
    "minGoalDist", "endStep", "ink",
    "areaX", "areaY", "areaW", "areaH",
]


def stage_row(stage):
    attempts = stage.get("attempts", [])
    solution = stage.get("solution") or {}

    return {
        "stageId": stage.get("stageId", ""),
        "cleared": stage.get("cleared", False),
        "pass": stage.get("pass", 0),
        "passName": stage.get("passName", ""),
        "tries": stage.get("tries", 0),
        "bestGoalDist": stage.get("bestGoalDist", 0.0),
        "seconds": stage.get("seconds", 0.0),
        "seed": stage.get("seed", 0),
        "deviceCount": stage.get("deviceCount", 0),
        "attemptCount": len(attempts),
        "clearCount": sum(1 for a in attempts if a.get("outcome") == "Clear"),
        "strokeCount": len(solution.get("Strokes", [])),
        "pivotCount": len(solution.get("Pivots", [])),
    }


def attempt_row(stage_id, attempt):
    area = attempt.get("area") or {}

    return {
        "stageId": stage_id,
        "index": attempt.get("index", 0),
        "pass": attempt.get("pass", 0),
        "passName": attempt.get("passName", ""),
        "outcome": attempt.get("outcome", ""),
        "minGoalDist": attempt.get("minGoalDist", 0.0),
        "endStep": attempt.get("endStep", 0),
        "ink": attempt.get("ink", 0.0),
        "areaX": area.get("x", 0.0),
        "areaY": area.get("y", 0.0),
        "areaW": area.get("width", 0.0),
        "areaH": area.get("height", 0.0),
    }


def write(path, columns, rows):
    # newline="" 이 아니면 Windows 에서 줄 사이에 빈 줄이 낀다.
    with open(path, "w", encoding=ENCODING, newline="") as f:
        writer = csv.DictWriter(f, fieldnames=columns)
        writer.writeheader()
        writer.writerows(rows)


def main(argv) -> int:
    # 출력 인코딩 강제: cp949 등 비-UTF-8 콘솔에서 한글/em대시 출력 시
    # UnicodeEncodeError로 죽는 것을 방지한다.
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report", type=Path, help="SolverBatch 가 쓴 json")
    parser.add_argument(
        "-o", "--out", type=Path,
        help="csv 를 쓸 자리. 이름만 준다 (.stages.csv 가 붙는다). 없으면 json 옆")
    args = parser.parse_args(argv)

    report = json.loads(args.report.read_text(encoding="utf-8"))
    stages = report.get("stages", [])

    base = args.out if args.out else args.report.with_suffix("")

    stage_rows = [stage_row(s) for s in stages]
    attempt_rows = [
        attempt_row(s.get("stageId", ""), a)
        for s in stages for a in s.get("attempts", [])
    ]

    stages_path = base.with_name(base.name + ".stages.csv")
    attempts_path = base.with_name(base.name + ".attempts.csv")

    write(stages_path, STAGE_COLUMNS, stage_rows)
    write(attempts_path, ATTEMPT_COLUMNS, attempt_rows)

    unreadable = report.get("unreadable", [])

    print(f"{stages_path}  — 스테이지 {len(stage_rows)}개")
    print(f"{attempts_path}  — 판 {len(attempt_rows)}개")
    if unreadable:
        print(f"읽지 못한 파일 {len(unreadable)}개 — json 의 unreadable 을 본다")

    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
