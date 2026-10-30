<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# GitHub issue consent and App authentication

Filing is disabled by default. This slice does **not** activate a deployment or wire the shared host automatically.

## Reporter consent

`/issue` composes the final owner, repository, title, body (including public Discord provenance), and labels **before** showing the ephemeral preview. Every posting field is immutable. Confirmation posts that snapshot, not a reconstruction from current settings. Labels and routing changes require a fresh preview to affect the submitted content.

Titles are limited to 160 characters and final bodies to 1200 characters. The full escaped preview must fit 1900 characters. Overlong drafts, hidden formatting controls, and labels that cannot be displayed unambiguously are refused, not truncated. Markdown is escaped and mentions are disabled. The reporter should remove personal information before confirming: the final issue is public. Related-issue advice is a separate message, not part of the posted body or an assertion of duplication.

Each random 256-bit token is bound to its reporter, public guild, channel/thread, and delivered preview message ID. Delivery failure revokes it. Provisional previews cannot be confirmed. File and Cancel atomically compete for one use; mismatched clicks do not reveal or consume the payload. Drafts expire after 15 minutes, including delivery time, and at most 200 are held in memory. Restart or eviction requires a fresh preview. Cancel, double-clicks, and expired previews never trigger GitHub writes.

An HTTP failure after submission can be ambiguous. The token remains consumed; Prompter says an issue **may** exist and asks the reporter to check the repository. It never replays the POST, even after an authentication failure. A rejected installation token is invalidated only for a subsequent, separately authorized operation.

## Deployment inputs (not activated here)

All settings below bind under `Cratis:Prompter:GitHub` (environment prefix `Cratis__Prompter__GitHub__`).

| Setting | Required value |
| --- | --- |
| `ApiUrl` | Exactly `https://api.github.com/`; enterprise/custom hosts are not supported by this helper. |
| `Owner` | The approved organization that owns the installation repositories. |
| `InstallationRepositories` | Explicit `owner/repository` allowlist. Empty denies all. Align default/product routing with this list. |
| `AppId` | Approved GitHub App numeric ID. |
| `InstallationId` | Approved installation numeric ID for that owner. |
| `AppPrivateKeyPath` | Path to a securely mounted PEM private key, not the PEM itself. |
| `AllowTokenFallback` | Defaults to false. Only an explicit true permits the legacy `Token`, and only with **no** App configuration present. |

Do not put keys/tokens into source configuration, CLI arguments, examples, logs, or reports. Provision a restricted secret mount through the deployment's approved mechanism; restrict file access to the process identity. The `IAppKey` boundary permits another approved secret source without changing token issuance. A partial or broken App configuration never silently falls back to a static token.

Install the App only on the named repositories with **Issues: read and write** and GitHub's implicit **Metadata: read**. Do not grant Contents, Pull requests, Administration, organization, or user permissions. Webhook permissions/subscriptions are a separate deployment decision. The mint request names only approved repositories and requests only Issues write. Unexpected response permissions/repositories, malformed/expired tokens, and mint failures fail closed.

RS256 App JWTs are signed locally (2048-bit minimum), backdated 60 seconds, and expire eight minutes after signing. JWTs go only to the fixed installation access-token endpoint. Installation tokens are memory-only, shared across concurrent callers, and refreshed with a one-minute expiry margin. API authorization is attached only after HTTPS, host, route, owner and repository validation. Both transports disable redirects/cookies/default credentials and HTTP client logging. Neither minting nor authenticated writes has an automatic retry policy.

## Parent host integration

In `Source/Hosting/HostBuilderExtensions.cs`, add:

```csharp
using Cratis.Prompter.GitHub.Authentication;
```

Remove the legacy `client.DefaultRequestHeaders.Authorization = ...` assignment. Keep the existing user-agent setup and **outer** protected write gate. Change the end of the existing issues-client registration from:

```csharp
}).AddHttpMessageHandler<GitHubWriteGate>();
```

to:

```csharp
}).AddHttpMessageHandler<GitHubWriteGate>().AddGitHubAppAuthentication();
```

This order matters: the protected gate rejects a disabled write **before** credential acquisition. The helper adds a separate narrow mint client, singleton policy/key/token cache and per-request authentication. Do not put the protected non-GET gate on the mint client, exempt arbitrary GitHub endpoints from it, or attach the App JWT to the issues client. Do not add retry/resilience handlers that replay writes. Existing admission, write kill switch, rate limits, and approved public-channel policy remain required. Parent validation should also reject invalid App configuration/routing at startup; request-time authorization independently fails closed.

The host registration and live App/secret provisioning remain parent/operator work. Offline specs use synthetic keys and fake HTTP only; they do not prove installation permissions, secret delivery, live GitHub behavior, or production activation.
