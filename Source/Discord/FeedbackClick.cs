// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Discord;

/// <summary>
/// The decoded content of a feedback button's custom id: the verdict the user chose and the interaction it
/// applies to.
/// </summary>
/// <param name="Verdict">The verdict the clicked button carried.</param>
/// <param name="InteractionId">The id of the interaction row the verdict should be written back to.</param>
public record FeedbackClick(FeedbackVerdict Verdict, long InteractionId);
