---
id: PROMPTER-0005
title: Use Claude for answers and Voyage for embeddings
status: accepted
stage: implemented
class: contract
reversibility: reversible
decided: 2026-07-15
decider: woksin
applies-to:
  - "Source/Answering/**"
  - "Source/Retrieval/**"
  - "Source/AnthropicOptions.cs"
enforced-by: "not verified — the Anthropic package is referenced in Directory.Packages.props; both models sit behind Microsoft.Extensions.AI abstractions"
review-trigger: "A change of answer or embedding model or provider, or a requirement for EU-region inference."
---

Historical decision D-5, 2026-07-15.

## Decision

- Answer generation: Claude Sonnet via the official `Anthropic` NuGet SDK exposed as `Microsoft.Extensions.AI.IChatClient`, with a prompt-cached system prompt.
- Embeddings: Voyage `voyage-4` behind our own `IEmbeddingGenerator`. Anthropic has no embeddings endpoint, and Voyage's free tier covers our corpus hundreds of times over.

Both sit behind Microsoft.Extensions.AI abstractions, so swapping models is config, not code.

## Consequences

Model choice here is a quality decision, not a cost one (whole-community LLM spend is $1–10/month).
