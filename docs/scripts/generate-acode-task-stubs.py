#!/usr/bin/env python3
"""Generate Acode task stubs from task-list.md.

This is adapted from your e-commerce stub generator. It reads `task-list.md`
and generates instruction-prepended stubs for parent tasks and their subtasks.

- Output folder: `tasks/stubs/`
- Naming: `task-XXX-... (NEEDS-REFINEMENT).md`
- For subtasks: `task-XXXa-... (NEEDS-REFINEMENT).md`

You can then ask Claude/ChatGPT to expand each stub into a full spec.
"""

from __future__ import annotations

import re
from dataclasses import dataclass
from pathlib import Path
from typing import List, Tuple

PROJECT_ROOT = Path(__file__).resolve().parent
TASK_LIST = PROJECT_ROOT / "task-list.md"
OUT_DIR = PROJECT_ROOT / "tasks" / "stubs"
OUT_DIR.mkdir(parents=True, exist_ok=True)

INSTRUCTIONS = '# INSTRUCTIONS FOR CLAUDE TO COMPLETE THIS TASK\n\n**READ THIS SECTION CAREFULLY BEFORE PROCEEDING**\n\nYou are being asked to expand this task stub into a complete, production-ready task specification for the **Agentic Coding Bot (Acode)**.\n\n## Required Sections and Quality Standards\n\nYour completed task MUST include all of the following sections with the specified level of detail:\n\n### 1. Header (Already Complete)\n- Priority, Tier, Complexity, Phase, Dependencies are already filled out\n- Do not modify these unless explicitly instructed by the task list\n\n### 2. Description (Expand)\n- **Length:** 3–6 paragraphs\n- Include: Business Value, Technical Details, Integration points (reference other tasks), Constraints/Considerations\n- Must clearly state what is **in scope** and **out of scope**\n\n### 3. Use Cases (3 scenarios)\nUse personas:\n- **Neil** (Owner/Developer)\n- **DevBot** (Automation runner)\n- **Jordan** (Contributor)\n\nEach scenario:\n- 10–15 lines\n- Before/After workflow\n- Explicit outcomes and verification cues\n\n### 4. User Manual Documentation\n- Overview\n- Step-by-step instructions (commands/config paths)\n- Settings/Configuration\n- Best Practices (5–7)\n- Troubleshooting (3–5)\n- **Length:** 150–300 lines (unless task requires more)\n\n### 5. Acceptance Criteria / Definition of Done\n- **Length:** 40–80 items (depending on task size)\n- Must be objectively verifiable checkboxes\n- Include categories: Functionality, Safety/Policy, UX/CLI, Logging/Audit, Performance, Docs, Tests\n\n### 6. Testing Requirements (All 5 types)\n- Unit tests (5–8)\n- Integration tests (3–5)\n- End-to-End tests (3–5)\n- Performance tests (3–4 benchmarks with targets)\n- Regression tests (list impacted areas or state N/A)\n\n### 7. User Verification Steps\n- 8–10 manual scenarios with “Verify:” expectations\n\n### 8. Implementation Prompt for Claude\n- 100–250 lines minimum\n- Include file paths, class names, interfaces, and why decisions are made\n- Must respect Clean Architecture boundaries (Domain → Application → Infrastructure → CLI)\n- Must include validation steps and next steps\n\n## Quality Checklist\n- [ ] No TODOs/placeholders remain\n- [ ] AC/DoD is measurable and complete\n- [ ] Tooling, safety, and docs are included\n- [ ] Fits the repo structure established by Task 000\n- [ ] References Task 001 constraints where applicable\n\n---\n\n**NOW PROCEED TO EXPAND THE TASK STUB BELOW INTO A COMPLETE SPECIFICATION**\n\n---\n\n'

