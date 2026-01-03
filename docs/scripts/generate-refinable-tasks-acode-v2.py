#!/usr/bin/env python3
"""Generate refinement-ready documents from stubs for Agentic Coding Bot (Acode), v2.

What this does:
- Prepends a strong instruction header (aligned to e-commerce sample quality targets).
- Injects canonical context from `task-list.md`:
  - For epics: lists all tasks/subtasks in the epic.
  - For tasks: injects epic/title/subtasks reminders.

It does NOT actually refine/expand the stub (that's done by an LLM).

Usage:
  python generate-refined-tasks-acode-v2.py --in ./ --out ./refined --mode all
"""

from __future__ import annotations

import argparse
import re
from pathlib import Path
from typing import Iterable, Optional, Tuple

TASK_HEADER = '# INSTRUCTIONS FOR CLAUDE TO COMPLETE THIS TASK (REFINED SPEC TARGET)\n\nYou are expanding a **task stub** into a *complete, enterprise-grade, implementation-ready* specification for **Agentic Coding Bot (Acode)**.\n\nThese specs must be on par with our e-commerce task samples:\n- Typical length: **8,457–22,968 words** (target **~10k–18k** unless task is genuinely smaller/larger)\n- Acceptance Criteria / Definition of Done: typically **103–341 checkboxes** (target **~180–260**)\n\n## Non-negotiable quality bar\n- Write as if a mediocre automation engineer will implement it verbatim.\n- No “hand-wavy” language (avoid: *should*, *ideally*, *nice to have*). Use *MUST* and *MUST NOT*.\n- Every section must be objectively testable or auditable.\n- Respect Clean Architecture boundaries (Domain → Application → Infrastructure → CLI).\n- Respect Task 001 constraints (no external LLM APIs; mode rules).\n\n## Required Sections (all required; do not delete)\n1) Description\n   - 6–12 paragraphs\n   - Include: business value, scope boundaries, integration points (with task numbers), failure modes, assumptions\n2) Glossary / Terms (10–25 entries where relevant)\n3) Out-of-Scope (explicit bullets)\n4) Functional Requirements (grouped; 40–120 items)\n5) Non-Functional Requirements (security, performance, reliability; 20–60 items)\n6) User Manual Documentation\n   - 250–600 lines typical\n   - Include: quick start, config knobs, CLI examples, best practices, troubleshooting, FAQs\n7) Acceptance Criteria / Definition of Done\n   - Target: 180–260 checkbox items\n   - Must include categories: Functionality, Safety/Policy, CLI/UX, Logging/Audit, Performance, Docs, Tests, Compatibility\n8) Testing Requirements (all 5 types)\n   - Unit (15–30)\n   - Integration (10–20)\n   - E2E (8–15)\n   - Performance/Benchmarks (5–10, with targets)\n   - Regression (explicit impacted areas)\n9) User Verification Steps\n   - 12–20 scenarios with “Verify:” expectations\n10) Implementation Prompt\n   - 200–600 lines\n   - Must include: file paths, class/interface names, contracts, error codes, logging fields\n   - Must include “Validation checklist before merge”\n   - Must include “Rollout plan” (even if local-only)\n\n## Anti-footgun requirements\n- Specify exit codes for CLI errors\n- Specify logging schema fields\n- Specify default config values and precedence\n- Specify how secrets are redacted in logs/artifacts\n\n---\n\n'
EPIC_HEADER = '# INSTRUCTIONS FOR CLAUDE TO COMPLETE THIS EPIC SUMMARY (REFINED SPEC TARGET)\n\nYou are expanding an **epic stub** into a complete EPIC specification for **Agentic Coding Bot (Acode)**.\n\nQuality bar:\n- This EPIC doc must make it easy to implement every task in the epic.\n- It must define boundaries, shared interfaces, and cross-cutting constraints.\n\n## Required Sections\n1) Epic Overview (purpose, boundaries, dependencies)\n2) Outcomes (10–25)\n3) Non-Goals (10–25)\n4) Architecture & Integration Points (interfaces, events, data contracts)\n5) Operational Considerations (modes/safety/audit)\n6) Acceptance Criteria / Definition of Done (50–120 checkboxes)\n7) Risks & Mitigations (12+)\n8) Milestone Plan (3–7 milestones mapping to tasks)\n9) “Definition of Epic Complete” checklist (20–40)\n\n---\n\n'

def iter_md_files(folder: Path) -> Iterable[Path]:
    for p in folder.rglob('*.md'):
        if p.is_file():
            yield p

def normalize_filename(name: str) -> str:
    name = name.replace('(NEEDS-REFINEMENT)', '').strip()
    name = re.sub(r'\s+', ' ', name)
    return name

def already_has_instructions(text: str) -> bool:
    return text.lstrip().startswith('# INSTRUCTIONS FOR CLAUDE')

def parse_task_header(md: str) -> Optional[Tuple[int, Optional[str], str]]:
    m = re.search(r"^#\s*Task\s+(\d{3})(?:\.([a-z]))?:\s*(.+?)\s*$", md, re.MULTILINE)
    if not m:
        return None
    return int(m.group(1)), m.group(2), m.group(3).strip()

def parse_epic_header(md: str) -> Optional[Tuple[str, str]]:
    m = re.search(r"^#\s*(EPIC\s+\d+)\s+—\s+(.+?)\s*$", md, re.MULTILINE)
    if not m:
        return None
    return m.group(1).strip(), m.group(2).strip()

