---
title: Set up the Discord app
description: Register Prompter's Discord application, enable the Message Content intent, invite it with minimal permissions, and configure the channel IDs.
---

This runbook registers Prompter's Discord application, invites the bot into a server, and points it at the
right channels. Prompter **dials out** to Discord over a gateway connection, so no inbound ports or public IP
are needed - only a bot token and the channel IDs. The behavior these steps enable is specified in the
[Discord integration contract](https://github.com/Cratis/Prompter/blob/main/Planning/DISCORD_INTEGRATION.md),
which is the authoritative runbook; this page is its published summary.

## Before you begin

You need a Discord account that can create an application, admin rights on the target server, and Prompter
itself running with the token from step 2 (see [Run Prompter locally](running-locally.md) or
[Deploy Prompter](deploying.md)). For the real Cratis server, steps 1-4 are one-time team actions (backlog
P-17a); do them on a private test server first and confirm every surface before the community server sees the
bot.

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
Cratis server (ID `1182595891576717413`), or to your test server while iterating.

## 4. Configure the channels

Turn on **Developer Mode** in Discord (User Settings, under Advanced), then right-click a channel and
**Copy ID**. Set the IDs for the surfaces you want Prompter to watch:

| Surface | Setting |
|---|---|
| Help forum (auto-reply to new posts) | `Cratis__Prompter__Discord__HelpForumChannelId` |
| Ask channel (every message is a question) | `Cratis__Prompter__Discord__AskChannelId` |

Mentions and `/ask` work everywhere the bot can read and need no channel configuration. The full settings
table is in [Configuration](../reference/configuration.md).

## 5. Pin the privacy notice

Pin the privacy notice (backlog P-23) in the ask channel so members can see what Prompter processes and which
subprocessors it uses. See [Privacy](../concepts/privacy.md) for the content it should carry.

## Gateway intents and slash commands

The bot requests four gateway intents on startup - `Guilds`, `GuildMessages`, `MessageContent`, and
`GuildMessageReactions` - so the privileged **Message Content** intent enabled in step 2 must stay switched on
to match. NetCord registers the `/ask` slash command on startup; global registration can take up to an hour to
propagate the first time, while guild-scoped registration is instant - prefer guild-scoped while iterating on a
test server.

For the full behavior contract - every surface, the answer format, rate limiting, and refusals - see the
[Discord integration contract](https://github.com/Cratis/Prompter/blob/main/Planning/DISCORD_INTEGRATION.md).
