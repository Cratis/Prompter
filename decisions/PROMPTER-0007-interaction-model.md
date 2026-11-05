---
id: PROMPTER-0007
title: Interact by mention, forum auto-reply and an ask channel, with no auto-chime in v1
status: accepted
stage: none
class: product
reversibility: reversible
decided: 2026-07-15
decider: woksin
applies-to:
  - "Source/Discord/**"
enforced-by: "not verified — a spec asserting the bot never replies unprompted would verify it"
review-trigger: "A proposal to let the bot interject unprompted, including the post-v1 confidence-gated chime-in experiment."
---

Historical decision D-7, 2026-07-15.

## Decision

Prompter answers through:

- an @mention anywhere,
- the `/ask` slash command,
- auto-reply to new threads in the designated help forum channel, and
- a dedicated #ask channel.

There are no unprompted interjections in text channels. Every surviving vendor converged on this model, and unsolicited bot answers erode trust faster than they help.

## Consequences

A confidence-gated chime-in on explicitly opted-in channels is a post-v1 experiment (parking lot), enabled per channel, threaded, and easy to mute.

[PROMPTER-0016](PROMPTER-0016-prompter-bridges-discord-and-trackers.md) extends this decision.