def load_task_list(task_list_path: Path):
    text = task_list_path.read_text(encoding='utf-8')
    epic_re = re.compile(r"^##\s+(EPIC\s+\d+)\s+—\s+(.*)$", re.MULTILINE)
    task_re = re.compile(r"^###\s+Task\s+(\d+):\s+(.*)$", re.MULTILINE)
    sub_re = re.compile(r"^####\s+(.*)$", re.MULTILINE)

    epics = []
    for em in epic_re.finditer(text):
        epics.append((em.start(), em.group(1), em.group(2)))
    epics.append((len(text), "", ""))

    epic_map = {}
    task_map = {}
    for i in range(len(epics)-1):
        start, ecode, etitle = epics[i]
        end = epics[i+1][0]
        block = text[start:end]
        tasks = []
        for tm in task_re.finditer(block):
            num = int(tm.group(1))
            title = tm.group(2).strip()
            tstart = tm.end()
            next_task = task_re.search(block, tstart)
            tend = next_task.start() if next_task else len(block)
            subt_block = block[tstart:tend]
            subs = [s.strip() for s in sub_re.findall(subt_block)]
            tasks.append((num, title, subs))
            task_map[num] = {'title': title, 'subs': subs, 'epic_code': ecode, 'epic_title': etitle}
        if ecode:
            epic_map[ecode] = {'title': etitle, 'tasks': tasks}
    return epic_map, task_map

def inject_task_context(md: str, task_num: int, suffix: Optional[str], task_map) -> str:
    info = task_map.get(task_num)
    if not info:
        return md
    canonical_title = info['title']
    if suffix is not None:
        idx = ord(suffix) - ord('a')
        if 0 <= idx < len(info['subs']):
            canonical_title = info['subs'][idx]

    subtasks = info['subs']
    epic_code = info['epic_code']
    epic_title = info['epic_title']

    siblings = "\n".join([f"  - Task {task_num:03d}.{chr(97+i)}: {t}" for i,t in enumerate(subtasks)]) if subtasks else "  - (none)"

    context = f"""## Canonical Context (from task-list.md)

- **Epic:** {epic_code} — {epic_title}
- **Canonical Task Title:** Task {task_num:03d}{('.'+suffix) if suffix else ''}: {canonical_title}
- **Sibling Subtasks (if applicable):**
{siblings}

- **Hard constraints reminder:** MUST comply with Task 001 operating modes and the “no external LLM API” constraint set.
- **Repo contract reminder:** MUST align with Task 002 `.agent/config.yml` contract where relevant.

---

"""

    marker = "\n---\n\n"
    idx = md.find(marker)
    if idx != -1:
        insert_at = idx + len(marker)
        return md[:insert_at] + context + md[insert_at:]
    return context + md

def inject_epic_context(md: str, epic_code: str, epic_map) -> str:
    e = epic_map.get(epic_code)
    if not e:
        return md
    tasks = e['tasks']
    lines = []
    for num,title,subs in tasks:
        lines.append(f"- Task {num:03d}: {title}")
        for i,sub in enumerate(subs):
            lines.append(f"  - Task {num:03d}.{chr(97+i)}: {sub}")
    task_list_lines = "\n".join(lines)

    context = f"""## Canonical Context (from task-list.md)

- **Epic:** {epic_code} — {e['title']}
- **Tasks in this epic:**
{task_list_lines}

---

"""

    marker = "\n---\n\n"
    idx = md.find(marker)
    if idx != -1:
        insert_at = idx + len(marker)
        return md[:insert_at] + context + md[insert_at:]
    return context + md

def ensure_header(md: str, header: str) -> str:
    if already_has_instructions(md):
        # Replace existing instructions block with the stronger header by stripping leading instructions section.
        # Strategy: If file starts with '# INSTRUCTIONS FOR CLAUDE', remove everything until the first '# Task' or '# EPIC'
        stripped = md.lstrip()
        if stripped.startswith('# INSTRUCTIONS FOR CLAUDE'):
            m = re.search(r"^#\s*(Task\s+\d{3}|EPIC\s+\d+)\b", stripped, re.MULTILINE)
            if m:
                md = stripped[m.start():]
            else:
                md = stripped
    return header + md

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--in', dest='in_dir', default='.', help='Input directory (containing tasks/ and epics/)')
    ap.add_argument('--out', dest='out_dir', default='./refined', help='Output directory')
    ap.add_argument('--mode', choices=['tasks','epics','all'], default='all')
    ap.add_argument('--task-list', dest='task_list', default='task-list.md', help='Path to task-list.md')
    args = ap.parse_args()

    in_dir = Path(args.in_dir).resolve()
    out_dir = Path(args.out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    epic_map, task_map = load_task_list(Path(args.task_list).resolve())

    if args.mode in ('tasks','all'):
        src = in_dir / 'tasks' if (in_dir / 'tasks').exists() else in_dir
        dst = out_dir / 'refined-tasks'
        dst.mkdir(parents=True, exist_ok=True)
        for f in iter_md_files(src):
            md = f.read_text(encoding='utf-8')
            th = parse_task_header(md)
            md2 = ensure_header(md, TASK_HEADER)
            if th:
                num, suffix, _ = th
                md2 = inject_task_context(md2, num, suffix, task_map)
            (dst / normalize_filename(f.name)).write_text(md2, encoding='utf-8')

    if args.mode in ('epics','all'):
        src = in_dir / 'epics' if (in_dir / 'epics').exists() else in_dir
        dst = out_dir / 'refined-epics'
        dst.mkdir(parents=True, exist_ok=True)
        for f in iter_md_files(src):
            md = f.read_text(encoding='utf-8')
            eh = parse_epic_header(md)
            md2 = ensure_header(md, EPIC_HEADER)
            if eh:
                code, _ = eh
                md2 = inject_epic_context(md2, code, epic_map)
            (dst / normalize_filename(f.name)).write_text(md2, encoding='utf-8')

    print('Done. Outputs in:', out_dir)

if __name__ == '__main__':
    main()
