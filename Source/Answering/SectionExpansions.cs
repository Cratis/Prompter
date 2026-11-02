// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Answering;

/// <summary>
/// Chooses which section and neighbor passages are added next to the admitted passages, within a byte budget.
/// </summary>
public static class SectionExpansions
{
    const int PassageOverheadBytes = 8;

    /// <summary>
    /// Selects the section expansions that fit the budget.
    /// </summary>
    /// <param name="candidates">The retrieved candidates, in retrieval order.</param>
    /// <param name="admitted">The passages already admitted for the answer.</param>
    /// <param name="budgetBytes">The bytes the expansions may add.</param>
    /// <returns>The expansions to add, in the given order.</returns>
    public static Passage[] Select(IEnumerable<Passage> candidates, IReadOnlyList<Passage> admitted, int budgetBytes)
    {
        var pages = admitted.Select(passage => passage.Page).ToHashSet();
        var chosen = new List<Passage>();
        var used = 0;
        foreach (var candidate in candidates.Where(candidate => candidate.IsSectionExpansion && pages.Contains(candidate.Page)))
        {
            var cost = Encoding.UTF8.GetByteCount(candidate.HeadingPath) + Encoding.UTF8.GetByteCount(candidate.Content) + PassageOverheadBytes;
            if (used + cost > budgetBytes || Contains(admitted, candidate) || Contains(chosen, candidate))
            {
                continue;
            }

            chosen.Add(candidate);
            used += cost;
        }

        return [.. chosen];
    }

    static bool Contains(IEnumerable<Passage> passages, Passage candidate) =>
        passages.Any(passage => passage.Page == candidate.Page && passage.HeadingPath == candidate.HeadingPath && passage.Content == candidate.Content);
}
