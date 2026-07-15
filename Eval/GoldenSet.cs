// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Cratis.Prompter.Eval;

/// <summary>
/// Represents the parsed golden Q&amp;A set - the top-level <c>questions:</c> list of
/// <c>Eval/golden-questions.yaml</c>.
/// </summary>
public record GoldenSet
{
    /// <summary>Gets the questions in the set.</summary>
    public IEnumerable<GoldenQuestion> Questions { get; init; } = [];

    /// <summary>
    /// Parses a golden set from its YAML representation. Unknown keys (such as <c>tags</c> and
    /// <c>rationale</c>) are ignored, and <c>expected_pages</c> maps to <see cref="GoldenQuestion.ExpectedPages"/>
    /// via the underscored naming convention.
    /// </summary>
    /// <param name="yaml">The YAML content of the golden set.</param>
    /// <returns>The parsed <see cref="GoldenSet"/>.</returns>
    public static GoldenSet Parse(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<GoldenSet>(yaml) ?? new GoldenSet();
    }
}
