---
title: Use Prompter on Discord
description: How to ask Prompter a question, phrase questions it can answer, read the cited sources, and understand its refusals.
---

Prompter is the Cratis community's documentation assistant. It lives on the
[Cratis Discord](https://discord.gg/kt4AMpV8WV), answers questions from the published docs at
[cratis.io](https://cratis.io) with citations, and refuses honestly when the docs don't cover something. This
guide is the practical how-to for getting good answers out of it. If you have never used it, the
[Your first answer](../getting-started/index.mdx) walkthrough is a faster start.

## Four ways to ask

Prompter only answers when you use one of its surfaces - it never joins normal conversation. Pick the one that
fits the moment:

- **Mention it** - `@Prompter <your question>` in any channel it can read. It replies to your message, right
  where you asked. Best for a quick question in the flow of a conversation.
- **`/ask`** - the slash command, anywhere. It acknowledges immediately and posts the answer in the channel.
  Best for a clean, standalone question.
- **The ask channel** - in the dedicated ask channel every message is treated as a question, so you can just
  type. Best when you have a run of questions.
- **The help forum** - open a new post in the help forum as you normally would; Prompter takes the first swing
  automatically and a human follows up in the same thread. Best when you want a person in the loop too.

## Ask a question it can answer

Prompter looks your answer up in the documentation, so a specific question retrieves better than a vague one:

- **Name the product and feature** - "how do I define a command in Arc?" beats "how do commands work?".
- **Ask one thing at a time** - split multi-part questions into separate asks; each gets its own focused
  retrieval.
- **Paste the error or the exact term** - real symbols and messages match the docs better than a paraphrase.
- **Skip the yes/no framing** - "what's the difference between a projection and a reducer?" gets you more than
  "is a projection a reducer?".

If Prompter refuses, rephrasing with the precise term from the docs is often all it takes.

## Read the sources

Every answer ends with a `Sources:` line linking the documentation pages it used. Those links are the point:
open one and the answer should be recognizable from the page. That is your verification path - an answer with
no sources does not exist, and if something surprises you, follow the link before trusting it.
[Grounded answers](../concepts/grounded-answers.md) explains why citations are the contract.

## When Prompter says it doesn't know

Sometimes you get an honest "I couldn't find anything in the Cratis documentation that answers this with
confidence" instead of an answer. That is deliberate, not a failure - Prompter would rather admit a gap than
improvise a wrong-but-confident snippet. When it happens:

- **Ask a human** - the community picks up where the docs stop; refusals carry no sources and point you there.
- **Try a sharper phrasing** - the feature might be documented under a different term.
- **Treat it as a docs gap** - a reasonable question the docs can't answer is a page someone should write, and
  refusals are reviewed for exactly this.

See [Grounded answers](../concepts/grounded-answers.md) for why refusals are a feature.

## If you ask a lot at once

Prompter allows a handful of questions per user in a short window (about five every ten minutes). Past that it
replies with a friendly "give me a breather" instead of an answer - just wait a moment and continue. There is
no penalty; the limit only smooths out bursts.

## React to make it better

Every answer carries 👍 and 👎 reactions that Prompter adds itself. React honestly:

- 👍 when the answer helped - it confirms the docs and retrieval are working.
- 👎 when it missed - and say what was wrong in the channel, so a human can pick it up.

Your verdict is logged against the answer - never your username, see [Privacy](../concepts/privacy.md) - and
feeds both the quality measurements and the docs team's list of gaps.
