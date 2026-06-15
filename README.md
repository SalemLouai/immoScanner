# .NET 9 / Azure — Autonomous AI Team (Claude Code)

Autonomous multi-agent pipeline: one brief in, a designed, coded, tested,
security-reviewed and published .NET 9 feature out. Five specialized sub-agents
orchestrated by Claude Code.

```
brief.md → 🏛️ Architect → ⚙️ Dev → 🧪 Tester → 🛡️ Reviewer → 🚀 Git Publisher → PR
              architecture.md  src/     tests/    SECURITY_REVIEW.md   (GitHub MCP)
```

> Auto hook: `dotnet format` runs after every code write (Write/Edit).

## Why this design reduces errors and time

| Prompt-engineering choice | Effect |
|---|---|
| **Strict I/O contracts** per agent (mandatory sections, fixed formats) | Deterministic, auto-validatable output |
| **Validation gates** between each step | An error is caught early, not propagated |
| **One relaunch per gate** then fail-fast | No infinite loop, bounded token cost |
| **Explicit prohibitions** (`❌ Do not…`) | Prevents gold-plating and scope drift |
| **Per-agent models** (opus/sonnet/haiku) | Cost optimized for each task |
| **Warnings as errors** enforced on the Dev | Code compiles on the first try |
| **Final self-check** in each agent | The agent fixes before delivering |

## Per-agent model routing

| Agent | Model | Rationale |
|-------|-------|-----------|
| 🏛️ Architect | `opus` | Architectural judgment, highest stakes |
| ⚙️ Dev | `sonnet` | Long implementation, great quality/cost |
| 🧪 Tester | `sonnet` | Test generation, code reading |
| 🛡️ Reviewer | `opus` | Security reasoning, OWASP analysis |
| 🚀 Git Publisher | `haiku` | Mechanical git/PR task, cheapest |

To switch all agents at once, set `CLAUDE_CODE_SUBAGENT_MODEL` and use `inherit`.

## Installation

1. Copy the `dotnet-ai-team/` folder wherever you want.
2. Prerequisites: Claude Code installed, a Max plan (sub-agents consume usage).
3. (Optional) .NET 9 SDK installed → agents validate via `dotnet build`/`test`.

### GitHub MCP (for the Git Publisher)

Create a GitHub Personal Access Token (scopes: `repo`, `read:org`, optionally
`workflow`), then register the MCP once:

```bash
claude mcp add -s user --transport http github \
  https://api.githubcopilot.com/mcp \
  -H "Authorization: Bearer YOUR_PAT"

claude mcp list   # should show: github ✓
```

## Structure

```
dotnet-ai-team/
├── CLAUDE.md                    # Orchestrator (read at startup)
├── brief.md                     # YOUR INPUT — describe the feature here
├── .claude/
│   ├── settings.json            # Permissions + dotnet format hook
│   ├── agents/
│   │   ├── architect.md         # Designs → architecture.md      (opus)
│   │   ├── dev.md               # Codes → src/                   (sonnet)
│   │   ├── tester.md            # Tests → tests/ + QA_REPORT.md   (sonnet)
│   │   ├── reviewer.md          # Security audit → SECURITY_REVIEW.md (opus)
│   │   └── git-publisher.md     # Branch + PR via GitHub MCP      (haiku)
│   ├── hooks/
│   │   └── format.sh            # dotnet format auto (PostToolUse)
│   └── commands/
│       └── build-feature.md     # /build-feature
└── output/                      # All deliverables land here
```

## Usage

```bash
cd dotnet-ai-team
# 1. Edit brief.md with your feature (an ImmoScorer example is included)
# 2. Launch Claude Code
claude --model claude-opus-4-6
```

Then inside Claude Code:

```
/build-feature
```

Or natural language: "Orchestrate the agents on the brief."
Or inline brief: `/build-feature An invoicing API with PDF export`

## Fully autonomous mode (no confirmations)

```bash
claude --dangerously-skip-permissions
> /build-feature
```

> ⚠️ Only use this flag in an isolated working folder. The provided `settings.json`
> already allows `dotnet`/`git` commands and blocks destructive ones (force-push,
> push to main/master, `rm -rf`).

## The reliability secret

Each agent has a **verifiable output contract**, and the orchestrator
**mechanically validates** that contract before moving to the next step. That is
what turns a fragile chain of prompts into a reproducible pipeline.

## Cost

Everything runs on tools you already pay for:

| Component | Cost |
|---|---|
| Claude Code (the agents) | Included in your Max plan |
| GitHub CLI / GitHub MCP | Free |
| GitHub (repos + PRs) | Free tier (unlimited private repos, PRs) |
| .NET 9 SDK | Free |

The only real "cost" is your Claude usage quota. Per-agent model routing
(opus only where it matters, haiku for the publisher) keeps it efficient.
