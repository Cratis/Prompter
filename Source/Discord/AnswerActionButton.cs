// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Discord;

/// <summary>Content-free custom ids for explicit answer actions.</summary>
public static partial class AnswerActionButton
{
    /// <summary>The human hand-off route.</summary>
    public const string HumanPrefix = "human";

    /// <summary>The documentation forwarding route.</summary>
    public const string DocumentedPrefix = "documented";

    /// <summary>The feature request route.</summary>
    public const string RequestPrefix = "request";

    /// <summary>The human hand-off label.</summary>
    public const string HumanLabel = "🙋 Ask a human";

    /// <summary>The documentation forwarding label.</summary>
    public const string DocumentedLabel = "📚 This should be documented";

    /// <summary>The feature request label.</summary>
    public const string RequestLabel = "💡 Request this";

    [GeneratedRegex(@"\A[a-z0-9-]{1,40}\z", RegexOptions.CultureInvariant, 100)]
    private static partial Regex ProductKey { get; }

    /// <summary>Builds an action id.</summary>
    /// <param name="prefix">The action route.</param>
    /// <param name="question">The question location.</param>
    /// <returns>The custom id.</returns>
    public static string CustomId(string prefix, QuestionLink question) =>
        $"{prefix}:{question.ChannelId}:{question.MessageId}";

    /// <summary>Builds a feature request id with a bounded product key.</summary>
    /// <param name="question">The question location.</param>
    /// <param name="product">The routing product.</param>
    /// <returns>The custom id.</returns>
    public static string RequestCustomId(QuestionLink question, string product) =>
        $"{CustomId(RequestPrefix, question)}:{(ProductKey.IsMatch(product) ? product : RoutingProduct.Unknown.Value)}";

    /// <summary>Parses an action id, failing closed for unknown shapes.</summary>
    /// <param name="customId">The clicked custom id.</param>
    /// <returns>The decoded action or null.</returns>
    public static AnswerActionClick? Parse(string customId)
    {
        var parts = customId.Split(':');
        if (!HasShape(parts) || !ulong.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var channel) ||
            !ulong.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var message))
        {
            return null;
        }
        return new(parts[0], new(channel, message), parts.Length == 4 ? parts[3] : null);
    }

    static bool HasShape(string[] parts) => parts switch
    {
        [HumanPrefix or DocumentedPrefix, _, _] => true,
        [RequestPrefix, _, _, var product] => ProductKey.IsMatch(product),
        _ => false
    };
}
