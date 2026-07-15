---
title: FAQ
description: Quick answers about Prompter - why it didn't answer, how fresh it is, and what to do about a bad answer.
---

## Why didn't it answer me?

Three possibilities: you weren't in one of its surfaces (mention it, use `/ask`, the ask channel, or a
help-forum thread - it ignores everything else by design); you hit the per-user rate limit (a few questions
per ten minutes - it tells you when that happens); or it refused because the documentation doesn't cover your
question - which is [deliberate](../concepts/grounded-answers.md).

## The answer was wrong. What do I do?

React 👎 and say so in the channel - a human picks it up, and the verdict feeds the quality measurements.
Every answer links its sources, so you can check where the wrong claim came from; if the documentation itself
is wrong, that's a valuable bug report.

## How up to date is it?

Within minutes of documentation changes - the knowledge base re-indexes automatically when
[cratis.io](https://cratis.io) deploys. It only knows published documentation: undocumented features are
invisible to it.

## Which AI does it use?

Claude (Anthropic) writes the answers from retrieved documentation passages; Voyage AI computes the
embeddings used for retrieval. Neither trains on your questions - see [Privacy](../concepts/privacy.md).

## Does it read my DMs or other channels?

No. It only processes messages on its answer surfaces, and it stores a one-way hash of your identity - never
your username. [Privacy](../concepts/privacy.md) lists exactly what is kept and for how long.

## Can I run it for my own project?

It's [MIT-licensed on GitHub](https://github.com/Cratis/Prompter) and points at any docs site that publishes
a sitemap with markdown mirrors - see [Run Prompter locally](../guides/running-locally.md). It's built for
the Cratis community first; PRs are welcome, but there are no support promises.
