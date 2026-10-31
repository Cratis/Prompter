<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Retrieval grounding specs

Build the full solution in Release (warnings are errors), then run the executable xUnit v3 specs:

```bash
dotnet build --configuration Release
./Specs/bin/Release/net10.0/Prompter.Specs -trait- "Category=Integration"
```

The focused regression is under `for_Answers/when_applying_the_retrieval_gate`. It covers a
single-arm RRF `1/61` positive, unrelated dual-arm `2/61` negative, empty retrieval, the inclusive
cosine cutoff, invalid similarities, and a relevant passage below an unrelated first result.
Every answer-context passage and citation must qualify; RRF ordering must not change. The prior
history/injection spec retains its prompt assertions with realistic RRF and independent similarity.

## Disposable CI pgvector

CI uses the existing Postgres service slot, now pinned to pgvector `0.8.7-pg17` by image digest.
The spec step explicitly supplies `PROMPTER_RETRIEVAL_TEST_PORT=5432`. The port selects only a
loopback disposable fixture, never the application's configured database. No live configuration,
provider keys, or provider calls are needed. Each context creates and drops its own random schema;
the vector extension is created before mapped Npgsql connections and is never dropped by cleanup.
The production embedded chunks migration and real `Passages.Search` SQL run against that schema.

The two `for_Passages/when_searching` integration facts check:

- A synthetic 1024-dimensional Chronicle excerpt with cosine similarity approximately `0.8`,
  no full-question lexical match, and real semantic-only RRF `1/61` reaches fake chat with provenance.
- A question-dependent orthogonal query yields only zero-similarity candidates and refuses.
- A weak `0.2` cosine match with dual-arm RRF `2/61` still refuses.
- A lexical-only glossary match outside the semantic top 20 has cosine `-1`, refuses, and cannot
  reach chat. Distinct synthetic query vectors prevent accidental positive-fixture reuse.

These facts skip locally only if the disposable port is absent. An invalid supplied port or missing
vector extension fails instead of skipping. CI must execute both facts, not report new skips.
This task did not run local containers; the SQL facts are CI-required and locally unverified.

## Limits

Synthetic vectors test signal separation, SQL mapping, ordering, and refusal behavior. They do not
calibrate Voyage embeddings or demonstrate production recall or answer quality. See the
[answering readiness runbook](runbooks/answering-readiness.md) before live acceptance.
