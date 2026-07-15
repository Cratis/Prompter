---
title: Grounded answers
description: Why every answer carries citations, why refusals are a feature, and how the knowledge stays current without retraining.
---

Prompter is not "trained on" the Cratis documentation - no model ever memorizes it. Instead it uses
retrieval-augmented generation (RAG): for every question it *looks the answer up* in the documentation and
has a language model phrase what it found. That distinction drives everything you experience as a user.

## Answers are looked up, not remembered

When documentation changes, Prompter's knowledge updates within minutes - the changed pages are re-indexed
and the very next question retrieves the new content. There is no retraining, no release, no lag between what
[cratis.io](https://cratis.io) says and what Prompter says.

## Citations are the contract

The language model is instructed to answer *only* from the retrieved passages and to reference which ones it
used - those pages become the `Sources:` links on every answer. If an answer ever surprises you, follow the
links: the answer should be recognizable from the source. That's your verification path, and it's why answers
without sources don't exist.

## Refusals are a feature

Before answering, Prompter scores how well the best retrieved passages match your question. Below a
threshold, it refuses - *"I couldn't find anything in the Cratis documentation that answers this with
confidence"* - rather than letting the model improvise. A wrong-but-confident code snippet costs you more
than an honest "ask a human," so honesty wins by design.

Refusals also have a second life: they are logged (see [Privacy](privacy.md) for what exactly is stored) and
reviewed as *documentation gaps*. If Prompter keeps refusing a reasonable question, that's a page someone
should write - and the community's 👎 reactions work the same way.

## What "current" means precisely

Prompter's knowledge is the published documentation - nothing more. A feature that shipped without
documentation is invisible to it, exactly as it is to a human reader of cratis.io. The knowledge base
refreshes automatically when documentation deploys, with a nightly sweep as a safety net.
