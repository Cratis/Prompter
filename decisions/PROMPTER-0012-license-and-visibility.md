---
id: PROMPTER-0012
title: Open source under MIT, public from day one
status: accepted
stage: implemented
class: strategy
reversibility: costly
decided: 2026-07-15
decider: woksin
applies-to:
  - "**"
enforced-by: "not verified — the repository is public and carries an MIT LICENSE file; nothing automated enforces visibility"
review-trigger: "A proposal to close the source or change the license."
---

Historical decision D-12, 2026-07-15. Ruled by the team when creating the repository; `https://github.com/Cratis/Prompter` was created public the same day.

## Decision

Open source, MIT, public from day one.

## Grounds

- The whole Cratis org is MIT/public. A closed bot answering questions about an open platform from public docs would be off-brand, and community trust in a devtools Discord depends on it.
- There is nothing to protect. The code is a thin RAG pattern over public content; the valuable assets (docs, community, interaction data, keys) are not in the code; and the 2025–26 vendor consolidation shows "bot as a paid product" is not a business worth preserving optionality for.
- Transparency is a GDPR asset: the privacy notice can link to the source proving the data handling ([PROMPTER-0008](PROMPTER-0008-gdpr-posture.md)).
- It is a showcase for the Cratis AI-native story (.NET 10 + Microsoft.Extensions.AI + Claude + pgvector with a measured eval gate). A configurable-docs-site stretch makes it adoptable by any Starlight project.

## Consequences

Accepted trade-off: the system prompt and refusal thresholds are readable. Obscurity is weak protection anyway, and blast radius is capped by minimal permissions and rate limits.

Guardrails now that it is public:

- Credentials stay in environment variables and encrypted Pulumi config only (already true).
- Deployment state lives with the stack ([PROMPTER-0015](PROMPTER-0015-pulumi-stack-lives-in-this-repo.md)).
- The README should set expectations: built for the Cratis community, PRs welcome, no support promises. The log noted this as still to add.
