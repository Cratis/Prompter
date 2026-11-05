---
id: PROMPTER-0002
title: Build a custom bot instead of buying a hosted documentation assistant
status: accepted
stage: implemented
class: strategy
reversibility: costly
decided: 2026-07-15
decider: woksin
applies-to:
  - "**"
enforced-by: "not verified — the bot is built in this repository; nothing automated enforces the choice"
review-trigger: "Reversing the build-versus-buy choice, or a hosted vendor offering Discord-only, zero-retention, EU-friendly terms at a cost that beats the custom bot."
fallback: "If we ever reverse: Inkeep (BYO-LLM keys, zero-retention options) over kapa for GDPR reasons."
---

Historical decision D-2, 2026-07-15.

## Context

Hosted options considered: kapa.ai, Inkeep, CrawlChat and Onyx.

## Decision

Build a custom bot instead of buying one of those products.

## Grounds

- Discord is the only surface we need, which is kapa's thinnest integration.
- kapa's paid tier is about $12–25k per year with a discretionary OSS-program exit clause.
- The vendor category consolidated hard through 2025–26 (Mendable dead, Threado dissolved, RunLLM pivoted).
- kapa hosts US-only with an enterprise-gated DPA, which is friction for a Norway-based controller.
- Our docs pipeline already emits LLM-ready artifacts, making the build about 9–12 developer-days at ≤ €15/month run cost.

The run-cost figure was later superseded by [PROMPTER-0011](PROMPTER-0011-deploy-on-existing-upcloud-cluster.md).

## Fallback

If we ever reverse: Inkeep (BYO-LLM keys, zero-retention options) over kapa for GDPR reasons.
