# Agents

This repository follows a lightweight governance protocol aligned to the PM v2.2 approach. When an explicit PM v2.2 document is not present, use the baseline rules below.

## Communication
- Speak to developers with technical precision.
- Report concrete actions, command results, and changed files.
- Call out blockers immediately.

## Burst Protocol
- Execute 3-4 slices per burst atomically.
- Run tests after each burst, not per slice.
- No mid-burst direction changes unless blocking.
- After each burst, answer: "What uncertainty was eliminated?"

## Slice Classification
- Each slice is one of: Structure, Content, Design, Infra.
- Do not mix categories in a single slice.

## Safe Assumption Rule
When ambiguity occurs:
- Choose the simplest interpretation.
- Log the assumption in DECISIONS.md immediately with a date stamp.
- Continue execution.
- Stop only if a test fails, a security risk is found, or the change would break scope.

## Issue Handling
- Blocking issues: halt and request user input.
- Non-blocking issues: log to FIXLOG.md and continue.

## Testing
- Keep CI offline-safe (no live endpoints required).
- Validate exit codes (0 success, non-zero failure).
- Preserve JSONL stdout contract (stderr for logs/errors).

## Scope Discipline
- No new features, no breaking changes, no new dependencies.
- Preserve the 4-project architecture and .NET 8 target.

## Required Documents
Maintain throughout the milestone:
- PROJECT.md
- MILESTONES.md (create if missing)
- DECISIONS.md (append-only)
- FIXLOG.md (create if needed)
- README.md
- PROMPTS.md
- AGENTS.md
- CHECKLIST.md
