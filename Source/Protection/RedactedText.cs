// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>Text safe for downstream processing and whether a secret shape was removed.</summary>
/// <param name="Text">The redacted text.</param>
/// <param name="Removed">Whether any secret-shaped value was removed.</param>
public sealed record RedactedText(string Text, bool Removed);
