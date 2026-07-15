# Discord integration — behavior contract & app setup

The specification for how Prompter behaves on Discord, and the runbook for registering the app. The
implementation order lives in [`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md) M3; interaction-model
decisions are D-7 in [`DECISIONS.md`](DECISIONS.md).

## Functionality set (v1 behavior contract)

| Surface | Trigger | Behavior |
|---|---|---|
| **@mention** | `@Prompter <question>` in any channel the bot can read (both `<@id>` and `<@!id>` forms; role mentions ignored) | Replies in-channel as a reply to the asking message, grounded answer + source links, or honest refusal |
| **/ask** | Slash command anywhere | Defers immediately ("thinking…"), follows up with the answer; response visible to the channel |
| **Ask channel** | Any plain message in the configured `#ask-prompter` channel | Treated as a question — no mention needed (the Prisma pattern) |
| **Help forum auto-reply** | New thread created in the configured help forum channel | Posts the first reply: cited answer + "a human will follow up" line |
| **Feedback** | 👍/👎 reactions the bot pre-adds on its answers | Verdict recorded on the interaction row (hashed user, D-8) |
| **Rate limit** | >5 questions / 10 min per user | Friendly "give me a breather" reply, no answer |
| **Long answers** | Answer > 2000 chars | Split on paragraph boundaries across up to 3 messages, sources on the last |
| **Failure** | Model/API error or >60 s timeout | Short apology message — never silence, handler never throws |

**Never in v1:** unprompted interjections in channels not listed above (D-7 — every surviving vendor converged
on mention/dedicated-channel; chime-in is a post-v1 experiment), DMs (not needed, keeps GDPR surface small),
and answering other bots.

### Answer format

Answer text (Discord markdown, code blocks allowed) + blank line + `Sources: <url> · <url>` with `<…>` to
suppress embeds, URLs pointing at the human pages (`.md` stripped). Refusals carry no sources and invite the
human community + suggest reporting a docs gap.

## Discord application setup (runbook — team action, P-17a/P-26)

1. <https://discord.com/developers/applications> → **New Application** → name **Prompter**, Cratis logo.
2. **Bot** tab: disable *Public Bot* (only we install it); enable **Message Content Intent** (privileged —
   required for mention/ask-channel/forum reading; no verification needed under 100 servers). Copy the
   **token** → `Cratis__Prompter__Discord__Token`.
3. **Installation** tab: Guild install only; scopes `bot` + `applications.commands`; bot permissions:
   **View Channels, Send Messages, Send Messages in Threads, Create Public Threads, Embed Links,
   Add Reactions, Read Message History**. Nothing more (no Administrator, no Manage anything).
4. Install to the Cratis server (ID `1182595891576717413`) via the generated URL.
5. Create/choose channels and capture IDs (Developer Mode → right-click → Copy ID):
   `#ask-prompter` text channel → `Discord:AskChannelId`; the help **forum** channel → `Discord:HelpForumChannelId`.
6. Pin the privacy notice (P-23) in `#ask-prompter`.

Gateway **intents the code requests** must stay in sync: `Guilds` (thread events), `GuildMessages`,
`MessageContent`, `GuildMessageReactions`. Slash commands are registered by NetCord's application-command
service on startup (global registration can take up to an hour to propagate the first time; guild-scoped
registration is instant — prefer guild-scoped while iterating on a test server).

## Test server protocol

Create a private test server; install the bot there first. Every M3 task's "done when" runs there before the
real server sees anything. Keep a `#playground` channel and a test forum channel mirroring the real setup.

## Notes for implementers

- NetCord 1.0.0-beta.11 is **pinned** in `Directory.Packages.props` — API surface may move between betas;
  verify against <https://netcord.dev> when touching gateway/interaction code, and record any rename in this
  file. Fallback decision if beta churn becomes costly: Discord.Net 3.20.x (D-3).
- The 3-second interaction ack window is a hard Discord rule — `/ask` must defer (M3.1) before answering.
- Discord Developer Policy: message content obtained from the API must not be used to train ML models — our
  Anthropic API usage does not train (documented in D-8/privacy notice); keep it that way.
