---
id: PROMPTER-0014
title: Whether to store question text
status: deferred
stage: none
class: product
reversibility: costly
decided: 2026-11-05
decider: woksin
applies-to:
  - "Source/Storage/**"
  - "Source/**/InteractionLog*"
enforced-by: "not verified — PROMPTER-0013 already removes question text from the schema; a schema spec would verify it"
review-trigger: "When a consent notice and a narrow retention window for refusal text are designed (Feed B of #33)."
---

Historical decision D-14, originally open on 2026-08-06. Deferred on 2026-11-05 by woksin.

## Decision

Deferred. Do not store refused question text until a consent mechanism and a retention window exist. [PROMPTER-0013](PROMPTER-0013-interaction-log-stores-no-personal-data.md) stands in the meantime.

## Context

The question PROMPTER-0013 deliberately left open. The docs-gap flywheel (backlog item P-33) wants to know what people asked that Prompter could not answer. Nothing knows today: a refusal writes `was_refusal` and a confidence, never the question. Re-admitting question text is a trivial schema change and a posture change, since it retires the strongest sentence in the privacy notice ("we keep no message content"), so it gets its own record rather than riding in on a feature.

## Options considered

- **A · Never store it; take consent in the moment.** A "this should be documented" button on refusals (backlog item P-45) forwards the question text straight to a maintainer channel or an issue. The click is the consent and the disclosure; nothing is persisted by us, PROMPTER-0013 stands verbatim, and the signal is self-filtering. Costs: only volunteered gaps are seen, and there are no aggregates ("how many people hit this?").
- **B · Store question text on refusals only, behind a consent notice and short retention.** Enables counting and clustering, which is what makes a weekly digest better than a stream of one-off reports. Costs: a channel notice and privacy-page rewrite, a lawful-basis line, a retention window that actually purges (the existing `RetentionPurge` becomes a privacy control again, not housekeeping), and the open-source claim weakens from "we keep nothing" to "we keep this, for N days, for this purpose".
- **C · Store everything again** (questions and answers, all interactions). Rejected: PROMPTER-0013's grounds have not changed. Nothing reads it back, and the marginal analytic value over B does not buy back the posture.

## Recommendation recorded in the log

A now, B only on evidence. Ship the button, run it for a few weeks, and see whether the volunteered reports are enough to drive docs work. Reach for B only if the flywheel demonstrably stalls without aggregates, and then scope it as narrowly as it will go (refused questions only, 30 days, purpose stated in the channel notice and the privacy page). The team rules on A versus B when the button has run long enough to argue from data rather than from expectation.