STUB_TEMPLATE = '# Task {task_num:03d}{suffix}: {title}\n\n**Priority:** {priority} / 49  \n**Tier:** {tier}  \n**Complexity:** {complexity} (Fibonacci points)  \n**Phase:** {phase}  \n**Dependencies:** {dependencies}  \n\n---\n\n## Description (EXPAND THIS)\n\n{stub_description}\n\n---\n\n## Use Cases (CREATE 3 DETAILED SCENARIOS)\n\n---\n\n## User Manual Documentation (WRITE COMPLETE DOCUMENTATION)\n\n---\n\n## Acceptance Criteria / Definition of Done (CREATE COMPREHENSIVE CHECKLIST)\n\n---\n\n## Testing Requirements (WRITE ALL 5 TEST TYPES)\n\n---\n\n## User Verification Steps (CREATE 8-10 MANUAL TESTS)\n\n---\n\n## Implementation Prompt for Claude (WRITE DETAILED GUIDE)\n\n---\n\n**END OF TASK {task_num:03d}{suffix}**\n'

@dataclass
class Task:
    number: int
    title: str
    subtasks: List[str]
    epic: str
    epic_title: str

def slugify(s: str) -> str:
    s = s.lower()
    s = re.sub(r"[^a-z0-9\s-]", "", s)
    s = re.sub(r"\s+", "-", s.strip())
    return s[:80].strip("-") or "task"

def parse_task_list(text: str) -> List[Task]:
    epic_re = re.compile(r"^##\s+(EPIC\s+\d+)\s+—\s+(.*)$", re.MULTILINE)
    task_re = re.compile(r"^###\s+Task\s+(\d+):\s+(.*)$", re.MULTILINE)
    sub_re = re.compile(r"^####\s+(.*)$", re.MULTILINE)

    epics: List[Tuple[int, str, str]] = []
    for em in epic_re.finditer(text):
        epics.append((em.start(), em.group(1), em.group(2)))
    epics.append((len(text), "", ""))

    tasks: List[Task] = []
    for i in range(len(epics)-1):
        start, ecode, etitle = epics[i]
        end = epics[i+1][0]
        block = text[start:end]
        for tm in task_re.finditer(block):
            num = int(tm.group(1))
            title = tm.group(2).strip()
            tstart = tm.end()
            next_task = task_re.search(block, tstart)
            tend = next_task.start() if next_task else len(block)
            subt_block = block[tstart:tend]
            subs = [s.strip() for s in sub_re.findall(subt_block)]
            tasks.append(Task(num, title, subs, ecode, etitle))
    return tasks

def default_meta(task_num: int, is_sub: bool) -> tuple[str, int, str, str]:
    # Keep this simple: you can customize later.
    tier = "S"
    complexity = 5 if is_sub else 8
    phase = "Phase 1 - Foundation"
    dependencies = "Task 000" if task_num > 0 else "None"
    return tier, complexity, phase, dependencies

def write_stub(task: Task, sub_idx: int | None = None):
    is_sub = sub_idx is not None
    suffix = ""
    title = task.title
    if is_sub:
        suffix = chr(ord('a') + sub_idx)
        title = task.subtasks[sub_idx]
        suffix = "." + suffix

    tier, complexity, phase, dependencies = default_meta(task.number, is_sub)
    priority = task.number  # simple default
    stub_description = "Expand this stub into a complete specification following the instructions."

    content = INSTRUCTIONS + STUB_TEMPLATE.format(
        task_num=task.number,
        suffix=suffix,
        title=title,
        priority=f"{priority}",
        tier=tier,
        complexity=complexity,
        phase=phase,
        dependencies=dependencies,
        stub_description=stub_description
    )

    filename = f"task-{task.number:03d}{suffix.replace('.', '')}-{slugify(title)} (NEEDS-REFINEMENT).md"
    (OUT_DIR / filename).write_text(content, encoding="utf-8")
    print("Created", OUT_DIR / filename)

def main():
    text = TASK_LIST.read_text(encoding="utf-8")
    tasks = parse_task_list(text)

    for t in tasks:
        # Always generate parent + subtasks stubs
        write_stub(t, None)
        for i in range(len(t.subtasks)):
            write_stub(t, i)

if __name__ == "__main__":
    main()
