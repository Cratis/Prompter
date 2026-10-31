// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.GitHub.Authentication;
using Cratis.Prompter.Protection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Cratis.Prompter.Discord;

/// <summary>
/// The <c language="csharp">/issue</c> slash command: turns what someone is describing in Discord into a GitHub issue.
/// </summary>
/// <param name="drafting">Drafts the issue from the description.</param>
/// <param name="issues">Looks for likely duplicates before the preview is shown.</param>
/// <param name="pending">Holds the draft between the preview and the confirmation.</param>
/// <param name="admission">The cheap shared admission guard.</param>
/// <param name="work">The global concurrency and durable spend guard.</param>
/// <param name="options">The Prompter options carrying the GitHub configuration and reply text.</param>
/// <param name="logger">Logger for diagnostics.</param>
/// <remarks>
/// Filing is always a deliberate act (decision D-16): Prompter drafts, the reporter confirms in an ephemeral
/// preview, and only then does an issue exist. Anyone may file — at this community's size an approval step
/// would cost more in missed reports than it saves in noise — with the duplicate hint, the shared rate limit
/// and the provenance label carrying that load instead.
/// </remarks>
public class FileIssue(
    IIssueDrafting drafting,
    IIssues issues,
    PendingIssues pending,
    Admission admission,
    PaidWork work,
    IOptions<PrompterOptions> options,
    ILogger<FileIssue> logger) : ApplicationCommandModule<ApplicationCommandContext>
{
    /// <summary>
    /// The reply when issue filing has no credential configured.
    /// </summary>
    public const string NotConfiguredReply =
        "I can't file issues yet — no GitHub credential is configured for me. A maintainer can set one up.";

    /// <summary>
    /// The reply when the model could not turn the description into an issue.
    /// </summary>
    public const string CouldNotDraftReply =
        "I couldn't turn that into an issue. Try describing what you expected and what happened instead.";

    /// <summary>
    /// Drafts a GitHub issue from a description and shows it for confirmation.
    /// </summary>
    /// <param name="description">What is wrong, missing, or being asked for.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [SlashCommand("issue", "Turn this into a GitHub issue on the right Cratis repository")]
    public async Task Handle(string description)
    {
        var github = options.Value.GitHub;
        if (!await Accept(description))
        {
            return;
        }

        var deferred = false;
        try
        {
            await using var lease = await work.BeginInteractive();
            lease.Activate();

            // Drafting is ephemeral so the preview stays private until confirmed.
            await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));
            deferred = true;
            var draft = await drafting.Draft(description, hint: null, lease.Token);
            if (draft is null)
            {
                logger.CouldNotDraftIssue();
                await Context.Interaction.SendFollowupMessageAsync(Ephemeral(CouldNotDraftReply));
                return;
            }

            var repository = IssueRouting.RepositoryFor(draft.Product, github);
            var routed = IssueRouting.IsRouted(draft.Product, github);
            var payload = IssueComposition.Payload(draft, repository, ConversationUrl(), github);
            new GitHubPolicy(options).RequireRepository(new(payload.Owner, payload.Repository));
            var preview = IssuePreview.Text(payload, routed);
            if (preview is null)
            {
                await Context.Interaction.SendFollowupMessageAsync(Ephemeral("That draft is too long or contains text that cannot be safely previewed. Please shorten or clarify it and run `/issue` again. Nothing was filed."));
                return;
            }
            var similar = await issues.FindSimilar(payload.Repository, payload.Title, lease.Token);
            if (similar.Count > 0)
            {
                await Context.Interaction.SendFollowupMessageAsync(Ephemeral(IssuePreview.Advisory(similar)));
            }
            var consent = new IssueConsent(Context.User.Id, Context.Interaction.GuildId!.Value, Context.Channel.Id);
            await pending.Present(payload, consent, async token =>
            {
                var message = await Context.Interaction.SendFollowupMessageAsync(new()
                {
                    Content = preview,
                    Flags = MessageFlags.Ephemeral,
                    AllowedMentions = AllowedMentionsProperties.None,
                    Components = [IssuePreview.Buttons(token)]
                });
                return message.Id;
            });
        }
        catch (WorkRefused)
        {
            await Refuse(admission.MayReply(new(Context.Interaction.GuildId, Context.Channel.Id, Context.User.Id)), deferred);
        }
        catch (Exception exception)
        {
            logger.IssueDraftingFailed(exception);
            await TryApologize();
        }
    }

    static InteractionMessageProperties Ephemeral(string content) =>
        new() { Content = content, Flags = MessageFlags.Ephemeral, AllowedMentions = AllowedMentionsProperties.None };

    async Task<bool> Accept(string description)
    {
        if (!CanDraft())
        {
            return false;
        }

        var decision = admission.TryEnter(new(Context.Interaction.GuildId, Context.Channel.Id, Context.User.Id), description);
        if (!decision.Allowed)
        {
            await Refuse(decision.Reply, false);
            return false;
        }

        if (!options.Value.GitHub.FilingEnabled)
        {
            await RespondEphemerally(NotConfiguredReply);
            return false;
        }

        return true;
    }

    bool CanDraft() => Context.Interaction.GuildId is not null && admission.InScope(Context.Interaction.GuildId, Context.Channel.Id) && options.Value.Protection.GitHubWritesEnabled;

    async Task Refuse(bool reply, bool deferred)
    {
        if (!reply)
        {
            return;
        }

        if (deferred)
        {
            await Context.Interaction.SendFollowupMessageAsync(Ephemeral(options.Value.Discord.RateLimitedReply));
        }
        else
        {
            await RespondEphemerally(options.Value.Discord.RateLimitedReply);
        }
    }

    string? ConversationUrl()
    {
        if (Context.Interaction.Channel is null)
        {
            return null;
        }

        var guild = Context.Interaction.GuildId?.ToString(CultureInfo.InvariantCulture) ?? "@me";
        var channel = Context.Interaction.Channel.Id.ToString(CultureInfo.InvariantCulture);

        return $"https://discord.com/channels/{guild}/{channel}";
    }

    async Task RespondEphemerally(string content) =>
        await Context.Interaction.SendResponseAsync(InteractionCallback.Message(Ephemeral(content)));

    async Task TryApologize()
    {
        try
        {
            await Context.Interaction.SendFollowupMessageAsync(Ephemeral(options.Value.Discord.ErrorReply));
        }
        catch (Exception exception)
        {
            // Nothing more can be delivered to this interaction; record it and let the command return.
            logger.IssueApologyFailed(exception);
        }
    }
}
