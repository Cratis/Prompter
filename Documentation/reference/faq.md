---
title: FAQ
description: Find answers to common Prompter questions about missing replies, incorrect answers, index freshness, AI providers, channel access and self-hosting.
---

## Why didn't it answer me?

First check that you used an asking method in a server channel where Prompter is enabled: a mention,
`/ask`, a configured ask channel or help-forum post, a reply to the bot, or the **Ask Prompter** message menu.
Server, channel and user restrictions still apply. The guide to [using Prompter](../guides/using-prompter.md)
explains those choices and how to continue a conversation.

If you receive a wait time, wait before trying again. If there is no reply, do not assume your question
reached the answer pipeline: paused answering, limits or a delivery failure can leave it unanswered.
A refusal is different from silence: it means retrieval did not find enough grounding, not that the
feature is unavailable. Read the [grounding limits](../concepts/grounded-answers.md) before drawing that conclusion.

## The answer was wrong. What do I do?

If the reply includes sources, open one and compare the exact claim with the page. Say in the channel or
thread what differs from your situation. Use **👎 Not helpful** when that feedback button is present;
a recorded verdict helps investigation, but does not itself request or guarantee human follow-up.

Ask the community directly, or use **Ask a human** when it is available to the person who asked. If the
source page is wrong or missing a step, name that page and the information you needed so someone can
investigate the documentation as well as the answer.

## How up to date is it?

The optional indexing schedule requests a run daily at 03:52 UTC when the repository variable
`PROMPTER_SCHEDULED_INDEXING_ENABLED=true` enables scheduled indexing. A maintainer can also run that
workflow on `main`. A scheduled time or documentation edit is not evidence that a reindex completed;
failed or disabled indexing can leave older material in use.

Prompter searches the material already in its index, including documentation and other configured Cratis
sources. Open the cited page and check the version and context you need. Neither a recent index nor a
citation guarantees that the answer covers the current behavior of your project.

## Which AI does it use?

Claude (Anthropic) generates replies from the question and retrieved passages when generation is needed;
Voyage AI computes query embeddings for retrieval. A request can also use bounded conversation context
and configured classification or issue-search services. [Privacy](../concepts/privacy.md) explains those
dataflows separately from Prompter's own interaction log; provider retention and training terms are not
established by that log's contents.

## Does it read my DMs or other channels?

DMs are not a supported way to ask Prompter. For an admitted server question, it can read bounded earlier
messages in the same public channel or thread as context, including messages not individually addressed
to it. That is different from writing those messages into the interaction log.

Prompter also uses temporary identifiers and keyed hashes for follow-ups, admission and button limits.
Do not treat the absence of user IDs in an interaction row as a promise that no identity is processed.
[Privacy](../concepts/privacy.md) explains the storage and sharing boundaries.

## Can I run it for my own project?

Running an instance is an operator task, not a prerequisite for asking on the community server. The code
is [MIT-licensed on GitHub](https://github.com/Cratis/Prompter); [Run Prompter locally](../guides/running-locally.md)
is the setup guide. Prompter is built for the Cratis community first, with no support promises for another deployment.
