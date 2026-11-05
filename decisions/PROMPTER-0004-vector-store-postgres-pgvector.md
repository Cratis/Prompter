---
id: PROMPTER-0004
title: Use Postgres with pgvector and hybrid search in SQL as the vector store
status: accepted
stage: implemented
class: contract
reversibility: costly
decided: 2026-07-15
decider: woksin
applies-to:
  - "Source/**"
  - "Docker/**"
  - "Deployment/**"
enforced-by: "not verified — Pgvector is referenced in Directory.Packages.props; nothing automated prevents another store"
review-trigger: "A change of vector store, or the corpus growing beyond what an exact scan handles (the decision assumed about 20k chunks)."
---

Historical decision D-4, 2026-07-15.

## Decision

One Postgres on the same box, `Pgvector` + Npgsql, exact scan (no ANN index needed at about 20k chunks), and hybrid retrieval (tsvector BM25 + cosine, RRF-fused) as one SQL query.

Not used: a dedicated vector database, or the Microsoft.Extensions.VectorData connector for search (its Postgres connector lacks hybrid support, so we write the SQL).

## Consequences

Anthropic's contextual-retrieval benchmarks show hybrid roughly halves top-20 retrieval failure. This is the one quality technique committed to from day one.
