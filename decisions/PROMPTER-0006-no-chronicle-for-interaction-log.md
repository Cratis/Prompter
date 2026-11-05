---
id: PROMPTER-0006
title: Use Chronicle for the interaction log
status: rejected
stage: none
class: strategy
reversibility: reversible
decided: 2026-07-16
decider: woksin
applies-to:
  - "Source/**"
enforced-by: "not verified — the interaction log is a Postgres IInteractionLog; nothing automated prevents adding Chronicle"
review-trigger: "A concrete pull for event-sourced analytics on the Q&A log (the re-raise condition: revisit only on a concrete pull)."
---

Historical decision D-6, decided "NOT now", 2026-07-16.

## Context

The Q&A interaction log (question asked, answer given, feedback received) is naturally event-shaped, and a bot built on our own platform is a good story. It was actively reconsidered on 2026-07-16.

## Decision

Chronicle is ruled out for the foreseeable roadmap: "it's just a Discord bot." v1 keeps the simple Postgres `IInteractionLog`.

## Grounds

A four-repo research pass (Chronicle/Arc/Ada) established that:

- Chronicle is a separate gRPC kernel server, not an in-process library, so it cannot share our Postgres in-process.
- Its Postgres backend is only about 7 weeks old (Mongo is the proven path).
- Adopting it would add a hard runtime dependency and a local kernel container to an otherwise self-contained bot, for analytics value we do not need at launch.

Our GDPR posture ([PROMPTER-0008](PROMPTER-0008-gdpr-posture.md)) already covers v1 without it.

## Re-raise condition

Revisit only on a concrete pull, from the findings and build path captured in the Chronicle research notes. Adopting Chronicle then is still an adapter swap behind the `IInteractionLog` seam, not a redesign.
