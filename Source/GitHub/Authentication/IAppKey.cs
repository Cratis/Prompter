// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub.Authentication;

/// <summary>
/// Reads a private key from the approved deployment source, not ordinary configuration values.
/// </summary>
public interface IAppKey
{
    /// <summary>
    /// Reads the PEM private key without logging it.
    /// </summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The PEM key, used only for signing the App JWT.</returns>
    Task<string> Read(CancellationToken cancellationToken);
}
