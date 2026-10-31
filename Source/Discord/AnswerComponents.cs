// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using NetCord;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>Builds every action row an answer message carries: feedback, follow-up suggestions and product clarification.</summary>
public static class AnswerComponents
{
    /// <summary>Builds the rows. A plain answer yields exactly the feedback row.</summary>
    /// <param name="answer">The answer.</param>
    /// <param name="actions">The explicit answer actions.</param>
    /// <returns>The action rows; empty when the answer was not recorded and offers nothing else.</returns>
    public static IReadOnlyList<ActionRowProperties> For(Answer answer, AnswerActions actions)
    {
        List<ActionRowProperties> rows = [];
        if (answer.InteractionId is { } id)
        {
            rows.Add(FeedbackButtonRow.For(id, actions));
        }

        if (answer.Clarification is { } clarification)
        {
            rows.Add(new(clarification.Options.Select(product => new ButtonProperties(ClarificationButton.CustomId(product), ProductScope.Display(product), ButtonStyle.Primary))));
        }
        else if (answer.Suggestions.Count > 0 && !answer.IsRefusal)
        {
            rows.Add(new(answer.Suggestions.Take(SuggestionButton.MaximumCount)
                .Select((suggestion, index) => new ButtonProperties(SuggestionButton.CustomId(index), suggestion, ButtonStyle.Secondary))));
        }

        return rows;
    }
}
