---
title: Report something with Prompter
description: Turn a Discord conversation into a GitHub issue on the right Cratis repository, without leaving the chat.
---

Good bug reports die in chat all the time. Someone hits a real problem at midnight, describes it perfectly in
a thread, gets a workaround, and the tracker never hears about it. The description was never the hard part -
the transcription was.

Prompter closes that gap. Describe the problem where you already are, and it writes the issue for you.

## Filing something

Use the `/issue` command with a description of what happened:

```text
/issue The projection stops updating after I rename a property. It worked in 16.0.4.
```

Prompter drafts the issue - a title, a body, what kind of work it is, and which repository it belongs in -
and shows it back to you privately. Nobody else sees the draft. You get three choices:

- **Create issue** - it is filed, and you get the link.
- **Cancel** - nothing happens, and the draft is discarded.
- Neither - the draft expires by itself after fifteen minutes.

Nothing reaches GitHub until you press the button. That is deliberate: filing is always a person's decision,
never an inference from something you typed.

## What Prompter fills in

The drafted issue carries what you said, a link back to the Discord conversation, and a note that Prompter
filed it on your behalf. The link is how a maintainer asks you a follow-up question - your Discord name is
never written into the issue, because the thread already knows who was there and a public tracker does not
need to.

It also picks the repository from what you described. When it cannot tell which product you mean, it says so
in the preview rather than guessing quietly, so you can correct it before anything is public.

## Anything worth tracking

`/issue` is not only for bugs. Use it for a missing API, a feature you want, a rough idea worth discussing, or
documentation that does not exist or cannot be found. Prompter works out which it is and labels it
accordingly.

If something similar is already open, Prompter shows it in the preview - commenting on the existing issue is
usually more useful than opening a second one.

## When Prompter answers your issue

On repositories that opt in, Prompter also reads newly-opened issues and comments with a grounded answer when
the documentation covers the question. If it cannot answer from the docs, it stays quiet - silence on a
tracker costs nothing, and a hedged guess costs a maintainer's attention.

To stop it commenting on a particular issue, label the issue `no-prompter`.

## What is not stored

Nothing you write here is kept by Prompter. The draft lives in memory until you confirm or it expires, and the
[privacy](../concepts/privacy.md) posture is unchanged: no message content, nothing that identifies you. What
becomes public is exactly what you approved in the preview.
