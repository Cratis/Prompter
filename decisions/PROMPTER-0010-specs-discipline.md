---
id: PROMPTER-0010
title: Specs discipline with the Cratis.Specifications BDD style and an eval gate
status: accepted
stage: implemented
class: working
reversibility: reversible
decided: 2026-07-15
decider: woksin
applies-to:
  - "Specs/**"
  - "Eval/**"
enforced-by: "not verified — the Specs and Eval projects exist; a CI gate on the groundedness score would verify the merge-blocking claim"
review-trigger: "A change to the specification style, or the golden Q&A eval harness no longer gating merges."
---

Historical decision D-10, 2026-07-15.

## Decision

Cratis.Specifications BDD style throughout (`for_<Type>/when_<behavior>/…`), same as every other repository. Pure logic (chunking, hashing, RRF math, prompt assembly, refusal thresholds) gets specs from day one.

The golden Q&A eval harness (M4) is the spec suite for answer quality: a failing groundedness score blocks merge the same way a failing spec does.
