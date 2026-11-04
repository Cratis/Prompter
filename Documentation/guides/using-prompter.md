---
title: Use Prompter on Discord
description: How to ask Prompter a question, phrase questions it can answer, read the cited sources, and understand its refusals.
---

Use Cratis Prompter to look up a Cratis question on the [Cratis Discord](https://discord.gg/kt4AMpV8WV), then
check the reply before applying it. Prompter searches indexed Cratis material, including the published
[documentation](https://cratis.io) and other configured sources. The [Your first answer](../getting-started/index.mdx)
walkthrough is a guided start if you have not used it before.

Remove credentials and personal information before posting. A question and its reply are Discord messages;
asking the bot is not a private conversation with an assistant.

## Ways to ask

Use a server channel where Prompter is enabled. Being able to read a channel is not enough: server,
channel and user restrictions can prevent a reply. If you do not know where to ask, check with a community member.

- **Mention it** - put `@Prompter <your question>` in one message. The answer may reply to that message or
  appear in a new thread, depending on the channel configuration.
- **`/ask`** - use the slash command for a standalone question in an enabled server channel. An admitted
  request is acknowledged while Prompter works; acknowledgment does not mean an answer has been delivered.
- **The ask channel** - if the operator configured one, post your question there without a mention.
  Ordinary channels do not automatically treat every message as a question.
- **The help forum** - open a post in the configured help forum with your question and relevant context.
  Prompter can answer automatically. Operators can also enable delayed replies in other forums; neither
  an immediate answer nor a human follow-up is guaranteed.

To ask about a message someone has already posted, select it and choose **Ask Prompter** from the message menu.
Only the acknowledgment is private to you: the requested answer is public on the message or in an applicable
answer thread. Use this only when that visibility is appropriate for the selected text.

For a follow-up, reply to Prompter's message and explain what you still need. In a tracked help-forum or
answer thread, the asker can continue without a mention while automatic follow-ups remain active. Another
human joining hands the conversation over; mention Prompter or reply to its message if you want it to answer again.
Include the product and relevant details instead of assuming the bot remembers the whole conversation.

When **Ask a human** is available, only the person who asked can use it. It requests a configured maintainer
role in the same channel, with a link and a short question excerpt when available; it does not guarantee a response.
**This should be documented** and **Request this**, when available, let the asker forward a sanitized excerpt
and question link to the configured maintainer channel. Check what you shared before forwarding: filtering
cannot remove every kind of sensitive information. A refusal or feedback click does not send that forward for you.
A refusal because the question is off-topic does not carry **This should be documented**.

## Ask a question it can answer

Name the product, the task and the part that stopped you. Those details give retrieval something concrete
to look for and help a person understand the same question if the bot cannot resolve it.

- **Name the product and feature** - "how do I define a command in Arc?" beats "how do commands work?".
- **Ask one thing at a time** - split multi-part questions into separate asks; each gets its own focused
  retrieval.
- **Paste the error or the exact term** - real symbols and messages match the docs better than a paraphrase.
- **Skip the yes/no framing** - "what's the difference between a projection and a reducer?" gets you more than
  "is a projection a reducer?".

Use only the relevant error text, with secrets and personal details removed. If the first reply misses
your situation, say what differs rather than repeating the same question unchanged.

## Read the sources

If the reply has a `Sources:` line, open a linked page and compare it with the claim or instruction you
intend to use. Check the product, feature and relevant version on that page. A link is a way to inspect
the evidence, not proof that the generated answer is correct or suitable for your project.

Prompter can also ask a clarifying question or refuse without citations. Neither means your task is
unsupported. [Grounded answers](../concepts/grounded-answers.md) explains how to interpret those results.

## When Prompter says it doesn't know

A reply such as "I couldn't find anything in the Cratis documentation that answers this with
confidence" means Prompter did not find enough grounding for this question. Try naming the exact product,
feature or error. Rephrasing can help retrieval, but it cannot make missing source material appear.

If you still need help, explain what remains unresolved in the same channel or thread. Use **Ask a human**
when it is offered, or ask the community directly. If the cited page itself is missing information, say
which part you needed; do not assume that a refusal automatically assigned the problem to a maintainer.
See [Grounded answers](../concepts/grounded-answers.md) for the limits of refusals and citations.

## If you ask a lot at once

Limits depend on the operator's configuration. The default per-user bucket holds 10 questions and refills
over a ten-minute window, with at least 15 seconds between admissions and 30 questions per UTC day. The
daily allowance resets at 00:00 UTC. Follow the wait time in a limit reply instead of repeatedly retrying.

Repeated requests rejected by per-user admission limits can cause a longer pause. With the defaults,
five such refusals within ten minutes start a pause of about five minutes; repeats double it up to six
hours. These are admission refusals, not answers saying the sources lack grounding. Waiting does not add
a refusal. Other limits, paused answering or a delivery failure can also prevent a reply, and limit
messages are themselves throttled. If the bot is silent, check the channel and asking method before retrying.

## React to make it better

Use **👍 Helpful** or **👎 Not helpful** when the feedback buttons are present. A recorded verdict updates
the answer's interaction row, helping identify replies or cited pages worth investigating. Recording is
best-effort; a button click is not a request for human help and does not guarantee someone will read the thread.

For a wrong answer, say what does not match your task and which claim or source needs checking. That gives
another person something to investigate. [Privacy](../concepts/privacy.md) distinguishes the interaction
record from temporary request state, Discord messages and external processing.
