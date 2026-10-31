// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Cratis.Prompter.Evaluation;

/// <summary>A curated YAML question set.</summary>
public sealed record EvalSet
{
    /// <summary>Gets the question cases.</summary>
    public IReadOnlyList<EvalCase> Questions { get; init; } = [];

    /// <summary>Loads the set without interpreting unrelated legacy fields.</summary>
    /// <param name="path">The YAML path.</param>
    /// <returns>The loaded cases.</returns>
    public static EvalSet Load(string path)
    {
        using var reader = File.OpenText(path);
        return new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeMapping<IReadOnlyList<EvalCase>, List<EvalCase>>()
            .WithTypeMapping<IReadOnlyList<string>, List<string>>()
            .IgnoreUnmatchedProperties().Build().Deserialize<EvalSet>(reader);
    }

    /// <summary>Finds the default set by walking to the solution root.</summary>
    /// <param name="startDirectory">The starting directory.</param>
    /// <returns>The default path, or null outside a source checkout.</returns>
    public static string? DefaultPath(string startDirectory)
    {
        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Prompter.slnx")))
            {
                var path = Path.Combine(directory.FullName, "Eval", "golden-questions.yaml");
                return File.Exists(path) ? path : null;
            }
        }

        return null;
    }
}
