---
id: PROMPTER-0003
title: Use NetCord as the Discord library
status: accepted
stage: implemented
class: contract
reversibility: costly
decided: 2026-07-15
decider: woksin
applies-to:
  - "Source/Discord/**"
  - "Directory.Packages.props"
enforced-by: "not verified — NetCord is referenced in Directory.Packages.props; nothing automated prevents another Discord library"
review-trigger: "NetCord pre-1.0 breakage costs more than it saves."
fallback: "Discord.Net 3.20.x if beta breakage costs more than it saves; the Discord feature folder is the only thing that would change."
---

Historical decision D-3, 2026-07-15.

## Decision

Use NetCord (net10.0-only, Generic Host/DI-native, most active development) over Discord.Net and DSharpPlus. It matches how we build hosts everywhere else.

## Consequences

Accepted risk: pre-1.0 churn.

## Fallback

Discord.Net 3.20.x if beta breakage costs more than it saves. The Discord feature folder is the only thing that would change.
