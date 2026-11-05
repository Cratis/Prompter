---
id: PROMPTER-0016
title: Prompter bridges Discord and the issue trackers
status: accepted
stage: implemented
class: product
reversibility: costly
decided: 2026-08-06
decider: woksin
applies-to:
  - "Source/**"
enforced-by: "not verified — a GitHub App scoped to issues: write on named repositories and a spec that filing requires a human confirmation would verify it"
review-trigger: "Issue volume making open filing wrong (switch to maintainer-approval mode), or any proposal for Prompter to write code, run a coding agent, or interject unprompted."
fallback: "If volume ever makes open filing wrong, a maintainer-approval mode is a config flag, not a redesign."
---

Historical decision D-16, 2026-08-06. Extends [PROMPTER-0007](PROMPTER-0007-interaction-model.md).

## Context

The expensive part of a community question is rarely the answer; it is that the signal dies in chat. A bug reported in a thread at midnight, an API someone expected to exist, a feature idea that gets three "yes please" reactions: none of it reaches the tracker unless a maintainer happens to be reading and happens to transcribe it. Prompter is already in every one of those conversations, already has the context, and already knows the products well enough to route. The docs-gap flywheel (backlog item P-33) was the first instance of this; treating it as the general case is what makes the whole thing worth building.

## Decision

Prompter's job is not only answering from the docs. It also moves work between the community and the Cratis issue trackers, in both directions, for every kind of work item: a bug, a missing API, a feature request, a half-formed idea, a documentation gap. Concretely: filing an issue from a Discord conversation (backlog item P-45), answering newly opened issues from the docs (P-44), telling Discord when an issue appears (P-46), and handing the mechanical ones to a coding agent (P-47).

## Rules that keep it from becoming noise

- **Filing is always a deliberate act by a person**: a slash command or a context-menu action, never an inference from message content. Prompter drafts, a human confirms in an ephemeral preview, and only then is an issue opened. This keeps [PROMPTER-0013](PROMPTER-0013-interaction-log-stores-no-personal-data.md) intact: the click is the consent, and Prompter persists nothing.
- **Anyone may file.** At the community's current size, an approval step would cost more in latency and missed reports than it saves in noise, and the person who hit the problem is the one who can describe it. Duplicate suppression, a per-user cap, and a `from-discord` label carry the load instead. If volume ever makes this wrong, a maintainer-approval mode is a config flag, not a redesign.
- **Prompter never writes code and never runs a coding agent.** It files issues; GitHub's own agents act on them, and only when a human applies the label, only as draft PRs, only in repos that opt in.
- **The no-auto-chime rule of PROMPTER-0007 still holds.** Nothing here lets Prompter interject unprompted; every path starts with someone asking for it.

## Consequences

Accepted trade-off: Prompter gains write access to repositories it did not previously touch, which is a real increase in blast radius. It is bounded by a GitHub App scoped to `issues: write` on named repositories, with no contents permission at all.
