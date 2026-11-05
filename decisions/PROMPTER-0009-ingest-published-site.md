---
id: PROMPTER-0009
title: Ingest the published documentation site, not the product repositories
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-07-15
decider: woksin
applies-to:
  - "Source/Ingestion/**"
enforced-by: "not verified — an ingestion spec that only reads the llms.txt index and per-page Markdown mirrors would verify it"
review-trigger: "A change to the ingestion source, or the Documentation site no longer generating the Markdown mirrors and llms.txt index."
---

Historical decision D-9, 2026-07-15.

## Decision

Ingest the per-page Markdown mirrors and the `llms.txt` index that the Documentation site already generates on every deploy (aggregated, DocFX-isms stripped, links resolved), instead of crawling about 15 product repositories and re-implementing `sync-content.mjs`. Exclude Chronicle's generated `client-snippets/` fragments.

## Consequences

Freshness rides the existing `build-docs` `repository_dispatch`: the Documentation repository already tells the world when docs change, and Prompter subscribes via a protected webhook (M5).
