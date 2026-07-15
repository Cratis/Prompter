# Discord integration — behavior contract & app setup

The specification for how Prompter behaves on Discord, and the runbook for registering the app. The
implementation order lives in [`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md) M3; interaction-model
decisions are D-7 in [`DECISIONS.md`](DECISIONS.md).

## Functionality set (v1 behavior contract)

| Surface | Trigger | Behavior |
|---|---|---|
| **@mention** | `@Prompter <question>` in any channel the bot can read — both the `<@id>` and nickname `<@!id>` forms, matched against the bot's own id | Replies in-channel as a reply-reference to the asking message: grounded answer + source links, or honest refusal. Role mentions (`<@&id>`), `@everyone`/`@here`, other bots, and the bot's own messages never trigger it; a bare mention with no question after the mention is stripped is ignored |
| **/ask** | Slash command anywhere | Defers immediately (Discord's native "thinking…"), then delivers the answer as a **public** followup carrying the 👍/👎 feedback buttons. The rate limit is checked *before* deferring, so an over-limit ask refuses ephemerally instead |
| **Ask channel** | Any plain message in the configured ask channel (`Discord:AskChannelId`) | Treated as a question — no mention needed (the Prisma pattern). Same bot/self guards and rate limit as a mention |
| **Help forum auto-reply** | New thread created in the configured help forum channel (`Discord:HelpForumChannelId`) | Fetches the thread's starter message and posts one first reply: cited answer + a standing "a human will follow up" note. Answers the starter **once**; never re-engages later thread messages (D-7). Enabled by setting the channel id — leave it unset to disable |
| **Feedback** | 👍/👎 **buttons** attached to every answer (message components, not reactions) | A button click (component interaction) carries the clicking user and the answer's interaction id in its custom id `feedback:<verdict>:<interaction-id>` (`verdict` is `up`/`down`); the handler acknowledges ephemerally and writes the verdict to the interaction row (hashed user, D-8) |
| **Rate limit** | >5 questions / 10 min per user (`Discord:RateLimit`, per hashed id) | Friendly "give me a breather" refusal, no answer — **ephemeral** on `/ask` so it does not clutter the channel, in-channel/in-thread on the other surfaces |
| **Long answers** | Answer > 2000 chars | On the mention/ask-channel path, split on paragraph/code-fence boundaries across up to 3 messages, sources on the last; `/ask` and the forum reply as a single message truncated with an ellipsis |
| **Failure** | Model/API error or timeout (`Discord:AnswerTimeoutSeconds`, default 60 s) | Short apology message — never silence, handler never throws |

**Never in v1:** unprompted interjections in channels not listed above (D-7 — every surviving vendor converged
on mention/dedicated-channel; chime-in is a post-v1 experiment), DMs (not needed, keeps GDPR surface small),
and answering other bots.

### Answer format

Answer text (Discord markdown, code blocks allowed) + blank line + `Sources: <url> · <url>` with `<…>` to
suppress embeds, URLs pointing at the human pages (`.md` stripped). Refusals carry no sources and invite the
human community + suggest reporting a docs gap. The angle-bracket wrapping is a **hard invariant** in the
assembler — a URL that slips out of its brackets unfurls into an embed card and three sources become a
screenful. On the `/ask` path this matters doubly: a followup after a defer can ignore the message-level
`SUPPRESS_EMBEDS` flag ([discord-api-docs #4784](https://github.com/discord/discord-api-docs/issues/4784)), so
suppression rides entirely on the angle brackets in the content, never on the flag.

Long answers are split on blank-line paragraph boundaries, packed greedily into at most three messages with the
sources line on the last. Splitting **never cuts inside a fenced code block** — a code block is kept whole as
its own paragraph; a single block that alone exceeds 2000 chars is hard-split into self-contained fenced
messages that each close with ` ``` ` and reopen with the same language hint. If the content still overflows
three messages the last chunk is truncated with an ellipsis so the sources always make it through.

### Feedback

Every answer carries an action row of two buttons — **👍 Helpful** (green) and **👎 Not helpful** (grey). The
emoji rides in the button label text, so no custom emoji is referenced and the button cannot fail to render
(the silent-fail case that rules out custom-emoji reactions). Buttons are used rather than reactions because a
click is a component interaction that hands us the (hashable) user id and the target interaction directly, costs
no pre-add API calls, and needs no reaction-remove/toggle handling. The button rides on the single answer
message (or on the **last** chunk of a split answer). Clicking acknowledges ephemerally ("Thanks for the
feedback!") within the 3-second interaction window, then writes the verdict best-effort — feedback is optional,
so a missing verdict is normal, not an error.

## Discord application setup (runbook — team action, P-17a/P-26)

1. <https://discord.com/developers/applications> → **New Application** → name **Prompter**, Cratis logo.
2. **Bot** tab: disable *Public Bot* (only we install it); enable **Message Content Intent** (privileged —
   required for mention/ask-channel/forum reading; no verification needed under 100 servers). Copy the
   **token** → `Cratis__Prompter__Discord__Token`.
3. **Installation** tab: Guild install only; scopes `bot` + `applications.commands`; bot permissions:
   **View Channels, Send Messages, Send Messages in Threads, Create Public Threads, Embed Links,
   Read Message History**. Nothing more (no Administrator, no Manage anything, and — since feedback moved to
   buttons — no Add Reactions).
4. Install to the Cratis server (ID `1182595891576717413`) via the generated URL.
5. Create/choose channels and capture IDs (Developer Mode → right-click → Copy ID):
   `#ask-prompter` text channel → `Discord:AskChannelId`; the help **forum** channel → `Discord:HelpForumChannelId`.
6. Pin the privacy notice (P-23) in `#ask-prompter`.

Gateway **intents the code requests** must stay in sync: `Guilds` (thread-create events), `GuildMessages`, and
`MessageContent`. Feedback button clicks arrive as component interactions and need no reaction intent. Slash
commands and component interactions are registered by NetCord's application-command / component-interaction
services on startup (global registration can take up to an hour to propagate the first time; guild-scoped
registration is instant — prefer guild-scoped while iterating on a test server).

## Test server protocol

Create a private test server; install the bot there first. Every M3 task's "done when" runs there before the
real server sees anything. Keep a `#playground` channel and a test forum channel mirroring the real setup.

## How the connection works (mechanics)

The bot **dials out** to Discord: on startup it opens a persistent WebSocket (the "gateway") authenticated
with the bot token, and Discord streams events (message created, thread created, button click) down that
connection; replies go out through Discord's REST API. Consequences worth knowing when wiring infrastructure:

- **No inbound port is needed for Discord at all** — the only inbound routes Prompter exposes are its own
  `GET /healthz` and `POST /reindex`; only those need an ingress route ([`DEPLOYMENT.md`](DEPLOYMENT.md)).
- **Exactly one instance** should run — two replicas means two gateway sessions answering everything twice,
  and the in-memory rate bucket (below) would split across them.
- If the connection drops, the library reconnects and resumes; missed-while-down messages are not replayed
  (acceptable: an unanswered mention is retryable by the human).

The per-user rate limit is an in-memory token bucket keyed by the **hashed** user id (D-8), default 5 questions
/ 10 min (`Discord:RateLimit` — `MaxQuestions` / `WindowMinutes`), checked before the expensive retrieval/LLM
path on every surface. `WindowMinutes` must be positive and `MaxQuestions` non-negative; the configuration is
validated at startup (`ValidateOnStart`) so a misconfiguration fails fast rather than silently disabling the
limit. The in-memory bucket is correct **only** because we run exactly one instance — horizontal scaling would
need shared storage.

Answer generation on every surface runs under a per-question timeout (`Discord:AnswerTimeoutSeconds`, default
60 s) inside a top-level `try/catch`: on timeout or any failure the handler logs and posts the short apology
(`Discord:ErrorReply`) — for `/ask` as a followup that resolves the "thinking…" placeholder — and returns
without rethrowing, so a slow or broken model never leaves a user in silence and never tears down the gateway
loop. The friendly refusal and apology text are configurable (`Discord:RateLimitedReply`, `Discord:ErrorReply`).

## Notes for implementers

- NetCord 1.0.0-beta.11 is **pinned** in `Directory.Packages.props` — API surface may move between betas;
  verify against <https://netcord.dev> when touching gateway/interaction code, and record any rename in this
  file. Fallback decision if beta churn becomes costly: Discord.Net 3.20.x (D-3).
- The 3-second interaction ack window is a hard Discord rule. `/ask` defers (M3.1) — **without** the ephemeral
  flag, so its answer is public (the flag is locked at ack time and cannot be flipped afterwards). Feedback
  button clicks are also interactions: their handler acks ephemerally first (well inside 3 s) and only then
  writes the verdict.
- The bot's own user id (for self-mention detection) is read from `GatewayClient.Id`, populated from the READY
  payload before any message-create event, so it needs no per-message or startup REST lookup.
- The forum handler only answers genuinely new threads (`NewlyCreated`) — Discord also raises `THREAD_CREATE`
  when the bot merely gains visibility of an existing thread, which must be ignored.
- Discord Developer Policy: message content obtained from the API must not be used to train ML models — our
  Anthropic API usage does not train (documented in D-8/privacy notice); keep it that way.
