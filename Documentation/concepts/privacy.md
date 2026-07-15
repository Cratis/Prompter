---
title: Privacy
description: Exactly what Prompter stores when you ask it a question, for how long, and your rights.
---

Prompter processes messages from community members, so it is deliberately conservative about what it keeps.
Because the bot is [open source](https://github.com/Cratis/Prompter), everything on this page is verifiable
in code.

## What is stored when you ask a question

| Data | Stored as | Why |
|---|---|---|
| Your Discord identity | A **one-way hash** - never your ID or username | Rate limiting and abuse control need to tell users apart; nothing needs to know who you are |
| Your question and the answer | Text, for **90 days**, then purged automatically | Measuring answer quality and finding documentation gaps |
| Where it was asked | The surface (mention, `/ask`, ask channel, forum) - not the message or channel ID | Understanding which surfaces work |
| Your 👍/👎 reaction | The verdict on the answer row | The quality feedback loop |

Prompter does not read or store DMs, does not store messages that aren't questions to it, and never posts
anywhere it wasn't asked.

## Who else sees your question

Two external services process question text, both under terms that exclude training on it:

- **Anthropic** (Claude) generates the answer from your question and the retrieved documentation passages.
  Anthropic does not train on API data.
- **Voyage AI** computes an embedding of the question for retrieval.

Everything stored at rest lives on Cratis-operated infrastructure in Norway.

## Your rights

You can ask for your interactions to be deleted - contact a maintainer on the
[Cratis Discord](https://discord.gg/kt4AMpV8WV) and the rows matching your hashed identity are removed.
Retention and processing questions go to the same place.
