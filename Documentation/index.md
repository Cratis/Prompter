---
title: Prompter
description: The Cratis community's documentation assistant on Discord - answers questions grounded in the published docs, with citations on every answer.
---

Prompter is the Cratis community's documentation assistant on Discord. Like a theater prompter, it sits just
offstage with the script - the published documentation at [cratis.io](https://cratis.io) - and feeds you the
line you are missing.

## What it does

- Answers questions when you mention **@Prompter** or use **/ask** in the community Discord.
- Grounds every answer strictly in the documentation and cites the pages it used.
- Refuses honestly when the documentation does not cover a question, instead of guessing.
- Stays current automatically - the corpus re-indexes when documentation changes are merged.

## What it does not do

- It does not answer from general knowledge, and it does not interject into conversations uninvited.
- It does not replace humans - answers it cannot ground get routed to the community.

## Asking good questions

Ask complete questions in one message, mention the product you are working with, and include the error or
code shape when relevant. Prompter answers from the documentation, so questions about undocumented internals
are better asked to a human.

## Privacy

Prompter stores questions and answers for quality follow-up with a one-way hash of the asker's Discord
identifier - never the identifier itself - and purges interactions after 90 days. Model inference runs
through the Anthropic API, which does not train on the data.
