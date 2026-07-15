# Planning

Repo-wide planning and decision documents for Prompter — kept deliberately small: **every live doc below owns
one concern needed to move v1 forward**; anything resolved, shipped, or superseded moves to `archive/` (fold its
open residue into `BACKLOG.md` first).

> Created 2026-07-15 from the buy-vs-build research recorded in [`RESEARCH.md`](RESEARCH.md). M0 (scaffold)
> shipped the same day — see [`SESSION_HANDOVER.md`](SESSION_HANDOVER.md) for current state.

## The working set (read in this order)

1. **[`SESSION_HANDOVER.md`](SESSION_HANDOVER.md)** — current resume state, next actions, gotchas.
   **Start here in a fresh session.**
2. **[`V1_PLAN.md`](V1_PLAN.md)** — the one-page v1 roadmap: milestones M0–M5, build order, definition of done.
3. **[`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md)** — the detailed plan to feature-complete: every
   milestone's tasks with files, approach, and verifiable "done when" criteria.
4. **[`BACKLOG.md`](BACKLOG.md)** — the single consolidated work list: P-items (detail owned by the
   implementation plan), open Q-questions, post-v1 parking lot.
5. **[`DECISIONS.md`](DECISIONS.md)** — durable decision records D-1…D-10. Do not re-litigate; append new
   rulings here.

## Specifications (behavior contracts)

| Doc | Owns |
|---|---|
| [`DISCORD_INTEGRATION.md`](DISCORD_INTEGRATION.md) | The Discord behavior contract (functionality set, triggers, never-do's), intents/permissions, and the app-registration runbook |
| [`DEPLOYMENT.md`](DEPLOYMENT.md) | Production topology, one-time setup, recurring operations, operational GDPR posture |

## Reference

- [`RESEARCH.md`](RESEARCH.md) — the 2026-07-15 research that led to building Prompter instead of buying
  kapa.ai/Inkeep: market survey, GDPR posture, cost model, and the verified retrieval-quality techniques the
  design leans on. Background material, not a work list.

## archive/

Empty so far — completed plans and superseded records will live here.
