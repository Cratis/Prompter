---
id: PROMPTER-0001
title: Name the project Prompter
status: accepted
stage: implemented
class: product
reversibility: costly
decided: 2026-07-15
decider: woksin
applies-to:
  - "**"
enforced-by: "not verified — the repository, package and image names already use Prompter; nothing automated enforces the name"
review-trigger: "A naming conflict or a rename proposal for the repository, the container image or the bot."
---

Historical decision D-1, 2026-07-15.

## Context

The project needed a name that fits the Cratis storytelling family (Chronicle, Narrator, Lens, Arc, Studio).

## Decision

The project is named Prompter. In theater, the prompter sits just offstage with the script and feeds the line to anyone who forgets. The docs are the script; the bot whispers the answer. The name also carries the LLM-prompt double meaning.

## Consequences

GitHub `Cratis/Prompter` and Docker Hub `cratis/prompter` were free at decision time.
