---
title: Report something with Prompter
description: Turn a Discord conversation into a GitHub issue on the right Cratis repository, without leaving the chat.
---

Use `/issue` to draft a GitHub issue from Discord, then review it before deciding whether to file it.
Prompter prepares the report; you decide whether its destination and contents are right.

## Filing something

Where issue filing is enabled, use the `/issue` command with a description of what happened. The preview
is private, but the issue you approve is public. Keep personal information and secrets out of your
description, and review the draft before confirming.

```text
/issue The projection stops updating after I rename a property. It worked in 16.0.4.
```

Prompter drafts the issue - a title, a body, what kind of work it is, and which repository it belongs in -
and shows it back to you privately. Nobody else sees the draft. Review the destination and every field
before choosing; you have fifteen minutes:

- **Create issue** - it is filed, and you get the link.
- **Cancel** - nothing happens, and the draft is discarded.
- Neither - the draft expires by itself.

No issue is created until you choose **Create issue**. That is deliberate: filing is always a person's decision,
never an inference from something you typed. Before showing the preview, Prompter does search GitHub for
possibly related open issues, using keywords from the drafted title only.

## What Prompter fills in

The drafted issue carries what you said, a link back to the Discord conversation, and a note that Prompter
filed it on your behalf. The link is how a maintainer asks you a follow-up question - your Discord name is
never written into the issue, because the thread already knows who was there and a public tracker does not
need to.

It also picks the repository from what you described. When it cannot tell which product you mean, it says so
in the preview rather than guessing quietly. Confirm only if the destination and every field are right for
a public issue.

## Anything worth tracking

`/issue` is not only for bugs. Use it for a missing API, a feature you want, a rough idea worth discussing, or
documentation that does not exist or cannot be found. Prompter works out which it is and labels it
accordingly.

Prompter may send suggestions for possibly related open issues in a separate private message. These are
not confirmed duplicates and are not included in the posted issue. Review them before deciding whether
to open a new issue or comment on an existing one.

## When Prompter answers your issue

On repositories that opt in, Prompter also reads newly-opened issues and comments with a grounded answer when
the documentation covers the question. If it cannot answer from the docs, it stays quiet - silence on a
tracker costs nothing, and a hedged guess costs a maintainer's attention.

To stop it commenting on a particular issue, label the issue `no-prompter`.

## What is not stored

Prompter does not store what you write here. While the preview is open, the draft and the identifiers needed
to deliver it (you, the server and the channel) are held in memory only, and are dropped when you confirm,
cancel or let it expire. The interaction log keeps its usual [privacy](../concepts/privacy.md) posture: no
message content, nothing that identifies you. What becomes public is exactly what you approved in the preview.
