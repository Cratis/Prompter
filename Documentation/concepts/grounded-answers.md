---
title: Grounded answers
description: Why every answer carries citations, why refusals are a feature, and how the knowledge stays current without retraining.
---

Prompter is not "trained on" the Cratis documentation - no model ever memorizes it. Instead it uses
retrieval-augmented generation (RAG): for every question it *looks the answer up* in the documentation and
has a language model phrase what it found. That distinction drives everything you experience as a user.

## Answers are looked up, not remembered

When documentation changes, Prompter's knowledge updates within a day through the nightly refresh;
maintainers can also re-index on demand. After re-indexing, the next question retrieves the new content
from [cratis.io](https://cratis.io). There is no retraining or release.

## Citations are the contract

The language model is instructed to answer *only* from the retrieved passages and to cite the pages it
used. Passages from the same page share one citation number. Discord shows numbered `Sources:` links,
and the `ask` CLI lists numbered source URLs: `[1]` in the answer always points to source `[1]`.
Only referenced pages are listed, in page order, with numbers compacted if the answer skips pages.
For example, citing only the second and fifth retrieved pages produces sources `[1]` and `[2]`,
and the answer's original `[2]` and `[5]` markers are rewritten to match. Invalid page markers are removed.
If the answer has no valid references, Prompter offers up to four retrieved pages instead.
Verbose CLI diagnostics label individual excerpts as `passage 1`, `passage 2`, and so on; these are not citation numbers.

If an answer ever surprises you, follow the links: the answer should be recognizable from the source.
That's your verification path.

## Summaries lead to page details

A retrieved page summary can point to useful detail that ranked below it. Prompter looks up the best-matching
non-summary excerpts from admitted summary pages, reusing the question embedding. At most two detail excerpts
receive reserved slots within the existing passage limit. They must meet the same similarity threshold as other
passages; a weak summary cannot bypass the refusal gate. This lookup uses the existing index and needs no reindex.

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
refreshes nightly at 03:52 UTC through **Reindex - Production**, or on demand when a maintainer runs the
workflow on `main`. A one-shot Job indexes the published docs without a public reindex route.
