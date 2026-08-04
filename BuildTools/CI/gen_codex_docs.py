#!/usr/bin/env python3
"""Codex용 미러 문서 자동 생성/검증.

Claude Code용 가드레일 문서를 단일 출처(SSOT)로 보고, Codex CLI가 참조하는
미러 문서를 거기서 결정론적으로 생성한다. 손으로 양쪽을 맞추다 보니 생긴
오타(`.Codex/agents/`)·누락(모듈 AGENTS.md)을 없애는 게 목적이다.

미러 대상 (Claude 쪽 → Codex 쪽):
  CLAUDE.md                       → AGENTS.md
  Assets/_Project/<M>/CLAUDE.md   → Assets/_Project/<M>/AGENTS.md
  .claude/agents/<name>.md        → .codex/agents/<name>.toml

변환 규칙 (md → AGENTS.md):
  - 문자열 치환만: `CLAUDE.md` → `AGENTS.md`, `.claude/agents` → `.codex/agents`.
  - 그 외 내용은 원본 그대로. 줄바꿈은 LF로 통일한다(아래 EOL 주석 참고).

EOL: 생성물은 항상 LF로 쓴다. 비교도 EOL을 정규화해서 한다. 그래서 Windows(작업
트리 CRLF)에서도, Linux CI(LF)에서도 --check 결과가 같다. 생성물은 .gitattributes에서
eol=lf로 고정해(working tree까지 LF) 플랫폼 간 churn을 없앤다.

변환 규칙 (.claude/agents/*.md → .codex/agents/*.toml):
  - YAML frontmatter에서 name/description만 TOML 키로 옮긴다(tools/model 등은 버림).
  - 본문은 위 치환을 적용해 developer_instructions = \"\"\"...\"\"\" 에 담는다.

사용:
  python BuildTools/CI/gen_codex_docs.py                    # 생성/갱신 (전체)
  python BuildTools/CI/gen_codex_docs.py --check            # 최신인지 검증 (전체). 다르면 비정상 종료
  python BuildTools/CI/gen_codex_docs.py --check --staged   # 스테이징된 출처만 검증 (pre-commit)
  python BuildTools/CI/gen_codex_docs.py --staged           # 스테이징된 출처만 생성/갱신

--staged 는 대상을 "이번 커밋에 올라간 SSOT 문서"로 좁힌다. 손대지도 않은 문서 때문에
커밋이 막히지 않게 하려는 것이다. --check 와 함께 쓰면 작업 트리가 아니라 **인덱스**를
읽는다 — 커밋에 실제로 들어가는 것이 인덱스이고, `git add CLAUDE.md` 만 하고 생성물을
빠뜨린 부분 스테이징을 잡아내려면 그래야 한다.
"""
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC_ROOT = ROOT / "Assets" / "_Project"
CLAUDE_AGENTS = ROOT / ".claude" / "agents"
CODEX_AGENTS = ROOT / ".codex" / "agents"

GEN = "BuildTools/CI/gen_codex_docs.py"


def normalize(data: bytes) -> str:
    """BOM 제거 + 줄바꿈을 LF로 정규화해서 읽는다(플랫폼 무관 비교용)."""
    if data.startswith(b"\xef\xbb\xbf"):
        data = data[3:]
    return data.decode("utf-8").replace("\r\n", "\n").replace("\r", "\n")


def read_text(path: Path) -> str:
    return normalize(path.read_bytes())


def git(*args) -> subprocess.CompletedProcess:
    """읽기 전용 git 조회 전용. 저장소 상태를 바꾸는 명령은 여기서 부르지 않는다."""
    return subprocess.run(("git",) + args, cwd=ROOT, capture_output=True)


def read_worktree(rel: str):
    """작업 트리의 내용. 없으면 None."""
    path = ROOT / rel
    return read_text(path) if path.exists() else None


def read_index(rel: str):
    """인덱스(스테이징 영역)에 올라간 내용. 없으면 None."""
    result = git("show", f":{rel}")
    return normalize(result.stdout) if result.returncode == 0 else None


def staged_sources() -> set:
    """이번 커밋에 올라간 파일 경로(저장소 기준 posix). 삭제(D)는 제외한다."""
    result = git("diff", "--cached", "--name-only", "--diff-filter=ACMR")
    if result.returncode != 0:
        raise SystemExit(
            "git diff --cached 실패 — 저장소 안에서 실행해야 한다.\n"
            + normalize(result.stderr)
        )
    return {line.strip() for line in normalize(result.stdout).splitlines() if line.strip()}


def mirror(text: str) -> str:
    """Claude 문서 → Codex 문서 문자열 치환."""
    return text.replace("CLAUDE.md", "AGENTS.md").replace(".claude/agents", ".codex/agents")


def md_banner(src_rel: str) -> str:
    """마크다운 미러 문서 상단 배너(HTML 주석, 렌더링에는 안 보임). LF."""
    return (
        f"<!-- 이 파일은 {GEN}가 자동 생성합니다. 직접 수정하지 마세요.\n"
        f"     출처(SSOT): {src_rel} — 내용을 바꾸려면 출처를 고치고 생성기를 다시 실행하세요. -->\n"
        "\n"
    )


