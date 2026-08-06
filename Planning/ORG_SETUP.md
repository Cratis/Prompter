# Org setup — the accounts, apps and secrets only an owner can create

Everything Prompter needs that belongs to **Cratis rather than to a person**: the two model-provider keys,
the Discord application, and the org-level secrets. Written for whoever owns the org accounts — in practice
Einari — to work through in one sitting without reading anything else in this repository.

**About 30 minutes for the part that unblocks testing** (steps 1–3). The rest is needed later, at the stages
named against it, and can wait.

## Why these are yours specifically

Not gatekeeping — durability. A model-provider key on a personal card, or a Discord application owned by one
person's account, quietly becomes a single point of failure: the day that account is unavailable, the bot is
unrecoverable and its token cannot be rotated by anyone else. Two rules carry the whole document:

- **Keys live on the org account**, not a personal one.
- **The Discord application lives in a Discord Team**, not on an individual — a Team can have several owners
  and survives any one of them leaving.

Everything else here is ordinary admin.

---

## 1 · Voyage API key — *unblocks indexing*

Prompter embeds the documentation with Voyage. The free tier covers this corpus many times over, so this
costs nothing.

- [ ] Sign in at <https://dash.voyageai.com> **with the Cratis account**, not a personal one
- [ ] **API keys** → create a key, name it `prompter`
- [ ] Copy it somewhere safe (see [handover](#5--hand-over-to-whoever-runs-stage-a) below)

## 2 · Anthropic API key — *unblocks answering*

Prompter generates answers with Claude. At community volume this is a few cents a month, but it is worth
capping anyway — a misconfiguration should cost pennies, not a surprise.

- [ ] Sign in at <https://console.anthropic.com> with the **Cratis organization**
- [ ] If your plan offers **Workspaces**, create one called `Prompter` and set a modest monthly limit on it.
      A workspace-scoped key with a cap means nothing Prompter does can drain the org's budget
- [ ] **API keys** → create a key, name it `prompter`, scoped to that workspace if you made one
- [ ] Copy it somewhere safe

## 3 · The Discord application — *unblocks everything on Discord*

This is the step with the durability trap in it, so do the Team part first.

### 3a · Create a Team to own it

- [ ] <https://discord.com/developers/teams> → **New Team**, name it `Cratis`
- [ ] Add whoever else should be able to administer the bot (Sindre at minimum)

### 3b · Create the application inside the Team

- [ ] <https://discord.com/developers/applications> → **New Application** → name **Prompter**, and set its
      **Team** to `Cratis` rather than "Personal"
- [ ] Give it the Cratis logo (it appears next to every answer)

### 3c · Bot settings

- [ ] **Bot** tab → turn **Public Bot** *off* (only we install it)
- [ ] Enable the **Message Content Intent**. It is a privileged intent, but needs no review under 100
      servers — without it Prompter cannot read the questions it is mentioned in
- [ ] **Reset Token** → copy the token. This is the most sensitive value here: it *is* the bot

### 3d · Permissions — exactly these, nothing more

- [ ] **Installation** tab → **Guild install** only
- [ ] Scopes: `bot` and `applications.commands`
- [ ] Bot permissions: **View Channels · Send Messages · Send Messages in Threads · Create Public Threads ·
      Embed Links · Read Message History**

No Administrator, no Manage anything. If the generated invite URL asks for more than the six above, something
was ticked by accident — a documentation bot that can manage a server is a bad trade for nobody's benefit.

### 3e · A private test server first

- [ ] Create a **private test server** (not the Cratis one) and install the app there with the generated URL
- [ ] Create a text channel and a **forum** channel in it
- [ ] Turn on **Developer Mode** (User Settings → Advanced), then right-click each → **Copy ID**

The real Cratis server comes later, at playbook step B8, once the bot has been proven on the test server.

## 4 · Later — org secrets and access

Not needed for testing. Each is listed against the stage that needs it, so they can be done when you get
there rather than up front.

| What | For | Stage |
|---|---|---|
| `PULUMI_CONFIG_PASSPHRASE` repo secret | Encrypts the deployment stack's secrets. Invent one, store it in the password manager — losing it means re-encrypting every stack secret | B1 |
| `UPCLOUD_TOKEN` repo secret | Lets the deploy reach the cluster. The same credential Studio's deploy uses | B1 |
| Confirm the `[self-hosted, linux, cratis]` runner is available to `Cratis/Prompter` | The deploy job **queues silently forever** if it is not — it does not fail | B1 |
| DNS record for the ingress host (e.g. `prompter.cratis.studio`) | Certificate issuance cannot complete without it | B3 |
| Fine-grained PAT, resource owner **Cratis**, `Issues: Read and write` | Lets Prompter file issues from Discord | C2 |
| Repository webhooks → `/github/webhook` | Lets Prompter answer newly-opened issues | C3 |

On the PAT: it acts as **you** — issues will show your name as the author, it expires on the schedule you
pick, and it shares your personal rate limit. A GitHub App gives Prompter its own identity and no expiry for
about an hour of setup. Start with the PAT; the code sends either as a bearer token, so switching later is
changing one secret.

## 5 · Hand over to whoever runs Stage A

Three values plus two channel ids are all Stage A needs:

| Value | From |
|---|---|
| Voyage API key | step 1 |
| Anthropic API key | step 2 |
| Discord bot token | step 3c |
| Test text channel id · test forum channel id | step 3e |

- [ ] Share them through the **password manager**, not Discord, email or chat
- [ ] If one does get pasted somewhere it should not be, rotate it rather than hoping — all three are
      one-click regenerable, and a leaked bot token lets anyone speak as Prompter

None of these are ever committed. They live in the runner's environment during testing, and in
passphrase-encrypted Pulumi config in production.

## Then what

Stage A runs on a laptop and needs nothing further from you:
[the go-live playbook](GO_LIVE_PLAYBOOK.md), tracked as
[issue #6](https://github.com/Cratis/Prompter/issues/6). Your next involvement is the row marked B1 above.
