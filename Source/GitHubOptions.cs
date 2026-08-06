// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Options for the GitHub integration: filing issues from Discord, answering newly-opened issues, and
/// telling a maintainer channel when one appears.
/// </summary>
/// <remarks>
/// Every capability here is off until its credential is configured, so a deployment that sets nothing
/// behaves exactly as before. See decision D-16 for the scope and its guardrails.
/// </remarks>
public class GitHubOptions
{
    /// <summary>
    /// Gets or sets the token used to file issues and post comments. A fine-grained personal access token
    /// with <c>Issues: Read and write</c> on the target repositories is enough; a GitHub App installation
    /// token works identically because both are sent as a bearer token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GitHub API base address.
    /// </summary>
    public string ApiUrl { get; set; } = "https://api.github.com/";

    /// <summary>
    /// Gets or sets the organization the repositories belong to.
    /// </summary>
    public string Owner { get; set; } = "Cratis";

    /// <summary>
    /// Gets or sets the repository an issue lands in when the product cannot be determined. Documentation is
    /// the safe default: a misrouted issue there is still visible to the people who triage the site, and
    /// nothing about it looks like a product bug report that never gets read.
    /// </summary>
    public string DefaultRepository { get; set; } = "Documentation";

    /// <summary>
    /// Gets or sets the product-to-repository routing, keyed by the product name the classifier produces
    /// (case-insensitive). Q-7 settled on the owning product repository rather than one shared inbox.
    /// </summary>
    public IDictionary<string, string> Repositories { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["chronicle"] = "Chronicle",
        ["arc"] = "Arc",
        ["fundamentals"] = "Fundamentals",
        ["components"] = "Components",
        ["cli"] = "cli",
        ["documentation"] = "Documentation",
    };

    /// <summary>
    /// Gets or sets the label put on every issue Prompter files, so maintainers can tell community-reported
    /// work from their own and filter it.
    /// </summary>
    public string IssueLabel { get; set; } = "from-discord";

    /// <summary>
    /// Gets or sets the label that opts an issue out of being answered. Applying it to an issue — or having
    /// a maintainer apply it by default in a repository's templates — stops Prompter commenting there.
    /// </summary>
    public string OptOutLabel { get; set; } = "no-prompter";

    /// <summary>
    /// Gets or sets the shared secret GitHub signs webhook deliveries with. When empty, the webhook endpoint
    /// refuses every request rather than accepting unsigned ones.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the repositories whose newly-opened issues Prompter may answer, as <c>owner/name</c>.
    /// Empty means answer nowhere: answering is opt-in per repository, because a comment on someone else's
    /// tracker is the most visible thing this bot does.
    /// </summary>
    public IList<string> AnsweringRepositories { get; set; } = [];

    /// <summary>
    /// Gets or sets the channel new issues are announced in. When unset, no announcement is posted — the
    /// zero-code GitHub-to-Discord webhook covers the plain notification, and this exists for the enriched
    /// one that says whether the documentation already answers the issue.
    /// </summary>
    public ulong? NotifyChannelId { get; set; }

    /// <summary>
    /// Gets a value indicating whether issues can be filed. Filing needs a token; without one the
    /// <c>/issue</c> command tells the user it is not configured instead of failing opaquely.
    /// </summary>
    public bool FilingEnabled => Token.Length > 0;

    /// <summary>
    /// Gets a value indicating whether the webhook endpoint accepts deliveries.
    /// </summary>
    public bool WebhookEnabled => WebhookSecret.Length > 0;
}
