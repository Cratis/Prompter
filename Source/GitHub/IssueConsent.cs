// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>
/// The Discord identity and public location authorized to confirm a preview.
/// </summary>
/// <param name="Reporter">The reporting user.</param>
/// <param name="Guild">The admitted public guild.</param>
/// <param name="Channel">The admitted public channel or thread.</param>
public sealed record IssueConsent(ulong Reporter, ulong Guild, ulong Channel);
