---
title: Set up the Discord app
description: Register Prompter's Discord application, enable the Message Content intent, invite it with minimal permissions, and configure the channel IDs.
---

This runbook registers Prompter's Discord application, invites the bot into a server, and points it at the
right channels. Prompter **dials out** to Discord over a gateway connection, so no inbound ports or public IP
are needed. A token and channel settings alone are not enough to start an answer-enabled host: the guild
restriction below must also be configured. For the supported ways to ask questions and read the answers,
see [Use Prompter on Discord](using-prompter.md).

## Before you begin

You need a Discord account that can create an application and admin rights on the Cratis server. Prompter
does not need to be running yet: steps 1-4 create the application and token, invite the bot, and configure
its channels. Pin the privacy notice in step 5 before inviting community questions.

Before starting an answer-enabled host, explicitly set the scalar
`Cratis__Prompter__Protection__ConfiguredAllowedGuilds=1182595891576717413`. Only the Cratis guild is
allowed. This is a current product restriction, including for local running and Development; it does not
permit a separate private test guild. The bot and the ask, index, and eval commands share this startup
check; migrate is separate. If answering is disabled, the guild list may be empty, but it still cannot
name a different guild, and admission remains disabled.

Steps 1-4 are one-time Cratis team actions (backlog P-17a), not a procedure for setting up another guild.

## 1. Create the application

Open the [Discord Developer Portal](https://discord.com/developers/applications), choose **New Application**,
name it **Prompter**, and give it the Cratis logo. On the **Bot** tab, disable **Public Bot** - only the
Cratis team installs it.

## 2. Enable the Message Content intent

On the **Bot** tab, enable the **Message Content Intent**. This privileged intent is **required** - without it
Prompter cannot read mentions, ask-channel messages, or forum posts, and every surface goes silent. It needs
no verification while the bot is on fewer than 100 servers. Copy the bot **token** and set it as
`Cratis__Prompter__Discord__Token` (keep it in environment variables or encrypted config, never in git).

## 3. Invite the bot with minimal permissions

On the **Installation** tab, choose **Guild install** only, with the scopes `bot` and `applications.commands`.
Grant only the permissions Prompter actually uses:

- View Channels
- Send Messages
- Send Messages in Threads
- Create Public Threads
- Embed Links
- Add Reactions
- Read Message History

Nothing more - no Administrator, no Manage permissions. Use the generated install URL to add the bot to the
Cratis server (ID `1182595891576717413`).

## 4. Configure the channels

Turn on **Developer Mode** in Discord (User Settings, under Advanced), then right-click a channel and
**Copy ID**. Set the IDs for the surfaces you want Prompter to watch:

| Surface | Setting |
|---|---|
| Help forum (auto-reply to new posts) | `Cratis__Prompter__Discord__HelpForumChannelId` |
| Channels where answers start a thread (optional) | `Cratis__Prompter__Discord__AnswerInThreadChannelIds__0`, `__1`, … |
| Forums whose unanswered posts get a delayed answer (optional; off until configured) | `Cratis__Prompter__Discord__DelayedForums__0__ChannelId` and `__DelayMinutes` (default `30`) |
| Maintainer role for the Ask a human button (optional; make the role mentionable). It also exempts holders from the per-user question limits and allows them to use `/answering pause`, `resume` and `status` | `Cratis__Prompter__Discord__MaintainerRoleId` |
| Maintainer channel (also enables the This should be documented and Request this buttons; see the configuration reference) | `Cratis__Prompter__Discord__MaintainerChannelId` |
| Ask channel (every message is a question) | `Cratis__Prompter__Discord__AskChannelId` |

`/answering` is registered with a default permission of Manage Server, which only controls who sees it. Prompter itself checks the guild and the maintainer role. To show the command to maintainers who lack Manage Server, add an override for the maintainer role in Server Settings → Integrations → Prompter → `/answering`.

Delayed forum answers need no extra permission beyond the list above, but the bot must have View Channels and Read Message History
on each opted-in forum so it can list active posts and check whether a human replied. Without a configured forum the feature stays off, and
Prompter answers a post only once, after its delay; posts more than 24 hours past their delay are skipped.

Within the allowed Cratis guild, mentions and `/ask` work where the bot can read and need no channel
configuration. The full settings table is in [Configuration](../reference/configuration.md).

## 5. Pin the privacy notice

Before inviting community questions, pin the privacy notice (backlog P-23) in the ask channel so members can
see what Prompter processes and which subprocessors it uses. See [Privacy](../concepts/privacy.md) for the
content it should carry.

With the token, channel settings, and required guild configuration in place, start Prompter using either
[Run Prompter locally](running-locally.md) or [Deploy Prompter](deploying.md). See
[Use Prompter on Discord](using-prompter.md) for the supported question surfaces and refusals. Allow for
the slash-command registration delay described below when checking a new application.

## Gateway intents and slash commands

The bot requests four gateway intents on startup - `Guilds`, `GuildMessages`, `MessageContent`, and
`GuildMessageReactions` - so the privileged **Message Content** intent enabled in step 2 must stay switched on
to match. NetCord registers the `/ask` slash command on startup; global registration can take up to an hour to
propagate the first time, while guild-scoped registration is instant.

For the supported surfaces, answer format, rate limiting, and refusals, see
[Use Prompter on Discord](using-prompter.md). [Grounded answers](../concepts/grounded-answers.md) explains
how citations and refusals keep answers tied to the documentation.
