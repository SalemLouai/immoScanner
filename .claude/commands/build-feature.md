---
description: Run the full pipeline Architect → Dev → Tester → Reviewer → Git Publisher on brief.md
---

# /build-feature

Orchestrate the full 5-agent pipeline on the current brief.

## Procedure

1. Read `brief.md`. If missing or empty, ask for the brief and STOP.
2. Show a summary of the brief and the planned sequence, then run WITHOUT waiting for
   further confirmation (autonomous mode).
3. Apply the `CLAUDE.md` workflow exactly:
   - Step 1: `architect` sub-agent → `output/architecture.md` → GATE 1
   - Step 2: `dev` sub-agent → `output/src/` → GATE 2
   - Step 3: `tester` sub-agent → `output/tests/` + `QA_REPORT.md` → GATE 3
   - Step 4: `reviewer` sub-agent → `output/SECURITY_REVIEW.md` (GO/NO-GO) → GATE 4
   - Step 5: `git-publisher` sub-agent → PR opened (only if GO/Conditional GO) → GATE 5
   - Step 6: `output/REPORT.md`
4. On any failed gate: at most one relaunch of the agent involved, otherwise STOP and
   report precisely. A GATE 4 NO-GO triggers one Dev → Tester → Reviewer remediation pass.
5. Finish by displaying `output/REPORT.md`.

## Optional argument
If the user provides $ARGUMENTS, treat it as the inline brief and write it to
`brief.md` before running the pipeline.
