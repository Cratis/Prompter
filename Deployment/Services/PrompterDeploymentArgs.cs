// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pulumi;
using KubernetesProvider = Pulumi.Kubernetes.Provider;

namespace Cratis.Prompter.Deployment.Services;

/// <summary>
/// Arguments for <see cref="PrompterDeployment"/>.
/// </summary>
public sealed class PrompterDeploymentArgs
{
    /// <summary>
    /// Gets the Kubernetes provider to deploy through.
    /// </summary>
    public required KubernetesProvider Provider { get; init; }

    /// <summary>
    /// Gets the namespace to deploy into.
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// Gets the environment label.
    /// </summary>
    public required string Environment { get; init; }

    /// <summary>
    /// Gets the resource the namespace is created by.
    /// </summary>
    public required Resource NamespaceResource { get; init; }

    /// <summary>
    /// Gets the image to run, pinned to a released version by the deploy workflow.
    /// </summary>
    public required string Image { get; init; }

    /// <summary>
    /// Gets the connection string for the corpus database.
    /// </summary>
    public required Output<string> ConnectionString { get; init; }

    /// <summary>
    /// Gets the Discord bot token.
    /// </summary>
    public required Output<string> DiscordToken { get; init; }

    /// <summary>
    /// Gets the Anthropic API key used to generate answers.
    /// </summary>
    public required Output<string> AnthropicApiKey { get; init; }

    /// <summary>
    /// Gets the Voyage API key used to embed the corpus and queries.
    /// </summary>
    public required Output<string> VoyageApiKey { get; init; }

    /// <summary>
    /// Gets the shared secret that authorizes <c>POST /reindex</c>.
    /// </summary>
    public required Output<string> ReindexSecret { get; init; }

    /// <summary>
    /// Gets the token used to file issues and comment on them. Empty leaves issue filing switched off, and
    /// the <c>/issue</c> command says so rather than failing opaquely.
    /// </summary>
    public required Output<string> GitHubToken { get; init; }

    /// <summary>
    /// Gets the secret GitHub signs webhook deliveries with. Empty makes the webhook endpoint refuse
    /// everything, which is the safe posture for an unconfigured deployment.
    /// </summary>
    public required Output<string> GitHubWebhookSecret { get; init; }

    /// <summary>
    /// Gets the repositories whose newly-opened issues may be answered, as a comma-separated
    /// <c>owner/name</c> list. Empty answers nowhere — answering someone's tracker is opt-in.
    /// </summary>
    public string? AnsweringRepositories { get; init; }

    /// <summary>
    /// Gets the channel new issues are announced in, if configured.
    /// </summary>
    public string? IssueNotifyChannelId { get; init; }

    /// <summary>
    /// Gets the id of the channel where plain messages are treated as questions, if configured.
    /// </summary>
    public string? AskChannelId { get; init; }

    /// <summary>
    /// Gets the id of the help forum channel new threads are auto-answered in, if configured.
    /// </summary>
    public string? HelpForumChannelId { get; init; }

    /// <summary>
    /// Gets resources this deployment must be created after — the database it connects to.
    /// </summary>
    public InputList<Resource> DependsOn { get; init; } = [];
}
