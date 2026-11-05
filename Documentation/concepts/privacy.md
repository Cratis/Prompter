---
title: Privacy
description: Exactly what Prompter stores when you ask it a question, for how long, and your rights.
---

Before asking Cratis Prompter, remove credentials and personal information from the text you post. A question,
a bot reply and a forwarded excerpt are Discord messages, separate from the records Prompter writes to its
database. Keeping message text out of an interaction row does not make the whole conversation private.

Prompter is [open source](https://github.com/Cratis/Prompter). Its code describes the processing below, but
provider terms, infrastructure location and legal obligations also depend on the service's operators and
agreements; source code alone cannot establish them.

## What is stored when you ask a question

The interaction log records an answer's operational details, not its question text, answer text or Discord
user ID. Each row has an interaction id and timestamp, with the surface, cited pages, score, refusal status,
category and product, plus feedback when it is recorded. That row is useful for investigating answer quality;
it is not a copy of the Discord conversation.

| Data | Stored as | Why |
|---|---|---|
| Which surface it was asked on | The surface type, not the question text or Discord user ID in the interaction row | Understanding which surfaces get used |
| Which docs pages the answer cited | Cited page URLs, which can also identify other configured Cratis sources | Finding the material behind an answer |
| Whether Prompter answered or refused, and how confident it was | A flag and score, with category and product codes in the interaction row | Investigating retrieval and documentation gaps |
| Thread follow-ups | Thread IDs and, for answer threads, the asker's ID in memory, with a 24-hour follow-up window refreshed by answers | Continuing the conversation without another mention while follow-ups are active |
| Ask a human | A Discord message in the same channel with a link and asker mention when the request succeeds, plus a question excerpt when available | Asking a configured maintainer role to help |
| This should be documented, Request this | A sanitized excerpt and question link posted to the configured maintainer channel when the asker's forward succeeds | Showing maintainers a documentation gap or requested capability |
| Your 👍/👎 | The verdict attached to the interaction row when the feedback write succeeds, not the clicker's identity in that row | Identifying answers worth investigating |

The bot also holds temporary state outside that log. Admission and feedback limits use keyed hashes;
thread follow-ups use the identifiers described above. Operator configuration can contain allowed or denied
user IDs. The `/answering` pause flag is persisted without an actor identity. These are different uses of
information, so the absence of a user-ID column is not a guarantee of anonymity or an exemption from legal obligations.

Interaction retention uses the configured `RetentionDays` value, whose code default is 90 days. The bot's
daily purge removes older interaction rows when it succeeds; failures can delay removal. This is not an
exact deletion deadline and does not describe retention of Discord messages, provider data or backups.
The database also holds operational bookkeeping such as spend/provider-attempt records and issue-delivery
markers; the interaction row is not an inventory of every stored record.

## Who else sees your question

Your original message and any public reply remain part of the Discord conversation. Selecting **Ask Prompter**
on a person's text message requests a public answer; only the acknowledgment is private. For an admitted
question, Prompter can read bounded earlier messages in the same public channel or thread as context.
It filters recognized mention, identity and secret-like shapes before downstream processing, but that
filter cannot guarantee anonymity or remove every sensitive detail. Remove such information yourself before posting.

- **Anthropic** (Claude) receives the question and retrieved passages when an answer is generated, with
  bounded conversation context when available.
- **Voyage AI** receives question text for query embeddings used in retrieval.

Depending on configuration, request excerpts can also go to a private classification endpoint, and keywords
derived from the current question can go to GitHub for related-issue searches. Public issue lookup caches
its derived keywords and results in memory. Neither an advisory issue link nor a refusal files an issue.

Human-help and maintainer-forward buttons share the excerpts and links described in the table only when
the relevant action is used and succeeds. If you use `/issue`, an expiring in-memory preview also holds
the draft and reporter/scope identifiers. Review it before confirming public filing; [GitHub consent](../github-consent.md)
describes that separate flow. Feedback buttons do not provide filing or forwarding consent.

Discord and the external services have their own retention and data-use terms. Prompter's interaction-log
schema does not establish whether those services retain text, use it for training or store it in a particular
country. Ask a maintainer which deployment settings and service terms apply before sharing sensitive material.

## Your rights

Contact a maintainer on the [Cratis Discord](https://discord.gg/kt4AMpV8WV) about access, deletion, retention
or where the service processes information. Distinguish a Discord message, an interaction record and an
external service's processing when describing your concern. The lack of message text or user IDs in the
interaction log does not by itself decide your rights or mean that there is nothing personal to address.