def toml_banner(src_rel: str) -> str:
    """TOML 미러 문서 상단 배너(# 주석). LF."""
    return (
        f"# 이 파일은 {GEN}가 자동 생성합니다. 직접 수정하지 마세요.\n"
        f"# 출처(SSOT): {src_rel}\n"
    )


def toml_escape(s: str) -> str:
    """TOML 기본 문자열(\"...\") 안에 들어갈 값 이스케이프."""
    return s.replace("\\", "\\\\").replace('"', '\\"')


def build_md(raw: str, src_rel: str) -> str:
    """가드레일 md → 미러 AGENTS.md 문자열(LF)."""
    return md_banner(src_rel) + mirror(raw)


def build_toml(raw: str, src_rel: str) -> str:
    """에이전트 정의 md(YAML frontmatter + 본문) → Codex TOML 문자열(LF)."""
    lines = raw.split("\n")  # raw 는 이미 LF로 정규화되어 들어온다
    if not lines or lines[0].strip() != "---":
        raise ValueError(f"{src_rel}: frontmatter(---)로 시작하지 않음")
    try:
        end = lines.index("---", 1)
    except ValueError:
        raise ValueError(f"{src_rel}: frontmatter 종료(---)를 찾지 못함")

    name = ""
    desc = ""
    for f in lines[1:end]:
        if f.startswith("name:"):
            name = f[len("name:"):].strip()
        elif f.startswith("description:"):
            desc = f[len("description:"):].strip()

    body = lines[end + 1:]
    while body and body[0].strip() == "":
        body.pop(0)
    while body and body[-1].strip() == "":
        body.pop()

    return (
        toml_banner(src_rel)
        + f'name = "{toml_escape(name)}"\n'
        f'description = "{toml_escape(desc)}"\n'
        'developer_instructions = """\n'
        + mirror("\n".join(body))
        + '"""\n'
    )


def build_targets(read, only=None):
    """(출력 경로, 출처 rel, 생성 내용) 목록.

    only 가 주어지면 그 안에 든 출처만 다룬다(--staged).
    출처 목록 자체는 작업 트리에서 훑는다 — 인덱스에만 있고 디스크에 없는 파일은
    커밋 직전 상태에서 나오지 않는다.
    """
    targets = []

    def add(out: Path, src_rel: str, build):
        if only is not None and src_rel not in only:
            return
        raw = read(src_rel)
        if raw is None:
            return
        targets.append((out, src_rel, build(raw, src_rel)))

    # 루트 가드레일 문서
    add(ROOT / "AGENTS.md", "CLAUDE.md", build_md)

    # 모듈별 가드레일 문서
    for claude_md in sorted(SRC_ROOT.glob("*/CLAUDE.md")):
        add(claude_md.with_name("AGENTS.md"), claude_md.relative_to(ROOT).as_posix(), build_md)

    # 에이전트 정의
    for md in sorted(CLAUDE_AGENTS.glob("*.md")):
        add(CODEX_AGENTS / (md.stem + ".toml"), md.relative_to(ROOT).as_posix(), build_toml)

    return targets


def main(argv) -> int:
    # 출력 인코딩 강제: cp949 등 비-UTF-8 콘솔에서 한글/em대시 출력 시
    # UnicodeEncodeError로 죽는 것을 방지한다.
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

    check = "--check" in argv
    staged = "--staged" in argv

    # 검증은 인덱스를 본다 — 커밋에 들어갈 것이 그것이므로.
    # 반면 생성은 작업 트리에 쓰므로 읽기도 작업 트리에서 해야 한다.
    # 인덱스에서 읽어 작업 트리에 쓰면 아직 스테이징하지 않은 최신 편집을 조용히 덮어쓴다.
    read = read_index if (check and staged) else read_worktree

    only = staged_sources() if staged else None
    targets = build_targets(read, only)

    if staged and not targets:
        print("스테이징된 SSOT 문서 없음 — 건너뜀")
        return 0

    if check:
        stale = []
        for out, _src, content in targets:
            out_rel = out.relative_to(ROOT).as_posix()
            if read(out_rel) != content:  # 양쪽 다 LF 정규화된 값
                stale.append(out_rel)

        if stale:
            where = "인덱스" if staged else "작업 트리"
            print(f"Codex 미러 문서가 SSOT(CLAUDE 쪽)와 어긋남 [{where}] — 다음 파일을 갱신해야 함:")
            for s in stale:
                print("  -", s)
            print(f"해결: python {GEN}")
            if staged:
                print("      git add " + " ".join(stale))
            return 1

        print("Codex 미러 문서 최신 OK")
        return 0

    for out, _src, content in targets:
        data = content.encode("utf-8")  # 생성물은 항상 LF
        if out.exists() and out.read_bytes() == data:
            continue
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_bytes(data)
        print(f"갱신: {out.relative_to(ROOT).as_posix()}")

    print("Codex 미러 문서 생성 완료")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
