---
id: PROMPTER-0008
title: GDPR posture
status: accepted
stage: implemented
class: strategy
reversibility: costly
decided: 2026-07-15
decider: woksin
applies-to:
  - "Source/**"
  - "Documentation/**"
enforced-by: "not verified — see PROMPTER-0013 for the data-minimization rule that amends part of this record"
review-trigger: "A change in controller status or subprocessors, or a requirement for EU-region inference."
fallback: "If EU-region inference becomes a requirement, Claude via Vertex AI (europe-west) or Bedrock (eu-central-1) is a config change behind IChatClient."
---

Historical decision D-8, 2026-07-15.

> **2026-07-16 — amended by [PROMPTER-0013](PROMPTER-0013-interaction-log-stores-no-personal-data.md).** The interaction log now stores no personal data at all: no message content and no user identifier, not even a hash. The hashed-ID, question/answer-text retention, and DSAR-by-hash rules below are superseded by data minimization. The rest of this record (controller status, subprocessor posture, EU-inference fallback) stands.

## Decision

We are the controller (Norway/EEA; Datatilsynet). v1 rules:

- Store Discord user IDs hashed (rate limiting and abuse control need identity-shaped data, analytics does not need identities).
- Question/answer text is retained on a configurable window (default 90 days) with an automatic purge job.
- A pinned privacy notice in the server names the bot, what it processes, and the LLM subprocessor.
- The DSAR path is "delete by hashed ID".

Model traffic goes to the Anthropic API (no training on API data by default).

## Fallback

If EU-region inference becomes a requirement, Claude via Vertex AI (europe-west) or Bedrock (eu-central-1) is a config change behind `IChatClient`.
