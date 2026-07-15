---
title: Why Prompter
description: The problem Prompter solves for the Cratis community - and the situations where a human is still the right answer.
---

In theater, the prompter sits just offstage with the script and quietly feeds the line to anyone who blanks
mid-scene. That's the role: the script is the [Cratis documentation](https://cratis.io), the stage is the
community Discord, and when someone forgets their line - *"wait, how do I append an event again?"* - Prompter
whispers it back, with citations.

## The problem

Most questions asked in a developer community are already answered in the documentation - the asker just
doesn't know where, and the humans who do know aren't always awake. Without help, every such question costs
twice: the asker waits, and a maintainer spends attention on something the docs already cover. The questions
that genuinely need human judgment queue up behind the ones that don't.

## What Prompter changes

The documentation answers first. Questions with a documented answer get one in seconds, with source links, in
the channel where they were asked. Humans keep the interesting ones. And because Prompter logs what it
*couldn't* answer, every miss becomes a signal for what documentation to write next - the bot pays the docs
back.

## Why it only answers from the documentation

A community bot that guesses is worse than no bot: a confidently wrong code snippet costs more time than an
unanswered question. Prompter is deliberately constrained - it retrieves passages from the documentation and
answers only from them, cites what it used, and refuses when the material isn't there. How that works is
covered in [Grounded answers](grounded-answers.md).

## When not to rely on it

- **Undocumented territory** - internals, edge-case behavior, or brand-new features the docs don't cover yet.
  Prompter will refuse; ask the community.
- **Judgment calls** - "should we use event sourcing for this system?" deserves a conversation, not a
  retrieval. Prompter can quote the trade-off pages, but the decision is yours.
- **Roadmap and plans** - Prompter knows what *is*, not what's coming. Ask the maintainers.
- **Anything where the answer matters more than the speed** - Prompter is a first answer, not a final
  authority. The sources are linked precisely so you can verify.
