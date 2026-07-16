# Discord bot best practices — research for M3

How other docs/support/RAG Discord bots are built, distilled into actions for our M3 tasks
([`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md) P-11…P-16 + resilience). The behavior contract is
[`DISCORD_INTEGRATION.md`](DISCORD_INTEGRATION.md); the interaction model is settled in
[`DECISIONS.md`](DECISIONS.md) D-7 (mention + `/ask` + forum auto-reply + `#ask` channel, **no unsolicited
chime-in in v1**) and D-8 (hashed IDs, retention, privacy notice). Nothing here re-litigates those — it
sharpens the implementation. Every line is meant to be actionable.

Primary references used throughout:
[kapa.ai Discord bot docs](https://docs.kapa.ai/integrations/discord-bot),
[Answer Overflow (repo)](https://github.com/AnswerOverflow/AnswerOverflow) +
[indexing/consent docs](https://docs.answeroverflow.com/channel-settings/indexing),
[NetCord — responding to interactions](https://netcord.dev/guides/basic-concepts/responding-to-interactions.html),
[discord.js — command response methods](https://discordjs.guide/slash-commands/response-methods),
[Discord — rate limits](https://docs.discord.com/developers/topics/rate-limits).

---

## 1. Slash commands: ack, defer, ephemeral, followups → **P-11**

**Best practice.** An interaction token is valid for only **3 seconds** to make a first response; deferring
extends the window to **15 minutes** for edits/followups. If you *might* exceed 3 s, defer immediately; if you
know you'll answer within 3 s, don't. Deferring shows Discord's native "*&lt;app&gt; is thinking…*" affordance —
you don't build your own spinner. The **ephemeral flag is locked at defer time**: you cannot change
public↔ephemeral after the initial ack, so decide before you defer. The first followup after a defer *edits*
the "thinking…" placeholder rather than posting a new message.
Sources: [discord.js response methods](https://discordjs.guide/slash-commands/response-methods),
[NetCord interactions](https://netcord.dev/guides/basic-concepts/responding-to-interactions.html).

**Who does it well.** kapa.ai answers in ~30 s in-channel — only possible because it defers first
([kapa Discord docs](https://docs.kapa.ai/integrations/discord-bot)).

**Maps to P-11.**
- Answering takes 5–15 s → defer is mandatory, not optional. First line of the handler:
  `InteractionCallback.DeferredMessage()` (verified in NetCord docs). Do the retrieval/LLM call *after*.
- `/ask` is a **public** reply per our contract → defer **without** `MessageFlags.Ephemeral`. Do not pass the
  ephemeral flag anywhere in the `/ask` path (you can't add publicness back later).
- Deliver the answer via `ModifyResponseAsync(...)` (edits the placeholder) **or** `SendFollowupMessageAsync(...)`
  — both are documented NetCord methods. Prefer `ModifyResponseAsync` for the single-message case; use
  followups only when P-15 needs multiple messages.
- Rely on Discord's built-in "thinking…" — do not post your own "one moment" message (double message noise).
- Guard the whole thing with the P-11↔resilience contract: if generation throws or times out, still land a
  message inside the 15-minute window (edit the deferral to the apology) so the "thinking…" never hangs forever.

---

## 2. @mention handling → **P-12**

**Best practice.** Resolve **both** mention encodings — `<@id>` (user) and `<@!id>` (nickname) — because
Discord emits either depending on whether the user has a server nickname. Match on the bot's **own application
/ user id**, not on text. Explicitly **ignore** `@everyone`/`@here` and **role** mentions (a role the bot holds
should never trigger an answer), and **ignore other bots** (`message.Author.IsBot`) and the bot's own messages
(self-mention loops). Strip the mention token out of the text before treating the remainder as the question.
Sources: mention-escaping/ignoring patterns in
[discord.js #3882](https://github.com/discordjs/discord.js/issues/3882),
[discord.py #6069](https://github.com/Rapptz/discord.py/issues/6069).

**Who does it well.** kapa/Inkeep both use "mention in text channels, plain message in the dedicated channel" —
the mention is the explicit opt-in ([kapa Discord docs](https://docs.kapa.ai/integrations/discord-bot),
[Inkeep Discord](https://docs.inkeep.com/cloud/integrations/discord)).

**Maps to P-12.**
- Detect self-mention against `GatewayClient.Id` (the scaffold's assumption). The plan already flags this:
  if `Id` isn't populated at gateway-ready, resolve the application id via REST at startup and cache it — do
  that once, not per message.
- Handle `<@id>` **and** `<@!id>` (nickname). Write specs for both forms (already a P-12 "done when").
- Early-return guards, in order: author is a bot → ignore; message has no *user* mention of us (role/@everyone
  only) → ignore; mention present but text empty after stripping → optional gentle nudge or ignore.
- Reply as a **reply-reference** to the asking message (threads the Q&A visually). NetCord exposes a message
  reference on send — verify the exact property name against
  [netcord.dev](https://netcord.dev) when wiring it and record any rename in `DISCORD_INTEGRATION.md`
  (per the beta-churn note).

---

## 3. Dedicated `#ask` channel (the Prisma `#ask-ai` model) → **P-12 (Questions handler)**

**Best practice.** Give the bot **its own channel** and treat *every* plain message there as a question — no
mention needed. kapa explicitly recommends **not** bolting the bot onto an existing human help channel, so
members can tell "AI answers here" from "humans answer here." Name it clearly (`#ask-ai`, we use
`#ask-prompter`).
Sources: [kapa Discord docs](https://docs.kapa.ai/integrations/discord-bot),
[Prisma customer story](https://www.kapa.ai/customer-stories/prisma) (Prisma's `#ask-ai` answers 10k+
questions/month).

**Maps to P-12 / the ask-channel handler.**
- Gate on `Discord:AskChannelId`: in that channel, skip the mention check and answer plain messages; in all
  other channels, require a mention (from §2).
- Still apply the bot/self guards and rate limiting there (a busy `#ask` channel is exactly where a runaway
  loop or a spammer shows up).
- Pin the privacy notice **in this channel** (already in the runbook, step 6) — it's the highest-traffic AI
  surface, so it's where "you're talking to a bot, here's what it stores" belongs.

---

## 4. Forum / thread auto-reply → **P-13**

**Best practice.** On a **new thread/forum post**, fetch the **starter message** and answer it as the first
in-thread reply. Keep etiquette tight: answer once, don't re-answer every follow-up message unless mentioned,
and include a standing **"a human will follow up"** line so the AI answer never reads as the final word.
Auto-reply belongs in **forum/help channels**, not general chat. kapa makes forum auto-reply an explicit
**toggle** (on = answer all new posts; off = only when tagged) — worth mirroring as config rather than
hardcoding.
Sources: [kapa Discord docs — forum auto-reply mode](https://docs.kapa.ai/integrations/discord-bot),
[Answer Overflow indexing](https://docs.answeroverflow.com/channel-settings/indexing) (delays indexing ~6 h
"to allow for questions to be solved before they are indexed" — evidence the human answer should stay
primary).

**Maps to P-13.**
- Implement NetCord's guild-thread-create handler; filter to `Discord:HelpForumChannelId` only.
- **Fetch the starter message** (the forum post body) and feed *that* as the question — a forum thread's title
  is often too terse to retrieve on.
- Post exactly **one** first reply: cited answer + the standing line, e.g. *"This is an automated answer from
  the docs — a human will follow up. React 👍/👎 to tell us if it helped."* (folds in the P-16 CTA).
- Do **not** subscribe to every subsequent message in the thread (that's chime-in, which D-7 rules out). Only
  re-engage if explicitly mentioned.
- If retrieval is below threshold, still post a short "I couldn't find this in the docs — a human will follow
  up" rather than staying silent, so the standing promise holds.

---

## 5. Rate limiting → **P-14**

**Best practice.** Two distinct concerns, don't conflate them:
1. **Discord's own API limits** (global 50 req/s, per-route buckets via `X-RateLimit-*` headers) — the client
   library queues/handles these; you don't hand-roll it.
   ([Discord rate limits](https://docs.discord.com/developers/topics/rate-limits)).
2. **Your product-level per-user throttle** — a small **in-memory token bucket keyed by user** to stop one
   person from monopolizing the bot / burning LLM spend. Since we run **exactly one instance**
   (`DISCORD_INTEGRATION.md`), an in-process dictionary is correct and needs no Redis. On exceed, send a
   **friendly, human refusal** ("give me a breather — try again in a couple minutes"), never a raw error and
   never silence.

**Maps to P-14.**
- Bucket key = the **hashed** user id (D-8) so rate-limiting shares the same identity shape as the interaction
  log and never stores raw ids. Default 5 questions / 10 min (per contract).
- Single-instance assumption is load-bearing: document that horizontal scaling would break the bucket (ties
  back to "exactly one gateway session"). If we ever add a replica, the bucket must move to shared storage.
- Check the limit **before** the expensive path (retrieval/LLM), and **before** deferring on `/ask` where
  possible — a rate-limited `/ask` can reply ephemerally with the refusal so it doesn't clutter the channel.
- Spec the bucket as **pure logic** (refill math, exceed boundary) per D-10 — it's exactly the kind of unit the
  plan wants covered from day one.

---

## 6. Feedback capture: reactions vs. components → **P-16**

**Best practice (industry direction: buttons).** Message-component **buttons** are more reliable than
emoji reactions for structured feedback: reactions are added one-at-a-time (rate-limit prone), custom emoji can
**fail silently** if the bot can't access them, and reaction events are noisier to attribute. Buttons carry an
**interaction** (identity + callback) directly, are unambiguous ("👍 Helpful / 👎 Not helpful" labels), and
must be answered within 3 s. This is why kapa and most modern support bots use **thumbs-up/down buttons**, not
reactions.
Sources: [Discord Buttons FAQ](https://support-dev.discord.com/hc/en-us/articles/6381892888087-Buttons-FAQ)
("Reactions have to be added one at a time … buttons solve this"),
[discord.js component interactions](https://discordjs.guide/interactive-components/interactions.html),
[kapa thumbs up/down feedback](https://docs.kapa.ai/data-sources/discord).

**Reality for us.** Our contract + P-16 currently specify **👍/👎 reactions** the bot pre-adds, recorded via a
reaction-add handler. Reactions are workable *because we use only the two default emoji* (the silent-fail case
is custom emoji, which we avoid) and they're visually lightweight. The trade-off:

| | Reactions (current P-16) | Buttons (industry default) |
|---|---|---|
| Emoji access risk | none (default 👍/👎) | n/a |
| Attribution | reaction-add gateway event, needs de-dupe/toggle handling | interaction gives the user directly |
| Bot setup cost | pre-add 2 reactions per answer (2 API calls) | one components row, zero extra calls |
| Removes/undo | user can un-react (must handle remove too) | one click, immutable unless you allow re-vote |
| Recommendation | acceptable v1 | stronger; low extra cost |

**Maps to P-16.**
- If staying with reactions (as written): only accept **the two default emoji**, ignore all others, ignore the
  bot's own pre-added reactions, and handle **reaction-remove** (a user toggling off) so the row reflects the
  final verdict. De-dupe per (message, user).
- Recommended upgrade: attach a **buttons row** ("👍 Helpful" / "👎 Not helpful") to the answer instead — one
  components row, no pre-add calls, and the button interaction hands you the (hashable) user id cleanly. This
  is a small change to the same `answer_message_id` migration (P-04) — the column stores the verdict either
  way. See "Suggested edits" below.
- Either mechanism: store **hashed** user id + verdict on the interaction row (D-8). Feedback is optional, so a
  missing verdict is normal, not an error.

---

## 7. Long answers: splitting vs. threads → **P-15**

**Best practice.** Discord hard-caps messages at **2000 chars**. Split on **natural boundaries** (paragraph →
newline → space), never mid-word, and **never split inside a markdown construct** — a code fence or link cut
across a boundary breaks rendering on both halves. Keep the chunk count small; walls of bot text read as spam.
Sources: [Discord message-splitter guidance](https://github.com/Half-Shot/matrix-appservice-discord/issues/174),
[omega #556](https://github.com/thomasdavis/omega/issues/556) ("split at natural boundaries … preserve
markdown/code blocks").

**Maps to P-15.**
- Split on **paragraph boundaries first** (blank line), fall back to single newline, then space — max **3
  messages** (per contract). If it still overflows 3, truncate the last with a "…(trimmed)" marker rather than
  spilling to a 4th.
- **Never cut inside a fenced code block.** If a paragraph split would land inside a ` ``` ` fence, move the
  boundary out; if a single code block alone exceeds 2000 chars, that's a doc-shaped edge case — truncate
  inside the fence and close it. Add a spec fixture with a code block spanning the boundary.
- **Sources go on the last message only** (per contract) so citations aren't repeated and are easy to find.
- For the **forum** surface, prefer keeping the whole answer **in-thread** as sequential messages rather than
  spawning a sub-thread — the thread already is the container.

---

## 8. Citations / sources formatting → **P-15 / answer assembly**

**Best practice.** Wrap every source URL in **angle brackets** `<https://…>` to **suppress the embed preview**
— otherwise Discord unfurls each link into a big card and 3 sources becomes a screenful. Angle brackets must
**fully** wrap the URL (one stray char re-enables the embed). Masked links can suppress too:
`[label](<url>)`. Keep sources as a short trailing line, not an embed, so they stay copy-pasteable.
Sources: [Discord — suppressing link previews with angle brackets](https://support.discord.com/hc/en-us/community/posts/360042180911-Link-Preview-Remove-or-hide-preview-on-a-per-image-basis),
[kapa cites sources per answer](https://www.kapa.ai/).

**Maps to P-15 / answer format.**
- Our contract already says `Sources: <url> · <url>` with `<…>` — this research confirms it's the right call.
  Make the `<…>` wrapping a **hard invariant** in the assembler and spec it (a URL that slips out of brackets
  spams embeds).
- Point URLs at the **human doc pages** (`.md` stripped), per contract — the reader clicks through to the real
  page, not the ingestion mirror.
- **Consider a real embed** (title + description + source field) *only if* it reads better than plain text —
  but embeds count against different limits and can't be split like text; plain text + suppressed links is the
  simpler, more robust default. Recommend staying with plain text for v1.
- Note the known Discord quirk: a **followup after a defer can ignore `SUPPRESS_EMBEDS`**
  ([discord-api-docs #4784](https://github.com/discord/discord-api-docs/issues/4784)) — so rely on
  **angle-bracket suppression in the content**, not on the message-level suppress flag, in the `/ask` (P-11)
  path.

---

## 9. Resilience → **Resilience task (M3.8)**

**Best practice.** Gateway/event handlers must **never throw** — an unhandled exception in an event handler
can tear down or destabilize the gateway loop. Wrap each request in a **per-request timeout** and a
**catch-all** that logs and posts a short apology, so the user gets *something* instead of silence. Treat "the
model is slow/broken" as an expected state with a friendly message, the way kapa returns a graceful "I'm not
sure" rather than erroring ([kapa uncertainty behavior](https://docs.kapa.ai/overview/faq)).

**Maps to the resilience task.**
- Wrap answer generation per question in a **60 s timeout** (contract). On timeout → apology, not a hang.
- **Every** entry point (`/ask` P-11, mention P-12, ask-channel, forum P-13, reaction/button P-16) gets a
  top-level `try/catch` that (a) logs with `[LoggerMessage]` in the `*Logging.cs` partial, (b) posts the short
  apology, (c) **swallows** so the handler returns normally. The gateway handler contract is: log, apologize,
  never rethrow.
- For `/ask` specifically: because you've already deferred, the apology must **edit the deferral / send a
  followup** — landing a message inside the 15-min window — so the "thinking…" placeholder resolves.
- "Done when" is already right: a bad API key produces the apology, not silence. Add the same forced-failure
  check for the mention and forum paths.
- On gateway disconnect, the library reconnects/resumes; missed-while-down messages aren't replayed
  (contract) — acceptable, don't build a replay buffer.

---

## 10. Privacy / GDPR notice → **D-8, P-23 (M5), touches every surface**

**Best practice.** An EU-operated community bot processes personal data the moment it receives a user id or
message content — GDPR applies regardless of scale. Publish a plain-language notice: **what** is processed,
**why** (lawful basis), **who** the subprocessor is (the LLM), **how long** it's kept, and **how to delete**.
Sources: [Privacy policy for Discord bots (2026)](https://www.legalforge.app/blog/privacy-policy-for-discord-bot),
[EU community bot GDPR guidance](https://sota.io/blog/discord-eu-alternative-gdpr-cloud-act-developer-community-messaging-2026);
Answer Overflow models **consent-before-indexing** ([AO indexing/consent](https://docs.answeroverflow.com/channel-settings/indexing)).

**Maps to us (already largely decided — D-8 as amended by D-13).**
- The pinned notice (P-23) should name Prompter, list what it processes (question text, sent to the
  **Anthropic** subprocessor and not retained), and state that it stores **no message content and nothing that
  identifies you** (D-13) — so there is no per-user data to delete.
- Because the repo is public (D-12), the notice can **link to the source** proving no content or identity is
  stored — a transparency asset, per D-8/D-13.
- Reinforce in the `#ask` channel pin (§3) since that's the busiest AI surface.
- Reminder already in the contract: Discord message content must **not** be used to train models — our
  Anthropic API usage doesn't train by default; keep it documented.

---

## 11. Anti-patterns to avoid

- **Unsolicited answers in general channels.** Every surviving vendor converged on mention/dedicated-channel;
  chime-in erodes trust faster than it helps. **Already ruled out by D-7** — do not add it in v1.
  ([RESEARCH.md](RESEARCH.md) market convergence.)
- **Silence on failure.** Worse than an error message. The resilience task exists to prevent it.
- **Embed-spamming citations.** Un-bracketed source links unfurl into a wall of cards — always `<…>`-wrap (§8).
- **Re-answering every message in a thread.** Answer the starter once; only re-engage on mention (§4).
- **Walls of text.** Respect 3-message cap; truncate gracefully rather than dumping (§7).
- **Building your own spinner** instead of Discord's native "thinking…" defer affordance (§1).
- **Answering other bots / yourself.** Guard `IsBot` and self-mention to avoid loops (§2).
- **Custom emoji for feedback.** They fail silently if inaccessible — default 👍/👎 or buttons only (§6).
- **Two instances running.** Double gateway sessions = double answers *and* a broken in-memory rate bucket
  (§5, contract).
- **Ephemeral confusion.** You can't flip public↔ephemeral after ack — decide before deferring (§1).

---

## Suggested edits to `DISCORD_INTEGRATION.md` (proposals — not applied here)

1. **Feedback mechanism (biggest one):** consider switching P-16 from pre-added **reactions** to a
   **buttons row** ("👍 Helpful / 👎 Not helpful"). Buttons are the industry default (kapa, most support bots),
   avoid pre-add API calls and reaction-remove/toggle handling, and hand you the user id via the interaction.
   The `answer_message_id` migration (P-04) is unchanged; only the capture path differs. If reactions stay,
   add "handle reaction-remove / de-dupe per (message,user); default emoji only" to the Feedback row.
2. **Forum re-engagement rule:** state explicitly that the bot answers the **starter message once** and does
   **not** respond to subsequent thread messages unless mentioned (currently implied; make it a rule so it
   can't drift into chime-in).
3. **Code-fence-safe splitting:** in the Long-answers row, add "never split inside a fenced code block; if a
   single code block exceeds 2000 chars, truncate inside the fence and close it."
4. **Embed suppression caveat:** note the deferred-followup `SUPPRESS_EMBEDS` quirk
   ([#4784](https://github.com/discord/discord-api-docs/issues/4784)) and that we rely on **angle-bracket
   suppression in content**, not the message suppress flag.
5. **Rate-limit refusal placement:** specify that a rate-limited `/ask` may reply **ephemerally** (so the
   refusal doesn't clutter the channel), while mention/ask-channel refusals stay in-channel.
6. **Forum auto-reply as config toggle:** mirror kapa's on/off switch — a `HelpForumAutoReply` bool under
   `DiscordOptions` — so the forum behavior can be disabled without a code change.
7. **Empty-mention handling:** define what happens on a bare `@Prompter` with no question (gentle nudge vs.
   ignore) so P-12 has a spec'd answer.
8. **Ephemeral decision for `/ask`:** record that `/ask` defers **without** the ephemeral flag (public), since
   the flag is immutable after ack — protects an implementer from a silent public↔ephemeral mistake.
