// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>
/// The kind of work an issue represents. Kept deliberately coarse: it decides a label and how the body is
/// framed, and a reporter should never have to think about taxonomy to report something.
/// </summary>
public enum IssueKind
{
    /// <summary>
    /// Something is broken or behaves against its documented contract.
    /// </summary>
    Bug = 0,

    /// <summary>
    /// A capability that does not exist and is being asked for concretely — including a missing API.
    /// </summary>
    Feature = 1,

    /// <summary>
    /// A direction worth considering, not yet a concrete request.
    /// </summary>
    Idea = 2,

    /// <summary>
    /// The behavior exists but is undocumented, wrongly documented, or impossible to find.
    /// </summary>
    Documentation = 3
}
