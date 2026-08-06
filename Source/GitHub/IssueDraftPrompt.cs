// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Prompter.GitHub;

/// <summary>
/// The prompt that turns a Discord conversation into a drafted issue.
/// </summary>
/// <remarks>
/// The model writes the issue; the reporter approves it. That order matters: people describe problems in
/// chat far better than they fill in issue templates, and the thing standing between a good report and the
/// tracker is usually the transcription work, not the willingness.
/// </remarks>
public static class IssueDraftPrompt
{
    /// <summary>
    /// The system prompt. Frozen so it can be prompt-cached the same way the answering prompt is.
    /// </summary>
    public const string SystemText =
        """
        You turn Cratis community conversations into GitHub issues that a maintainer can act on.

        Return ONLY a JSON object, no prose and no code fence, with exactly these fields:
        {"title": "...", "body": "...", "kind": "bug|feature|idea|documentation", "product": "..."}

        Rules:
        - title: one line, specific, no ticket-speak. "Projection stops after a rename" beats "Bug in projections".
        - body: markdown. Lead with what the person is trying to do and what happened instead. Include any
          version, error text, or code they gave, verbatim. Never invent reproduction steps, versions, stack
          traces, or API names that were not in the conversation - an issue that contains a fabricated detail
          costs a maintainer more than no issue at all. If something important is missing, add a short
          "Unknown from the conversation" list naming what a maintainer will need to ask.
        - kind: "bug" when something is broken; "feature" when a concrete capability or API is missing;
          "idea" when it is a direction rather than a request; "documentation" when the behavior exists but
          is undocumented or unfindable.
        - product: one of chronicle, arc, fundamentals, components, cli, documentation. Use an empty string
          when the conversation genuinely does not say - guessing routes the issue to the wrong maintainers.
        - Use American English.
        """;

    /// <summary>
    /// Builds the user message describing the conversation to draft from.
    /// </summary>
    /// <param name="conversation">What the person wrote, and any surrounding context worth including.</param>
    /// <param name="hint">An optional steer from the reporter, such as what they think it is.</param>
    /// <returns>The user message text.</returns>
    public static string UserMessage(string conversation, string? hint)
    {
        var message = new StringBuilder("Conversation:\n\n");
        message.Append(conversation.Trim());

        if (!string.IsNullOrWhiteSpace(hint))
        {
            message.Append("\n\nThe reporter adds: ").Append(hint.Trim());
        }

        message.Append("\n\nDraft the issue as JSON.");

        return message.ToString();
    }
}
