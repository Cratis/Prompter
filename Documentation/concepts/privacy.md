---
title: Privacy
description: Exactly what Prompter stores when you ask it a question, for how long, and your rights.
---

Prompter processes messages from community members, so it is deliberately conservative about what it keeps.
Because the bot is [open source](https://github.com/Cratis/Prompter), everything on this page is verifiable
in code.

## What is stored when you ask a question

Prompter keeps **no personal data** — no message content and nothing that identifies you. The only thing
recorded is anonymous operational signal about each answer:

| Data | Stored as | Why |
|---|---|---|
| Which surface it was asked on | The surface type (mention, `/ask`, ask channel, forum) — not the message, channel, or user | Understanding which surfaces get used |
| Which docs pages the answer cited | The page URLs | Measuring grounding and finding documentation gaps |
| Whether Prompter answered or refused, and how confident it was | A flag and a score | Tracking refusal rate and answer quality |
| Your 👍/👎 | The verdict, attached to that anonymous row | The quality feedback loop |

Your **question and the answer text are not stored** — they are processed to produce the reply and then
discarded. Your **Discord identity is never stored**: the bot throttles each person only in memory, and that
key is never written to disk or logs. None of the recorded signal can be traced back to you, so the
anonymous rows fall outside data-protection obligations entirely. Prompter does not read or store DMs, does
not store messages that aren't questions to it, and never posts anywhere it wasn't asked.

## Who else sees your question

To answer you, your question text is sent to two external services and then discarded — neither they nor
Prompter retain it for training:

- **Anthropic** (Claude) generates the answer from your question and the retrieved documentation passages.
  Anthropic does not train on API data.
- **Voyage AI** computes an embedding of the question for retrieval.

Everything Prompter stores at rest (the anonymous signal above) lives on Cratis-operated infrastructure in
Norway.

## Your rights

Because Prompter stores nothing that identifies you and keeps none of your message content, there is nothing
personal to export or delete — the recorded signal is anonymous by construction. Questions about how it works,
or about the processing above, go to a maintainer on the [Cratis Discord](https://discord.gg/kt4AMpV8WV).
